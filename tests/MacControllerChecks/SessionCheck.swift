import Foundation

// Transport/lifetime check only. The portable decoder validates Annex B framing;
// it does not emulate VideoToolbox or prove native screen rendering.
final class CGImage {}
final class ViewerDecoder {
    let image: (CGImage) -> Void
    init(width: Int, height: Int, image: @escaping (CGImage) -> Void) { self.image = image }
    func decode(_ data: Data) throws { _ = try H264AccessUnit(data); image(CGImage()) }
}
@main struct SessionCheck {
    static func main() throws {
        let mode = CommandLine.arguments[1], port = UInt16(CommandLine.arguments[2])!
        let finished = DispatchSemaphore(value: 0), lock = NSLock()
        var session: ControllerSession!, connections = 0, frames = 0, endings = 0, failure: Error?
        var textReceived = false
        session = ControllerSession(address: "127.0.0.1", password: "Office-LAN#42", quality: 0, port: port,
            status: { object in
                if object["op"] as? String == "connected" {
                    lock.lock(); connections += 1; lock.unlock()
                    if mode == "clipboard" { session.send(["op":"clipboard_sync","requestId":1,"enabled":true]) }
                    else if mode == "stop" { session.stop() }
                    else {
                        for message: [String: Any] in [
                            ["op":"key_event","vk":162,"down":true], ["op":"key_event","vk":67,"down":true],
                            ["op":"key_event","vk":67,"down":false], ["op":"key_event","vk":162,"down":false],
                            ["op":"mouse_move","x":0.5,"y":0.25], ["op":"disconnect"]] { session.send(message) }
                    }
                }
                if mode == "clipboard", object["op"] as? String == "clipboard_sync" {
                    precondition(object["requestId"] as? Int == 1 && object["enabled"] as? Bool == true)
                    session.send(["op":"clipboard_text","requestId":1,"encoding":"utf8-base64url","text":"TWFjIOKGkiBXaW5kb3dz"])
                }
                if mode == "clipboard", object["op"] as? String == "clipboard_text" {
                    precondition(object["text"] as? String == "V2luZG93cyDihpIgTWFj")
                    lock.lock(); textReceived = true; lock.unlock(); session.send(["op":"disconnect"])
                }
            }, frame: { _ in lock.lock(); frames += 1; lock.unlock() }, ended: { error in
                lock.lock(); endings += 1; failure = error; lock.unlock(); finished.signal()
            })
        session.start(); session.start() // Starting twice must not create two sockets/workers.
        precondition(finished.wait(timeout: .now()+18) == .success, "Session did not terminate")
        session.stop(); session.send(["op":"key_event","vk":65,"down":true])
        Thread.sleep(forTimeInterval: 0.05)
        lock.lock(); defer { lock.unlock() }
        precondition(endings == 1)
        if mode == "reject" { precondition(connections == 0 && failure is ControllerError && frames == 0) }
        else if mode == "auth-eof" { if case WireError.authentication? = failure as? WireError {} else { fatalError("Rejected authentication not classified") } }
        else { precondition(connections == 1); if mode == "session" { precondition(frames == 1) }; if mode == "clipboard" { precondition(textReceived) } }
        print("PASS production ControllerSession: \(mode); one start/end, authenticated gate, ordered writer and interruptible stop (decoder adapter)")
    }
}
