import AppKit
import Foundation

// Pasteboard access and state live on main; socket writes stay on background
// queues. Dispatch timer events coalesce while a slow send is in progress.
final class MacClipboard {
    private let wire: Wire
    private let allowed: () -> Bool
    private let output = DispatchQueue(label: "localremote.clipboard")
    private var timer: DispatchSourceTimer?
    private var sync: TextClipboardSync!
    private var requested = false, requestID = 0, stopped = false, advertised = false
    init(wire: Wire, allowed: @escaping () -> Bool) {
        self.wire = wire; self.allowed = allowed
        DispatchQueue.main.sync {
            let board = NSPasteboard.general
            self.sync = TextClipboardSync(revision: { board.changeCount }, read: { (true, board.types?.contains(.fileURL) == true ? nil : board.string(forType: .string)) },
                write: { text in board.clearContents(); return board.setString(text, forType: .string) })
        }
        let source = DispatchSource.makeTimerSource(queue: output)
        source.schedule(deadline: .now() + .milliseconds(500), repeating: .milliseconds(500))
        source.setEventHandler { [weak self] in
            guard let self else { return }
            let message: [String: Any]? = DispatchQueue.main.sync {
                guard !self.stopped else { return nil }
                _ = self.refresh()
                if self.advertised != self.sync.enabled { return self.ack() }
                guard let text = self.sync.poll() else { return nil }
                return ["op":"clipboard_text", "requestId":self.requestID, "encoding":"utf8-base64url", "text":text]
            }
            if let message { do { try self.wire.json(message) } catch { self.wire.close() } }
        }
        timer = source; source.resume()
    }
    private func refresh() -> Bool {
        let enabled = !stopped && requested && allowed()
        guard enabled != sync.enabled else { return false }
        sync.setEnabled(enabled); return true
    }
    private func ack() -> [String: Any] {
        advertised = sync.enabled
        return ["op":"clipboard_sync", "enabled":sync.enabled, "requestId":requestID]
    }
    func configure(_ message: [String: Any]) throws {
        guard let enabled = message["enabled"] as? Bool, let id = message["requestId"] as? Int, id > 0 else { return }
        try output.sync {
            let response: [String: Any]? = DispatchQueue.main.sync {
                guard !stopped, id > requestID else { return nil }
                requested = enabled; requestID = id
                sync.setEnabled(false); _ = refresh()
                return ack()
            }
            if let response { try wire.json(response) }
        }
    }
    func receive(_ message: [String: Any]) {
        guard message["encoding"] as? String == "utf8-base64url", let text = message["text"] as? String,
              let id = message["requestId"] as? Int else { return }
        DispatchQueue.main.sync {
            _ = refresh()
            guard !stopped, id == requestID else { return }
            _ = sync.receive(text)
        }
    }
    func stop() {
        timer?.cancel(); timer = nil
        DispatchQueue.main.sync { stopped = true; sync.setEnabled(false) }
    }
}
