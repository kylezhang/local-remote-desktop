import Foundation
import CoreMedia
import CoreVideo
import VideoToolbox
import CoreGraphics

private final class EncodedFrames {
    let lock = NSLock()
    var units = [Data](), failed = false
    func add(_ sample: CMSampleBuffer?, status: OSStatus) {
        lock.lock(); defer { lock.unlock() }
        guard status == noErr, let sample, let format = CMSampleBufferGetFormatDescription(sample),
              let block = CMSampleBufferGetDataBuffer(sample) else { failed = true; return }
        var data = Data()
        for i in 0..<2 {
            var pointer: UnsafePointer<UInt8>?, size = 0, count = 0
            guard CMVideoFormatDescriptionGetH264ParameterSetAtIndex(format, parameterSetIndex: i,
                parameterSetPointerOut: &pointer, parameterSetSizeOut: &size,
                parameterSetCountOut: &count, nalUnitHeaderLengthOut: nil) == noErr, let pointer else { failed = true; return }
            data.append(contentsOf: [0,0,0,1]); data.append(pointer, count: size)
        }
        let size = CMBlockBufferGetDataLength(block)
        var bytes = [UInt8](repeating: 0, count: size)
        guard CMBlockBufferCopyDataBytes(block, atOffset: 0, dataLength: size, destination: &bytes) == noErr else { failed = true; return }
        var offset = 0
        while offset+4 <= bytes.count {
            let length = bytes[offset..<offset+4].reduce(0) { ($0 << 8) | Int($1) }; offset += 4
            guard length > 0, offset+length <= bytes.count else { failed = true; return }
            data.append(contentsOf: [0,0,0,1]); data.append(contentsOf: bytes[offset..<offset+length]); offset += length
        }
        guard offset == bytes.count else { failed = true; return }
        units.append(data)
    }
}

private func encoded(width: Int, height: Int) throws -> [Data] {
    let output = EncodedFrames()
    var session: VTCompressionSession?
    let options = [kVTVideoEncoderSpecification_EnableHardwareAcceleratedVideoEncoder: true] as CFDictionary
    let result = VTCompressionSessionCreate(allocator: kCFAllocatorDefault, width: Int32(width), height: Int32(height),
        codecType: kCMVideoCodecType_H264, encoderSpecification: options, imageBufferAttributes: nil,
        compressedDataAllocator: nil, outputCallback: { ref, _, status, _, sample in
            guard let ref else { return }
            Unmanaged<EncodedFrames>.fromOpaque(ref).takeUnretainedValue().add(sample, status: status)
        }, refcon: Unmanaged.passUnretained(output).toOpaque(), compressionSessionOut: &session)
    guard result == noErr, let session else { throw ControllerError.video }
    defer { VTCompressionSessionInvalidate(session) }
    for (key,value) in [(kVTCompressionPropertyKey_RealTime,kCFBooleanTrue!),
                        (kVTCompressionPropertyKey_AllowFrameReordering,kCFBooleanFalse!)] {
        precondition(VTSessionSetProperty(session, key: key, value: value) == noErr)
    }
    precondition(VTSessionSetProperty(session, key: kVTCompressionPropertyKey_ProfileLevel,
                                     value: kVTProfileLevel_H264_Baseline_AutoLevel) == noErr)
    precondition(VTCompressionSessionPrepareToEncodeFrames(session) == noErr)
    for index in 0..<9 {
        var pixel: CVPixelBuffer?
        precondition(CVPixelBufferCreate(kCFAllocatorDefault, width, height, kCVPixelFormatType_32BGRA,
            [kCVPixelBufferIOSurfacePropertiesKey: [:]] as CFDictionary, &pixel) == kCVReturnSuccess)
        let buffer = pixel!
        CVPixelBufferLockBaseAddress(buffer, [])
        let bytes = CVPixelBufferGetBaseAddress(buffer)!.assumingMemoryBound(to: UInt8.self)
        let stride = CVPixelBufferGetBytesPerRow(buffer)
        for y in 0..<height { for x in 0..<width {
            let p = y*stride+x*4
            bytes[p] = index%3 == 2 ? 255 : 0
            bytes[p+1] = index%3 == 1 ? 255 : 0
            bytes[p+2] = index%3 == 0 ? 255 : 0; bytes[p+3] = 255
        } }
        CVPixelBufferUnlockBaseAddress(buffer, [])
        precondition(VTCompressionSessionEncodeFrame(session, imageBuffer: buffer,
            presentationTimeStamp: CMTime(value: Int64(index), timescale: 30), duration: CMTime(value: 1, timescale: 30),
            frameProperties: [kVTEncodeFrameOptionKey_ForceKeyFrame: true] as CFDictionary,
            sourceFrameRefcon: nil, infoFlagsOut: nil) == noErr)
    }
    precondition(VTCompressionSessionCompleteFrames(session, untilPresentationTimeStamp: .invalid) == noErr)
    precondition(!output.failed && output.units.count == 9)
    return output.units
}

@main enum NativeVideoCheck {
    static func main() throws {
        let frames = try encoded(width: 320, height: 180)
        var images = 0
        let decoder = ViewerDecoder(width: 320, height: 180) { image in
            precondition(image.width == 320 && image.height == 180)
            var rgba = [UInt8](repeating: 0, count: 4)
            rgba.withUnsafeMutableBytes { pixels in
                let context = CGContext(data: pixels.baseAddress, width: 1, height: 1, bitsPerComponent: 8,
                    bytesPerRow: 4, space: CGColorSpaceCreateDeviceRGB(),
                    bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue)!
                context.draw(image, in: CGRect(x: 0,y: 0,width: 1,height: 1))
            }
            let primary = images%3
            precondition(rgba[primary] > 160 && rgba[(primary+1)%3] < 100 && rgba[(primary+2)%3] < 100,
                         "Decoded pixels differ from synthetic encoded color")
            images += 1
        }
        for frame in frames { try decoder.decode(frame) }
        precondition(images == 9)
        print("PASS native VideoToolbox H264 encode -> production ViewerDecoder -> CoreImage/CGImage: 9 frames, dimensions and red/green/blue pixel content")
        let wrong = ViewerDecoder(width: 640, height: 360) { _ in fatalError("Mismatched header allocated output") }
        do { try wrong.decode(frames[0]); fatalError("Mismatched coded dimensions accepted") }
        catch ControllerError.video { }
        let changed = try encoded(width: 640, height: 360)
        do { try decoder.decode(changed[0]); fatalError("Changed SPS dimensions accepted") }
        catch ControllerError.video { }
        print("PASS native coded/header dimensions and SPS-change validation before decode allocation")
    }
}
