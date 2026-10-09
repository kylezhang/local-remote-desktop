import AppKit
import Foundation
import Dispatch
final class Wire {
    private let lock = NSLock()
    private var records: [[String: Any]] = []
    func json(_ message: [String: Any]) throws { lock.lock(); records.append(message); lock.unlock() }
    func close() {}
    var messages: [[String: Any]] { lock.lock(); defer { lock.unlock() }; return records }
}
func require(_ condition: @autoclosure () -> Bool, _ text: String) { if !condition() { fatalError(text) } }
func until(_ condition: () -> Bool) {
    let deadline = Date().addingTimeInterval(3)
    while !condition() { require(Date() < deadline, "adapter timeout"); Thread.sleep(forTimeInterval: 0.02) }
}
func copy(_ text: String) { DispatchQueue.main.sync { NSPasteboard.general.clearContents(); _ = NSPasteboard.general.setString(text, forType: .string) } }
func current() -> String? { DispatchQueue.main.sync { NSPasteboard.general.string(forType: .string) } }
@main struct AdapterCheck {
    static func main() {
        DispatchQueue.global().async {
            do {
                let wire = Wire(); var allowed = false
                copy("existing private text")
                let session = MacClipboard(wire: wire, allowed: { allowed })
                try session.configure(["enabled":true,"requestId":1])
                require(wire.messages.last?["enabled"] as? Bool == false, "mutual consent")
                DispatchQueue.main.sync { allowed = true }
                until { wire.messages.last?["enabled"] as? Bool == true }
                require(!wire.messages.contains { $0["op"] as? String == "clipboard_text" }, "baseline leaked")
                copy("Mac → Windows 中文 😀")
                until { wire.messages.contains { $0["op"] as? String == "clipboard_text" } }
                let transfer = wire.messages.first { $0["op"] as? String == "clipboard_text" }!
                require(TextClipboardSync.decode(transfer["text"] as! String) == "Mac → Windows 中文 😀", "outgoing content")
                session.receive(["requestId":1,"encoding":"utf8-base64url","text":TextClipboardSync.encode("Windows → Mac 🎉")!])
                require(current() == "Windows → Mac 🎉", "incoming not applied before return/paste")
                Thread.sleep(forTimeInterval: 0.6)
                require(wire.messages.filter { $0["op"] as? String == "clipboard_text" }.count == 1, "echo")
                DispatchQueue.main.sync { allowed = false }
                // Revocation observed by receive must still be advertised by poll.
                session.receive(["requestId":1,"encoding":"utf8-base64url","text":TextClipboardSync.encode("forbidden")!])
                until { wire.messages.last?["enabled"] as? Bool == false }
                require(current() == "Windows → Mac 🎉", "revoked write")
                try session.configure(["enabled":true,"requestId":2])
                DispatchQueue.main.sync { allowed = true }
                until { wire.messages.last?["enabled"] as? Bool == true }
                session.receive(["requestId":1,"encoding":"utf8-base64url","text":TextClipboardSync.encode("stale")!])
                session.receive(["requestId":2,"encoding":"utf8-base64url","text":"_w=="])
                require(current() == "Windows → Mac 🎉", "stale/malformed write")
                try session.configure(["enabled":false,"requestId":3])
                try session.configure(["enabled":true,"requestId":2])
                require(wire.messages.last?["enabled"] as? Bool == false, "stale configuration")
                session.stop(); let count = wire.messages.count; copy("after disconnect")
                Thread.sleep(forTimeInterval: 0.6)
                require(wire.messages.count == count, "stopped timer sent text")
                print("PASS production Mac clipboard adapter with mocked AppKit/Wire: consent, baseline, directions, before-paste application, no echo, revoke notification, stale IDs, invalid text and stopped timer")
                print("Mock adapter result only; native AppKit/Apple SDK acceptance remains pending.")
                exit(0)
            } catch { fatalError("adapter check: \(error)") }
        }
        dispatchMain()
    }
}
