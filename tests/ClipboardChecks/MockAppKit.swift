// Linux-only adapter fixture. Does not validate Apple AppKit or touch a real clipboard.
import Foundation
public final class NSWorkspace {public static let shared = NSWorkspace(); @discardableResult public func open(_ url: URL) -> Bool {true}}
public final class NSPasteboard {
    public struct PasteboardType: Equatable { let id: Int; public static let string = PasteboardType(id: 1); public static let fileURL = PasteboardType(id: 2) }
    public var types: [PasteboardType]? { !urls.isEmpty ? [.fileURL] : value == nil ? [] : [.string] }
    public struct ReadingOptionKey: Hashable { public static let urlReadingFileURLsOnly = ReadingOptionKey() }
    public static let general = NSPasteboard()
    public private(set) var changeCount = 0
    private var value: String?
    private var urls: [NSURL] = []
    public func readObjects(forClasses: [AnyClass], options: [ReadingOptionKey:Any]?) -> [Any]? { urls.isEmpty ? nil : urls }
    public func writeObjects(_ objects: [NSURL]) -> Bool { urls = objects; return true }
    public func string(forType: PasteboardType) -> String? { value }
    @discardableResult public func clearContents() -> Int { value = nil; urls = []; changeCount += 1; return changeCount }
    public func setString(_ text: String, forType: PasteboardType) -> Bool { value = text; return true }
}
