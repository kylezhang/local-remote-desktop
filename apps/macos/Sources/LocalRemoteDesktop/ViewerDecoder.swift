import Foundation
import VideoToolbox
import CoreMedia
import CoreImage

// Owned by the receive worker. Decode synchronously, retain no compressed-frame
// queue, and present only the most recent decoded image to the main thread.
final class ViewerDecoder {
    private var session: VTDecompressionSession?, format: CMVideoFormatDescription?
    private var sps = Data(), pps = Data(), waitingForIDR = true
    private var index: Int64 = 0, outputError: OSStatus = noErr
    private let width: Int, height: Int
    private let image: (CGImage) -> Void
    private let context = CIContext(options: [.cacheIntermediates: false])
    init(width: Int, height: Int, image: @escaping (CGImage) -> Void) { self.width = width; self.height = height; self.image = image }
    private func invalidate() {
        if let session { VTDecompressionSessionWaitForAsynchronousFrames(session); VTDecompressionSessionInvalidate(session) }
        session = nil; format = nil
    }
    func decode(_ data: Data) throws {
        let unit = try H264AccessUnit(data)
        let nextSPS = unit.sps ?? sps, nextPPS = unit.pps ?? pps
        if nextSPS != sps || nextPPS != pps {
            invalidate(); sps = nextSPS; pps = nextPPS; waitingForIDR = true
        }
        guard unit.picture, !sps.isEmpty, !pps.isEmpty else { return }
        if waitingForIDR && !unit.idr { return }
        if session == nil { try create() }
        guard let session, let format else { throw ControllerError.video }
        let avcc = unit.avcc
        guard !avcc.isEmpty else { return }
        var block: CMBlockBuffer?
        guard CMBlockBufferCreateWithMemoryBlock(allocator: kCFAllocatorDefault, memoryBlock: nil, blockLength: avcc.count,
            blockAllocator: kCFAllocatorDefault, customBlockSource: nil, offsetToData: 0, dataLength: avcc.count,
            flags: 0, blockBufferOut: &block) == kCMBlockBufferNoErr, let block else { throw ControllerError.video }
        let copied = avcc.withUnsafeBytes { CMBlockBufferReplaceDataBytes(with: $0.baseAddress!, blockBuffer: block, offsetIntoDestination: 0, dataLength: avcc.count) }
        guard copied == kCMBlockBufferNoErr else { throw ControllerError.video }
        var sample: CMSampleBuffer?, size = avcc.count
        var timing = CMSampleTimingInfo(duration: .invalid, presentationTimeStamp: CMTime(value: index, timescale: 60), decodeTimeStamp: .invalid)
        index += 1
        guard CMSampleBufferCreateReady(allocator: kCFAllocatorDefault, dataBuffer: block, formatDescription: format,
            sampleCount: 1, sampleTimingEntryCount: 1, sampleTimingArray: &timing, sampleSizeEntryCount: 1,
            sampleSizeArray: &size, sampleBufferOut: &sample) == noErr, let sample else { throw ControllerError.video }
        outputError = noErr
        let result = VTDecompressionSessionDecodeFrame(session, sampleBuffer: sample, flags: [], frameRefcon: nil, infoFlagsOut: nil)
        VTDecompressionSessionWaitForAsynchronousFrames(session)
        guard result == noErr, outputError == noErr else { throw ControllerError.video }
        waitingForIDR = false
    }
    private func create() throws {
        let result = sps.withUnsafeBytes { a in pps.withUnsafeBytes { b -> OSStatus in
            let pointers = [a.bindMemory(to: UInt8.self).baseAddress!, b.bindMemory(to: UInt8.self).baseAddress!]
            let sizes = [sps.count,pps.count]
            return pointers.withUnsafeBufferPointer { ptr in sizes.withUnsafeBufferPointer { lengths in
                CMVideoFormatDescriptionCreateFromH264ParameterSets(allocator: kCFAllocatorDefault, parameterSetCount: 2,
                    parameterSetPointers: ptr.baseAddress!, parameterSetSizes: lengths.baseAddress!, nalUnitHeaderLength: 4,
                    formatDescriptionOut: &format)
            } }
        } }
        guard result == noErr, let format else { throw ControllerError.video }
        let dimensions = CMVideoFormatDescriptionGetDimensions(format)
        guard Int(dimensions.width) == width, Int(dimensions.height) == height else { throw ControllerError.video }
        var callback = VTDecompressionOutputCallbackRecord(decompressionOutputCallback: { ref, _, status, _, buffer, _, _ in
            guard let ref else { return }
            let owner = Unmanaged<ViewerDecoder>.fromOpaque(ref).takeUnretainedValue()
            guard status == noErr else { owner.outputError = status; return }
            guard let buffer else { return }
            let frame = CIImage(cvPixelBuffer: buffer)
            if let image = owner.context.createCGImage(frame, from: frame.extent) { owner.image(image) }
            else { owner.outputError = -1 }
        }, decompressionOutputRefCon: Unmanaged.passUnretained(self).toOpaque())
        let options = [kVTVideoDecoderSpecification_EnableHardwareAcceleratedVideoDecoder: true] as CFDictionary
        let pixels = [kCVPixelBufferPixelFormatTypeKey: kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange] as CFDictionary
        guard VTDecompressionSessionCreate(allocator: kCFAllocatorDefault, formatDescription: format,
            decoderSpecification: options, imageBufferAttributes: pixels, outputCallback: &callback,
            decompressionSessionOut: &session) == noErr else { throw ControllerError.video }
    }
    deinit { invalidate() }
}
