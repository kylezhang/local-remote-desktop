import Foundation
import ScreenCaptureKit
import VideoToolbox
import CoreMedia

enum CaptureError: Error { case noDisplay, encoder(OSStatus) }
final class Capture: NSObject, SCStreamOutput, SCStreamDelegate {
    var stream: SCStream?
    private var encoder: VTCompressionSession?
    private let queue = DispatchQueue(label: "localremote.capture")
    private let wire: Wire
    private let failure: () -> Void
    private let outputLock = NSLock()
    private let flightLock = NSLock()
    private var inFlight = 0, skipped = 0
    private var encodingTotal = 0.0, encoded = 0
    init(wire: Wire, failure: @escaping () -> Void) { self.wire = wire; self.failure = failure }
    func start(quality: Int) async throws {
        let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)
        guard let display = content.displays.first(where: { $0.displayID == CGMainDisplayID() }) ?? content.displays.first else { throw CaptureError.noDisplay }
        let bounds = CGDisplayBounds(display.displayID)
        let target = quality == 0 ? 1280 : 1920
        let scale = min(Double(target)/Double(display.width), Double(quality == 0 ? 720 : 1080)/Double(display.height), 1)
        let w = max(2, Int(Double(display.width)*scale)/2*2), h = max(2, Int(Double(display.height)*scale)/2*2)
        let fps = quality == 0 ? 30 : 60, bitrate = quality == 0 ? 4_000_000 : quality == 1 ? 10_000_000 : 18_000_000
        let status = VTCompressionSessionCreate(allocator: kCFAllocatorDefault, width: Int32(w), height: Int32(h), codecType: kCMVideoCodecType_H264,
            encoderSpecification: [kVTVideoEncoderSpecification_RequireHardwareAcceleratedVideoEncoder: true] as CFDictionary,
            imageBufferAttributes: nil, compressedDataAllocator: nil, outputCallback: { ref, timing, status, _, sample in
                var milliseconds = 0.0
                if let timing { let p = timing.assumingMemoryBound(to: Double.self); milliseconds = (CFAbsoluteTimeGetCurrent()-p.pointee)*1000; p.deinitialize(count: 1); p.deallocate() }
                guard let ref else { return }
                let owner = Unmanaged<Capture>.fromOpaque(ref).takeUnretainedValue()
                defer { owner.flightLock.lock(); owner.inFlight -= 1; owner.flightLock.unlock() }
                guard status == noErr else { owner.failure(); return }
                if let sample { owner.output(sample, milliseconds: milliseconds) }
            }, refcon: Unmanaged.passUnretained(self).toOpaque(), compressionSessionOut: &encoder)
        guard status == noErr, let encoder else { throw CaptureError.encoder(status) }
        let properties: [(CFString, CFTypeRef)] = [(kVTCompressionPropertyKey_RealTime, NSNumber(value: true)), (kVTCompressionPropertyKey_AllowFrameReordering, NSNumber(value: false)), (kVTCompressionPropertyKey_ProfileLevel, kVTProfileLevel_H264_Baseline_AutoLevel), (kVTCompressionPropertyKey_AverageBitRate, NSNumber(value: bitrate)), (kVTCompressionPropertyKey_ExpectedFrameRate, NSNumber(value: fps)), (kVTCompressionPropertyKey_MaxKeyFrameInterval, NSNumber(value: fps))]
        for (key,value) in properties {
            let s = VTSessionSetProperty(encoder, key: key, value: value); guard s == noErr else { throw CaptureError.encoder(s) }
        }
        let prepared = VTCompressionSessionPrepareToEncodeFrames(encoder); guard prepared == noErr else { throw CaptureError.encoder(prepared) }
        try wire.json(["op":"video_header","width":w,"height":h,"fps":fps,"codec":"h264-annexb"])
        InputInjector.bounds = bounds
        let config = SCStreamConfiguration(); config.width = w; config.height = h
        config.minimumFrameInterval = CMTime(value: 1, timescale: Int32(fps)); config.queueDepth = 3
        config.pixelFormat = kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange; config.showsCursor = true
        config.capturesAudio = false
        let stream = SCStream(filter: SCContentFilter(display: display, excludingWindows: []), configuration: config, delegate: self)
        self.stream = stream; try stream.addStreamOutput(self, type: .screen, sampleHandlerQueue: queue)
        try await stream.startCapture()
    }
    func stop() async {
        if let stream { try? await stream.stopCapture() }; stream = nil
        await withCheckedContinuation { continuation in queue.async { [self] in
            if let encoder { VTCompressionSessionCompleteFrames(encoder, untilPresentationTimeStamp: .invalid); VTCompressionSessionInvalidate(encoder) }; encoder = nil
            continuation.resume()
        } }
    }
    func stream(_ stream: SCStream, didStopWithError error: Error) { failure() }
    func stream(_ stream: SCStream, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        guard type == .screen, sampleBuffer.isValid, let pixel = sampleBuffer.imageBuffer, let encoder,
              let attachments = CMSampleBufferGetSampleAttachmentsArray(sampleBuffer, createIfNecessary: false) as? [[SCStreamFrameInfo: Any]],
              let raw = attachments.first?[.status] as? Int, SCFrameStatus(rawValue: raw) == .complete else { return }
        // Capture callback and encoder callbacks may differ; timing is attached to sourceFrameRefCon instead of shared dictionaries.
        // Drop raw capture frames before encoding, never encoded reference frames.
        // A slow socket/encoder must not accumulate an unbounded video backlog.
        flightLock.lock()
        guard inFlight < 2 else { skipped += 1; flightLock.unlock(); return }
        inFlight += 1; flightLock.unlock()
        let time = UnsafeMutablePointer<Double>.allocate(capacity: 1); time.initialize(to: CFAbsoluteTimeGetCurrent())
        let status = VTCompressionSessionEncodeFrame(encoder, imageBuffer: pixel, presentationTimeStamp: sampleBuffer.presentationTimeStamp, duration: sampleBuffer.duration, frameProperties: nil, sourceFrameRefcon: time, infoFlagsOut: nil)
        if status != noErr { flightLock.lock(); inFlight -= 1; flightLock.unlock(); time.deinitialize(count: 1); time.deallocate(); failure() }
    }
    private func output(_ sample: CMSampleBuffer, milliseconds: Double) {
        outputLock.lock(); defer { outputLock.unlock() }
        guard let format = sample.formatDescription, let block = sample.dataBuffer else { return }
        var result = Data(), lengthField: Int32 = 4
        let attachments = CMSampleBufferGetSampleAttachmentsArray(sample, createIfNecessary: false) as? [[String: Any]]
        let keyframe = (attachments?.first?[kCMSampleAttachmentKey_NotSync as String] as? Bool) != true
        if keyframe {
            for index in 0..<2 {
                var pointer: UnsafePointer<UInt8>?, size = 0, count = 0
                guard CMVideoFormatDescriptionGetH264ParameterSetAtIndex(format, parameterSetIndex: index, parameterSetPointerOut: &pointer, parameterSetSizeOut: &size, parameterSetCountOut: &count, nalUnitHeaderLengthOut: &lengthField) == noErr, let pointer else { failure(); return }
                result.append(contentsOf: [0,0,0,1]); result.append(pointer, count: size)
            }
        }
        // VideoToolbox's AVCC access unit becomes Annex B; copy supports non-contiguous CMBlockBuffers.
        let size = CMBlockBufferGetDataLength(block); var bytes = [UInt8](repeating: 0, count: size)
        guard CMBlockBufferCopyDataBytes(block, atOffset: 0, dataLength: size, destination: &bytes) == noErr else { failure(); return }
        var offset = 0
        while offset+Int(lengthField) <= size {
            var count = 0; for i in 0..<Int(lengthField) { count = (count << 8) | Int(bytes[offset+i]) }; offset += Int(lengthField)
            guard count > 0 && offset+count <= size else { failure(); return }
            result.append(contentsOf: [0,0,0,1]); result.append(contentsOf: bytes[offset..<offset+count]); offset += count
        }
        guard offset == size else { failure(); return }
        do {
            try wire.send(type: 2, payload: result)
            encodingTotal += milliseconds; encoded += 1
            if encoded >= 30 {
                flightLock.lock(); let dropped = skipped; flightLock.unlock()
                try wire.json(["op":"metrics","encodeMs":encodingTotal/Double(encoded),"captureSkipped":dropped]); encodingTotal = 0; encoded = 0
            }
        } catch { failure() }
    }
}
