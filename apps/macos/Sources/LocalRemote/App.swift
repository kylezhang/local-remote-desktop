import SwiftUI
import Combine
import AppKit
import ApplicationServices
import ScreenCaptureKit
private enum AppBrand {
    static let icon: NSImage? = {
        for ext in ["png", "icns"] {
            if let url = Bundle.main.url(forResource: "LocalRemote", withExtension: ext),
               let image = NSImage(contentsOf: url), image.isValid { return image }
        }
        return nil
    }()
    static func applyDockIcon() {
        if let icon { NSApplication.shared.applicationIconImage = icon }
    }
}
@main
struct LocalRemoteApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var delegate
    var body: some Scene { WindowGroup("LocalRemote") { HostView(model: delegate.model) }.windowResizability(.contentSize) }
}
final class AppDelegate: NSObject, NSApplicationDelegate {
    let model = HostModel()
    func applicationDidFinishLaunching(_ notification: Notification) {
        // A LaunchAgent starts the executable directly, independently of Finder.
        // Resolve duplicate GUI instances before either one accesses Keychain.
        let current = ProcessInfo.processInfo.processIdentifier
        if let first = NSRunningApplication.runningApplications(withBundleIdentifier: "com.remotemaclan.host")
            .filter({ !$0.isTerminated }).min(by: { $0.processIdentifier < $1.processIdentifier }), first.processIdentifier < current {
            first.activate(options: [.activateIgnoringOtherApps])
            NSApplication.shared.terminate(nil); return
        }
        // LaunchAgents run the executable directly. Set the running Dock icon
        // explicitly as well as supplying CFBundleIconFile for Finder.
        AppBrand.applyDockIcon()
        model.start()
    }
    func applicationWillTerminate(_ notification: Notification) { model.stop() }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
}
final class HostModel: ObservableObject {
    @Published var status = "准备启动"
    @Published var secret = ""
    @Published var active = false
    @Published var starting = false
    @Published var screenCheck = "尚未检查"
    @Published var keychainCheck = "尚未读取"
    // Avoid SwiftUI.State's SDK macro dependency in Command Line Tools builds.
    @Published var revealSecret = false
    @Published var allowClipboard = false { didSet { host?.setClipboardAllowed(allowClipboard) } }
    private var host: Host?
    private let preparation = SharingPreparation<Data>()
    private var pairingKey: Data? { preparation.cachedKey }
    private var startup: Task<Void, Never>?
    private let inputSelfTest = InputSelfTest()
    @Published var inputTestResult = ""
    func testInput() { inputSelfTest.run { [weak self] result in self?.inputTestResult = result } }
    func openAccessibilitySettings() {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(options)
        NSWorkspace.shared.open(URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")!)
    }
    init() {}
    func retryPairing() {
        guard host == nil, !starting, pairingKey == nil else { return }
        starting = true; status = "正在读取配对密钥…"
        startup = Task { @MainActor [weak self] in
            guard let self else { return }
            defer { self.starting = false }
            do {
                let key = try await self.preparation.readKey { try await self.fetchPairingKey(allowInteraction: true) }
                self.displayPairingKey(key)
                self.status = "配对密钥已就绪。点击开始共享后检查屏幕权限。"
            } catch is CancellationError { }
            catch { self.reportKeychainError(error) }
        }
    }
    @MainActor private func fetchPairingKey(allowInteraction: Bool) async throws -> Data {
        keychainCheck = allowInteraction ? "用户主动读取" : "启动静默读取"
        return try await Pairing.load(allowInteraction: allowInteraction)
    }
    private func displayPairingKey(_ key: Data) {
        secret = key.map { String(format: "%02x", $0) }.joined()
        keychainCheck = "读取成功（本次运行已缓存）"
    }
    private func reportKeychainError(_ error: Error) {
        let e = error as NSError
        keychainCheck = "\(e.domain) / \(e.code)"
        status = "无法读取配对密钥：\(e.localizedDescription)。不会自动重复请求；需要时点击读取配对密钥。"
    }
    func start(requestPermission: Bool = false) {
        guard host == nil, !starting else { return }
        if !requestPermission && pairingKey == nil && !Pairing.permitsAutomaticRead {
            keychainCheck = "等待用户操作（启动未访问钥匙串）"
            status = "点击开始共享。会先检查屏幕权限，再读取配对密钥。"
            return
        }
        starting = true; status = "正在检查屏幕访问…"
        startup = Task { @MainActor [weak self] in
          guard let self else { return }
          defer { self.starting = false }
          // Explicit sharing checks ScreenCaptureKit even if the legacy
          // preflight reports false. Login must not request screen permission.
          if !requestPermission && !CGPreflightScreenCaptureAccess() {
            self.status = "尚未启动：点击开始共享，检查屏幕访问。授权后只需退出重开，不要重新安装。"
            return
          }
          var checkingScreen = true
          do {
            let key = try await self.preparation.prepare(checkScreen: {
                self.status = "正在通过 ScreenCaptureKit 检查屏幕访问…"
                self.screenCheck = "ScreenCaptureKit 请求中"
                let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)
                guard !content.displays.isEmpty else { throw CaptureError.noDisplay }
                self.screenCheck = "ScreenCaptureKit 成功：\(content.displays.count) 个显示器"
            }, readKey: {
                checkingScreen = false
                self.status = "正在读取配对密钥…"
                return try await self.fetchPairingKey(allowInteraction: requestPermission)
            })
            // prepare can reuse an already cached key without invoking readKey.
            checkingScreen = false
            self.displayPairingKey(key)
            let trusted = AXIsProcessTrusted()
            let h = Host(key: key, clipboardAllowed: self.allowClipboard) { [weak self] text in DispatchQueue.main.async { self?.status = text } }
            try h.start(); self.host = h; self.active = true
            self.status = trusted ? "共享已启动，等待 Windows 连接 · TCP 47980" : "共享已启动（只能查看）· 辅助功能权限未被认可"
          } catch is CancellationError { }
          catch {
            let e = error as NSError
            if checkingScreen {
                self.screenCheck = "失败：\(e.domain) / \(e.code)"
                self.status = "屏幕访问失败：\(e.domain) / \(e.code)：\(e.localizedDescription)\n本次没有读取钥匙串。授权后退出重开，不要再次安装或重置。"
            } else if self.pairingKey == nil { self.reportKeychainError(error) }
            else { self.status = "共享启动失败：\(e.domain) / \(e.code)：\(e.localizedDescription)" }
          }
        }
    }
    func stop() { startup?.cancel(); host?.stop(); host = nil; active = false; status = "已停止" }
    func copyDiagnostics() {
        let text = """
        LocalRemote \(Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "unknown")
        Build: \(Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? "unknown")
        macOS: \(ProcessInfo.processInfo.operatingSystemVersionString)
        InstalledAtApplications: \(Bundle.main.bundlePath == "/Applications/LocalRemote.app")
        AppIconLoaded: \(AppBrand.icon != nil)
        BuildSigningMode: \(Pairing.buildSigningMode)
        LegacyScreenPreflight: \(CGPreflightScreenCaptureAccess())
        ScreenCaptureKit: \(screenCheck)
        Accessibility: \(AXIsProcessTrusted())
        Listening: \(active)
        Starting: \(starting)
        Keychain: \(keychainCheck)
        KeychainBackend: file-based (existing pairing-v1 item)
        KeychainReadAttempts: \(Pairing.readAttempts)
        KeychainCreateAttempts: \(Pairing.createAttempts)
        ClipboardAllowed: \(allowClipboard)
        Status: \(status)
        """
        NSPasteboard.general.clearContents(); NSPasteboard.general.setString(text, forType: .string)
    }
}
struct HostView: View {
    @ObservedObject var model: HostModel
    private let blueberry = Color(red: 82.0 / 255.0, green: 101.0 / 255.0, blue: 236.0 / 255.0)
    private let brandText = Color(nsColor: NSColor(name: nil) { appearance in
        if appearance.bestMatch(from: [.darkAqua, .aqua]) == .darkAqua {
            return NSColor(srgbRed: 177.0 / 255.0, green: 183.0 / 255.0, blue: 1, alpha: 1)
        }
        return NSColor(srgbRed: 40.0 / 255.0, green: 44.0 / 255.0, blue: 103.0 / 255.0, alpha: 1)
    })
    private var brandIcon: NSImage? { AppBrand.icon }
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack(spacing: 12) {
                if let icon = brandIcon {
                    Image(nsImage: icon).resizable().interpolation(.high).scaledToFit()
                        .frame(width: 60, height: 60).accessibilityHidden(true)
                }
                Text("LocalRemote 0.1.0").font(.title)
                    .foregroundStyle(brandText)
            }
            Text(model.status).textSelection(.enabled).frame(width: 450, alignment: .leading).fixedSize(horizontal: false, vertical: true)
            Text("在 Windows 填写此 Mac 的局域网 IP 和配对密钥。当前桌面会被共享；停止共享即可收回控制。").frame(width: 450, alignment: .leading)
            Toggle("显示配对密钥（仅交给自己的 Windows）", isOn: $model.revealSecret)
            Toggle("允许双向文本剪贴板同步", isOn: $model.allowClipboard)
            Text("两端开启后同步新复制的纯文本，最多 32 KiB；可能包含敏感内容。不传图片和文件。")
                .font(.caption).foregroundStyle(.secondary).frame(width: 450, alignment: .leading)
            if model.revealSecret {
                Text(model.secret.isEmpty ? "密钥尚未读取，可点击下方按钮。" : model.secret)
                    .font(.system(.caption, design: .monospaced)).textSelection(.enabled).frame(width: 450)
                Button("复制配对密钥") { NSPasteboard.general.clearContents(); NSPasteboard.general.setString(model.secret, forType: .string) }.disabled(model.secret.isEmpty)
                if model.secret.isEmpty { Button("读取配对密钥") { model.retryPairing() }.disabled(model.starting) }
            }
            HStack {
                Button(model.starting ? "正在检查…" : model.active ? "停止共享" : "开始共享") { if model.active { model.stop() } else { model.start(requestPermission: true) } }.buttonStyle(.borderedProminent).disabled(model.starting)
                Button("屏幕权限") { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button("辅助功能权限") { model.openAccessibilitySettings() }
                Button("退出") { NSApplication.shared.terminate(nil) }
            }
            Button("本机键盘自检") { model.testInput() }
            Button("复制启动诊断") { model.copyDiagnostics() }
            if !model.inputTestResult.isEmpty { Text(model.inputTestResult).font(.caption).frame(width: 450, alignment: .leading).textSelection(.enabled) }
            Text("仅支持正常登录后的用户会话；不支持 FileVault 预启动解锁。\n端口 47980；不传输音频、不写录像文件。").font(.caption).foregroundStyle(.secondary)
        }.padding(24).tint(blueberry)
    }
}
