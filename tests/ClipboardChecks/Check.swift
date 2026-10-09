import Foundation
func check(_ value: @autoclosure () -> Bool, _ message: String) { if !value() { fatalError(message) } }
final class Board {
    var revision = 0, text: String?, reads = 0, busyRead = false, busyWrite = false
    func copy(_ value: String?) { text = value; revision += 1 }
    func write(_ value: String) -> Bool { if busyWrite { return false }; copy(value); return true }
    func sync(_ now: @escaping () -> Double = { 0 }) -> TextClipboardSync {
        TextClipboardSync(revision: { self.revision }, read: { self.reads += 1; return (!self.busyRead, self.text) }, write: { self.write($0) }, now: now)
    }
}
@main struct Check {
    static func main() throws {
        for text in ["", "hello", "中文 🎉\r\nsecond line\t", String(repeating: "?", count: 32768), String(repeating: "\u{ffff}", count: 10922)] {
            let payload = TextClipboardSync.encode(text)!
            check(TextClipboardSync.decode(payload) == text, "roundtrip")
            let message: [String: Any] = ["op":"clipboard_text", "requestId":Int.max, "encoding":"utf8-base64url", "text":payload]
            let size = try JSONSerialization.data(withJSONObject: message).count
            check(size + 17 <= 65536, "JSON record limit")
        }
        check(TextClipboardSync.encode(String(repeating: "a", count: 32769)) == nil, "size")
        check(TextClipboardSync.encode(String(repeating: "中", count: 10923)) == nil, "UTF8 size")
        check(TextClipboardSync.encode("a\0b") == nil, "NUL")
        for bad in ["!", "Zg=", "Zg==\n", "/w==", "_w==", "wK8=", "AA==", String(repeating: "A", count: TextClipboardSync.maxBase64 + 4)] {
            check(TextClipboardSync.decode(bad) == nil, "malformed payload")
        }
        print("PASS Swift Unicode/empty text, strict UTF8/base64url, byte limits and JSON record fit")
        var time = 0.0; let board = Board(); board.copy("existing private text"); let sync = board.sync { time }
        check(sync.poll() == nil && !sync.receive("aGVsbG8="), "default off")
        sync.setEnabled(true); check(sync.poll() == nil && board.reads == 0, "enable baseline")
        board.copy("new text"); board.busyRead = true; check(sync.poll() == nil, "busy read")
        board.busyRead = false; check(TextClipboardSync.decode(sync.poll()!) == "new text" && sync.poll() == nil, "new copy")
        check(sync.receive(TextClipboardSync.encode("remote")!) && board.text == "remote" && sync.poll() == nil, "echo")
        board.copy(nil); check(sync.poll() == nil, "non-text")
        board.copy(String(repeating: "a", count: 32769)); check(sync.poll() == nil, "oversize")
        board.busyWrite = true; sync.receive(TextClipboardSync.encode("retry")!); _ = sync.poll()
        board.busyWrite = false; _ = sync.poll(); check(board.text == "retry" && sync.poll() == nil, "busy retry")
        board.busyWrite = true; sync.receive(TextClipboardSync.encode("stale")!); board.copy("local wins"); board.busyWrite = false
        check(TextClipboardSync.decode(sync.poll()!) == "local wins" && board.text == "local wins", "local priority")
        board.busyWrite = true; sync.receive(TextClipboardSync.encode("expired")!); time = 2.001; _ = sync.poll(); board.busyWrite = false; _ = sync.poll()
        check(board.text == "local wins", "expiry")
        board.busyWrite = true; sync.receive(TextClipboardSync.encode("cancelled")!); sync.setEnabled(false); board.busyWrite = false; _ = sync.poll()
        check(board.text == "local wins", "disable")
        board.copy("offline secret"); sync.setEnabled(true); check(sync.poll() == nil, "offline copy")
        print("PASS Swift consent baseline, non-text/oversize skip, no echo, busy recovery, local priority, expiry and disconnect")
        let left = Board(), right = Board(); let a = left.sync(), b = right.sync(); a.setEnabled(true); b.setEnabled(true)
        left.copy("Windows → Mac 中文"); check(b.receive(a.poll()!) && right.text == left.text, "forward")
        right.copy("Mac → Windows emoji 😀"); check(a.receive(b.poll()!) && left.text == right.text, "reverse")
        for _ in 0..<200 { check(a.poll() == nil && b.poll() == nil, "feedback loop") }
        print("PASS Swift both directions settle without a feedback loop (200 polls)")
    }
}
