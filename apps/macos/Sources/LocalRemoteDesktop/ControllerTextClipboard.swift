import Foundation

// Main-thread state. Network output uses the session's bounded writer; old
// generations, disabled consent and unmatched acknowledgments cannot apply text.
final class ControllerTextClipboard {
    private let sync: TextClipboardSync, allowed: () -> Bool, send: ([String: Any]) -> Void
    private var active = false, supported = false, requested = false, requestID = 0
    init(sync: TextClipboardSync, allowed: @escaping () -> Bool, send: @escaping ([String: Any]) -> Void) {
        self.sync = sync; self.allowed = allowed; self.send = send
    }
    func start(supported: Bool) {
        stop(); active = true; self.supported = supported
        requested = !allowed(); refresh()
    }
    private func refresh() {
        guard active, supported else { return }
        let enabled = allowed()
        guard requested != enabled else { return }
        requested = enabled; sync.setEnabled(false); requestID += 1
        send(["op":"clipboard_sync", "enabled":enabled, "requestId":requestID])
    }
    func receive(_ message: [String: Any]) {
        refresh()
        guard active, supported, let id = message["requestId"] as? Int, id == requestID else { return }
        if message["op"] as? String == "clipboard_sync", let enabled = message["enabled"] as? Bool {
            sync.setEnabled(enabled && requested && allowed())
        } else if message["op"] as? String == "clipboard_text", message["encoding"] as? String == "utf8-base64url",
                  requested, allowed(), let text = message["text"] as? String { sync.receive(text) }
    }
    func poll() {
        refresh()
        guard active, supported, let text = sync.poll() else { return }
        send(["op":"clipboard_text", "requestId":requestID, "encoding":"utf8-base64url", "text":text])
    }
    func stop() { active = false; supported = false; sync.setEnabled(false) }
}
