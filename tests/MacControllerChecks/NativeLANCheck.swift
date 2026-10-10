import Foundation
import CoreGraphics

// Synthetic test password is never loaded from the user's pairing store.
// Run only against the companion test-owned Windows field, not a user's app.
@main enum NativeLANCheck {
    static func main() throws {
        let args = CommandLine.arguments
        guard args.count == 5, let x = Double(args[2]), let y = Double(args[3]),
              let quality = Int(args[4]), (0...2).contains(quality), (0...1).contains(x), (0...1).contains(y) else { fatalError("Expected LAN address, normalized test field x/y and quality") }
        let lock = NSLock(), ready = DispatchSemaphore(value: 0), ended = DispatchSemaphore(value: 0)
        var frames = 0, failure: Error?, connected = false
        let session = ControllerSession(address: try ControllerRules.address(args[1]), password: "Office-LAN#42", quality: quality,
            status: { message in
                if message["op"] as? String == "connected" { lock.lock(); connected = true; lock.unlock() }
            }, frame: { image in
                precondition(image.width > 0 && image.height > 0)
                lock.lock(); frames += 1; let notify = frames == 3; lock.unlock()
                if notify { ready.signal() }
            }, ended: { error in lock.lock(); failure = error; lock.unlock(); ended.signal() })
        session.start()
        guard ready.wait(timeout: .now()+20) == .success else {
            session.stop(); _ = ended.wait(timeout: .now()+5)
            fatalError("Native Mac did not decode Windows LAN video: \(String(describing: failure))")
        }
        lock.lock(); let isConnected = connected; lock.unlock(); precondition(isConnected)
        for code: UInt16 in [0,18] {
            let vk = ControllerKeys.vk(code, shortcuts: true)!
            session.send(["op":"key_event","vk":vk,"down":true]); session.send(["op":"key_event","vk":vk,"down":false])
        }
        session.send(["op":"mouse_move","x":x,"y":y])
        session.send(["op":"mouse_button","button":0,"down":true,"x":x,"y":y])
        session.send(["op":"mouse_button","button":0,"down":false,"x":x,"y":y])
        session.send(["op":"scroll","delta":120,"x":x,"y":y])
        // release_all intentionally purges pending input. Let the writer deliver
        // this test sequence before exercising that safety operation.
        Thread.sleep(forTimeInterval: 0.5)
        session.send(["op":"release_all"])
        Thread.sleep(forTimeInterval: 1)
        session.stop(); precondition(ended.wait(timeout: .now()+5) == .success)
        lock.lock(); let count = frames; let error = failure; lock.unlock()
        // The Windows harness may close immediately after accepting all input.
        if let wireError = error as? WireError, case .closed = wireError { }
        else { precondition(error == nil) }
        print("PASS native Mac ControllerSession/VideoToolbox -> actual Windows LAN host: \(count) decoded frames, ordered test input, stop cleanup")
    }
}
