import Foundation

// Platform-independent state machine; all calls are serialized by its adapter.
final class TextClipboardSync {
    static let maxBytes = 32 * 1024
    static let maxBase64 = ((maxBytes + 2) / 3) * 4
    private let revision: () -> Int
    private let read: () -> (Bool, String?)
    private let write: (String) -> Bool
    private let now: () -> Double
    private var observed = 0, pendingRevision = 0
    private var pending: String?, expires = 0.0
    private(set) var enabled = false
    init(revision: @escaping () -> Int, read: @escaping () -> (Bool, String?), write: @escaping (String) -> Bool,
         now: @escaping () -> Double = { ProcessInfo.processInfo.systemUptime }) {
        self.revision = revision; self.read = read; self.write = write; self.now = now
    }
    func setEnabled(_ enabled: Bool) { self.enabled = enabled; pending = nil; observed = revision() }
    static func encode(_ text: String) -> String? {
        // Do not scan or encode an arbitrarily large local clipboard on main.
        let bytes = Array(text.utf8.prefix(maxBytes + 1))
        guard bytes.count <= maxBytes, !bytes.contains(0) else { return nil }
        return Data(bytes).base64EncodedString().replacingOccurrences(of: "+", with: "-").replacingOccurrences(of: "/", with: "_")
    }
    static func decode(_ value: String) -> String? {
        let base64 = value.replacingOccurrences(of: "-", with: "+").replacingOccurrences(of: "_", with: "/")
        guard value.utf8.count <= maxBase64, let data = Data(base64Encoded: base64), data.count <= maxBytes,
              let text = String(data: data, encoding: .utf8), Self.encode(text) == value else { return nil }
        return text
    }
    func poll() -> String? {
        guard enabled else { return nil }
        let current = revision()
        if let text = pending {
            if current != pendingRevision || now() >= expires { pending = nil }
            else if write(text) { pending = nil; observed = revision(); return nil }
            else { return nil }
        }
        guard current != observed else { return nil }
        let (success, text) = read()
        guard success, current == revision() else { return nil }
        observed = current
        return text.flatMap { Self.encode($0) }
    }
    @discardableResult func receive(_ value: String) -> Bool {
        guard enabled, let text = Self.decode(value) else { return false }
        pending = nil
        if write(text) { observed = revision() }
        else { pending = text; pendingRevision = revision(); expires = now() + 2 }
        return true
    }
}
