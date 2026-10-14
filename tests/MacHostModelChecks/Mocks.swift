import Foundation

// Portable service doubles, not substitutes for compiling against Apple SDKs.
protocol ObservableObject {}
@propertyWrapper struct Published<Value> {
    var wrappedValue: Value
    init(wrappedValue: Value) { self.wrappedValue = wrappedValue }
}
typealias CFDictionary = [String: Any]
struct AXPrompt { func takeUnretainedValue() -> String { "prompt" } }
let kAXTrustedCheckOptionPrompt = AXPrompt()
func AXIsProcessTrusted() -> Bool { true }
func AXIsProcessTrustedWithOptions(_ options: CFDictionary) -> Bool { true }
func CGPreflightScreenCaptureAccess() -> Bool { true }
final class NSWorkspace {
    static let shared = NSWorkspace()
    func open(_ url: URL) {}
}
final class NSPasteboard {
    enum PasteboardType { case string }
    static let general = NSPasteboard()
    var text = ""
    func clearContents() { text = "" }
    @discardableResult func setString(_ value: String, forType: PasteboardType) -> Bool { text = value; return true }
}
enum CaptureError: Error { case noDisplay }
enum ScreenSharingError: Error, Equatable { case chooseMainDisplay, selectionUnavailable }
struct ScreenCaptureSource {}
final class ScreenSharingAccess {
    static var attempts = 0, reject = false, pause = false
    static var pending: CheckedContinuation<ScreenCaptureSource, Error>?
    var cached: ScreenCaptureSource?
    let mode = "recording system picker"
    func select(interactive: Bool) async throws -> ScreenCaptureSource {
        if let cached { return cached }
        Self.attempts += 1
        if Self.reject { throw NSError(domain:"com.apple.ScreenCaptureKit.SCStreamErrorDomain", code:-3801) }
        let selected: ScreenCaptureSource
        if Self.pause { selected = try await withCheckedThrowingContinuation { Self.pending = $0 } }
        else { selected = ScreenCaptureSource() }
        try Task.checkCancellation(); cached = selected; return selected
    }
    func cancelPending() { Self.pending?.resume(throwing:CancellationError()); Self.pending = nil }
    func invalidate() { cached = nil; cancelPending() }
}
struct SCShareableContent {
    let displays = [1]
    static func excludingDesktopWindows(_ exclude: Bool, onScreenWindowsOnly: Bool) async throws -> Self { Self() }
}
enum AppBrand { static var icon: Int? { nil } }
enum SharingPreferences { static func enabled(_ key: String) -> Bool { true } }
enum MacFiles { static var receivedDirectory: URL { FileManager.default.temporaryDirectory } }
final class InputSelfTest { func run(_ done: (String) -> Void) { done("test") } }
final class HostDiscovery {
    static var starts = 0, stops = 0
    static var lastName = ""
    var state = "stopped"
    func start(name: String = DeviceName.current()) { Self.starts += 1; Self.lastName = name; state = "published" }
    func stop() { Self.stops += 1; state = "stopped" }
}
func randomBytes(_ count: Int) -> Data { Data((0..<count).map { UInt8($0) }) }
enum StoreFailure: Error { case rejected }
enum Pairing {
    static let buildSigningMode = "ad-hoc"
    static let permitsAutomaticRead = false
    static var readAttempts = 0, createAttempts = 0, saveAttempts = 0
    static var rejectSave = false, pauseSave = false
    static var pendingSave: CheckedContinuation<Void, Never>?
    static func load(allowInteraction: Bool) async throws -> PairingCredential {
        precondition(allowInteraction); readAttempts += 1
        return try PairingCredential.decode(Data(repeating: 7, count: 32))
    }
    static func savePassword(_ text: String) async throws -> PairingCredential {
        saveAttempts += 1
        if pauseSave { await withCheckedContinuation { pendingSave = $0 } }
        if rejectSave { throw StoreFailure.rejected }
        return try PairingCredential.password(text, salt: Data(0..<16))
    }
}
final class Host {
    static var instances: [Host] = []
    let key: Data
    let passwordSalt: Data?
    let status: (String) -> Void, fileStatus: (String) -> Void
    private(set) var stopped = false
    let screenAccessEnded: () -> Void
    init(key: Data, deviceName: String = DeviceName.current(), screenSource: ScreenCaptureSource? = nil, screenAccessEnded: @escaping () -> Void = {}, passwordSalt: Data?, clipboardAllowed: Bool, filesAllowed: Bool,
         fileStatus: @escaping (String) -> Void, status: @escaping (String) -> Void) {
        self.key = key; self.passwordSalt = passwordSalt; self.status = status; self.fileStatus = fileStatus
        self.screenAccessEnded = screenAccessEnded
        Self.instances.append(self)
    }
    func start() throws {}
    func stop() { stopped = true }
    var deviceName = ""
    func setDeviceName(_ name: String) { deviceName = name }
    func setClipboardAllowed(_ allowed: Bool) {}
    func setFilesAllowed(_ allowed: Bool) {}
    func cancelFiles() {}
}
