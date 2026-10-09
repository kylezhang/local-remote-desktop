// Linux-only adapter fixture. Does not validate Apple AppKit or touch a real clipboard.
public final class NSPasteboard {
    public struct PasteboardType { public static let string = PasteboardType() }
    public static let general = NSPasteboard()
    public private(set) var changeCount = 0
    private var value: String?
    public func string(forType: PasteboardType) -> String? { value }
    @discardableResult public func clearContents() -> Int { value = nil; changeCount += 1; return changeCount }
    public func setString(_ text: String, forType: PasteboardType) -> Bool { value = text; return true }
}
