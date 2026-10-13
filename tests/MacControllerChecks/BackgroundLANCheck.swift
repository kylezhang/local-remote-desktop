import Foundation
import CoreGraphics
import Darwin

enum InteropFailure: Error { case failed(String) }

// Companion to ClientChecks' explicit LAN_BACKGROUND_CHECK. Never run against
// a user's active host. Files are disposable; clipboard/input are recordings.
@main enum BackgroundLANCheck {
    static func require(_ ok: Bool, _ message: String) throws { if !ok { throw InteropFailure.failed(message) } }
    static func wait(_ message: String, _ check: () -> Bool) throws {
        let limit = Date().addingTimeInterval(20)
        while !check() { if Date() > limit { throw InteropFailure.failed(message) }; Thread.sleep(forTimeInterval: 0.02) }
    }
    static func fileWire(_ address: String, _ port: UInt16) throws -> Wire {
        let fd = Darwin.socket(AF_INET, SOCK_STREAM, 0)
        guard fd >= 0 else { throw WireError.closed }
        let wire = Wire(fd, controller: true, fileChannel: true)
        do { try wire.connect(address: address, port: port); try wire.authenticateController(secret: "Fixture1"); return wire }
        catch { wire.close(); throw error }
    }
    static func main() throws {
        let args = CommandLine.arguments
        guard args.count == 3, let port = UInt16(args[2]) else { throw InteropFailure.failed("Expected private fixture address and port") }
        let address = try ControllerRules.address(args[1])
        let root = FileManager.default.temporaryDirectory.appendingPathComponent("lrd-background-lan-"+UUID().uuidString,isDirectory: true)
        try FileManager.default.createDirectory(at: root,withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: root) }
        let binary = Data((0..<(512*1024)).map { UInt8($0 % 251) })
        for quality in 0...2 {
            let lock = NSLock(), ended = DispatchSemaphore(value: 0)
            var frames = 0, caps = [String: Any](), messages = [[String: Any]](), failure: Error?
            let session = ControllerSession(address: address,password: "Fixture1",quality: quality,port: port,
                status: { message in lock.lock(); defer { lock.unlock() }
                    if message["op"] as? String == "capabilities" { caps = message }; messages.append(message)
                }, frame: { image in precondition(image.width == 320 && image.height == 180); lock.lock(); frames += 1; lock.unlock() },
                ended: { error in lock.lock(); failure = error; lock.unlock(); ended.signal() })
            func snapshot() -> (Int,[String:Any],[[String:Any]],Error?) { lock.lock(); defer { lock.unlock() }; return (frames,caps,messages,failure) }
            session.start()
            do {
                try wait("Native frames not decoded") { snapshot().0 >= 3 }
                try require(snapshot().1["deviceName"] as? String == "Fixture Windows", "Authenticated name differs")
                if quality == 0 {
                    for code: UInt16 in [0,18,83] {
                        let vk = ControllerKeys.vk(code,shortcuts: true)!
                        session.send(["op":"key_event","vk":vk,"down":true]); session.send(["op":"key_event","vk":vk,"down":false])
                    }
                    session.send(["op":"mouse_move","x":0.5,"y":0.5])
                    session.send(["op":"mouse_button","button":0,"down":true,"x":0.5,"y":0.5])
                    session.send(["op":"mouse_button","button":0,"down":false,"x":0.5,"y":0.5])
                    session.send(["op":"scroll","delta":120])
                    session.send(["op":"clipboard_sync","requestId":1,"enabled":true])
                    session.send(["op":"file_sync","requestId":1,"enabled":true])
                    try wait("Text/file consent missing") {
                        let list = snapshot().2
                        return ["clipboard_sync","file_sync"].allSatisfy { op in list.contains { $0["op"] as? String == op && $0["enabled"] as? Bool == true } }
                    }
                    session.send(["op":"clipboard_text","requestId":1,"encoding":"utf8-base64url","text":TextClipboardSync.encode("Mac to Windows 中文 🎉")!])
                    try wait("Return text differs") { snapshot().2.contains { $0["op"] as? String == "clipboard_text" && ($0["text"] as? String).flatMap(TextClipboardSync.decode) == "Windows to Mac 中文 🎉" } }
                    let capabilities = snapshot().1
                    guard let identity = capabilities["fileSession"] as? String, let filePort = capabilities["filePort"] as? Int,
                          let channelPort = UInt16(exactly: filePort) else { throw InteropFailure.failed("File capabilities missing") }
                    let paths = ["Mac-中文.txt","empty.txt","binary.bin"].map { root.appendingPathComponent($0).path }
                    try Data("Mac file 中文 🎉".utf8).write(to: URL(fileURLWithPath: paths[0])); try Data().write(to: URL(fileURLWithPath: paths[1])); try binary.write(to: URL(fileURLWithPath: paths[2]))
                    let source = try FileSource(paths: paths)
                    let push = try fileWire(address,channelPort); defer { push.close() }
                    try push.json(["op":"file_push","session":identity,"requestId":1,"items":FileTransport.metadata(source.items)])
                    try FileTransport.expect(push.receive(),"file_ready"); try FileTransport.send(source,wire: push)
                    let done = try push.receive(); try FileTransport.expect(done,"file_done"); try require(done["clipboardReady"] as? Bool == true,"Recorded host clipboard not ready")
                    push.close()
                    try wait("Return files not offered") { snapshot().2.contains { $0["op"] as? String == "file_offer" } }
                    let offer = snapshot().2.first { $0["op"] as? String == "file_offer" }!
                    let pull = try fileWire(address,channelPort); defer { pull.close() }
                    try pull.json(["op":"file_pull","session":identity,"requestId":1,"offer":offer["offer"]!])
                    let ready = try pull.receive(); try FileTransport.expect(ready,"file_ready")
                    let items = try FileTransport.items(ready["items"])
                    let received = try FileTransport.receive(items,root: root.appendingPathComponent("received"),wire: pull)
                    try require(received.count == 3,"Return file count differs")
                    try require(try Data(contentsOf: URL(fileURLWithPath: received[0])) == Data("Windows file 中文 🎉".utf8),"Return Unicode file differs")
                    try require(try Data(contentsOf: URL(fileURLWithPath: received[1])).isEmpty,"Return empty file differs")
                    try require(try Data(contentsOf: URL(fileURLWithPath: received[2])) == binary,"Return binary differs")
                    try pull.json(["op":"file_done","clipboardReady":true]); pull.close()
                    session.send(["op":"clipboard_text","requestId":1,"encoding":"utf8-base64url","text":TextClipboardSync.encode("interop complete")!])
                    try wait("Input acknowledgement missing") { snapshot().2.contains { ($0["inputPosted"] as? Int ?? 0) >= 10 } }
                    session.send(["op":"release_all"])
                }
                session.stop(); try require(ended.wait(timeout: .now()+5) == .success,"Stop did not settle")
                try require(snapshot().3 == nil,"Session failed")
                print("PASS native cross-device quality \(quality): \(snapshot().0) decoded synthetic frames; clean stop")
                Thread.sleep(forTimeInterval: 0.5)
            } catch { session.stop(); _ = ended.wait(timeout: .now()+5); throw error }
        }
        print("PASS native Mac -> production Windows LAN fixture; Unicode text/files in both directions, recorded inputs; no OS input, capture, pasteboard or Keychain")
    }
}
