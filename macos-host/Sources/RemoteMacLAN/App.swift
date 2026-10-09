import SwiftUI
import Combine
import AppKit
import ApplicationServices
import ScreenCaptureKit
@main
struct RemoteMacLANApp: App {
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
    // Avoid SwiftUI.State's SDK macro dependency in Command Line Tools builds.
    @Published var revealSecret = false
    private var host: Host?
    private var pairingKey: Data?
    private var pairingAttempted = false
    private var startup: Task<Void, Never>?
    private let inputSelfTest = InputSelfTest()
    @Published var inputTestResult = ""
    func testInput() { inputSelfTest.run { [weak self] result in self?.inputTestResult = result } }
    init() {}
    private func loadPairingKey() {
        guard !pairingAttempted else { return }
        pairingAttempted = true
        do {
            let key = try Pairing.load(); pairingKey = key
            secret = key.map { String(format:"%02x", $0) }.joined()
        } catch { status = "无法读取 Keychain 配对密钥：\(error.localizedDescription)。不会自动重复请求；需要时点击重试读取密钥。" }
    }
    func retryPairing() {
        guard host == nil, !starting, pairingKey == nil else { return }
        pairingAttempted = false; start(requestPermission: true)
    }
    func start(requestPermission: Bool = false) {
        guard host == nil, !starting else { return }
        if pairingKey == nil { loadPairingKey() }
        guard let key = pairingKey else { return }
        // Do not trigger prompts on login. On an explicit click, ask the actual
        // ScreenCaptureKit API even if the legacy CG preflight reports false.
        if !requestPermission && !CGPreflightScreenCaptureAccess() {
            status = "尚未启动：点击开始共享，检查屏幕访问。授权后只需退出重开，不要重新安装。"
            return
        }
        starting = true; status = "正在通过 ScreenCaptureKit 检查屏幕访问…"
        screenCheck = "ScreenCaptureKit 请求中"
        startup = Task { @MainActor [weak self] in
          guard let self else { return }
          defer { self.starting = false }
          do {
            let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)
            try Task.checkCancellation()
            guard !content.displays.isEmpty else { throw CaptureError.noDisplay }
            self.screenCheck = "ScreenCaptureKit 成功：\(content.displays.count) 个显示器"
            let trusted = AXIsProcessTrusted()
            let h = Host(key: key) { [weak self] text in DispatchQueue.main.async { self?.status = text } }
            try h.start(); self.host = h; self.active = true
            self.status = trusted ? "共享已启动，等待 Windows 连接 · TCP 47980" : "共享已启动（只能查看）· 辅助功能权限未被认可"
          } catch is CancellationError { }
          catch {
            let e = error as NSError
            self.screenCheck = "失败：\(e.domain) / \(e.code)"
            self.status = "启动失败：\(e.domain) / \(e.code)：\(e.localizedDescription)\n若刚授予屏幕录制权限，请退出重开应用，不要再次安装或重置。"
          }
        }
    }
    func stop() { startup?.cancel(); host?.stop(); host = nil; active = false; status = "已停止" }
    func copyDiagnostics() {
        let text = """
        LocalRemote \(Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "unknown")
        macOS: \(ProcessInfo.processInfo.operatingSystemVersionString)
        InstalledAtApplications: \(Bundle.main.bundlePath == "/Applications/RemoteMacLAN.app")
        LegacyScreenPreflight: \(CGPreflightScreenCaptureAccess())
        ScreenCaptureKit: \(screenCheck)
        Accessibility: \(AXIsProcessTrusted())
        Listening: \(active)
        Starting: \(starting)
        KeychainReadAttempts: \(Pairing.readAttempts)
        KeychainCreateAttempts: \(Pairing.createAttempts)
        Status: \(status)
        """
        NSPasteboard.general.clearContents(); NSPasteboard.general.setString(text, forType: .string)
    }
}
struct HostView: View {
    @ObservedObject var model: HostModel
    private let cherry = Color(red: 161.0 / 255.0, green: 0, blue: 53.0 / 255.0)
    private var brandIcon: NSImage? {
        guard let url = Bundle.main.url(forResource: "LocalRemote", withExtension: "icns") else { return nil }
        return NSImage(contentsOf: url)
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack(spacing: 12) {
                if let icon = brandIcon {
                    Image(nsImage: icon).resizable().interpolation(.high).scaledToFit()
                        .frame(width: 60, height: 60).accessibilityHidden(true)
                }
                Text("LocalRemote 0.1.0").font(.title)
                    .foregroundStyle(Color(red: 75.0 / 255.0, green: 8.0 / 255.0, blue: 36.0 / 255.0))
            }
            Text(model.status).textSelection(.enabled).frame(width: 450, alignment: .leading).fixedSize(horizontal: false, vertical: true)
            Text("在 Windows 填写此 Mac 的局域网 IP 和配对密钥。当前桌面会被共享；停止共享即可收回控制。").frame(width: 450, alignment: .leading)
            Toggle("显示配对密钥（仅交给自己的 Windows）", isOn: $model.revealSecret)
            if model.revealSecret {
                Text(model.secret.isEmpty ? "密钥读取失败，请查看上方错误信息。" : model.secret)
                    .font(.system(.caption, design: .monospaced)).textSelection(.enabled).frame(width: 450)
                Button("复制配对密钥") { NSPasteboard.general.clearContents(); NSPasteboard.general.setString(model.secret, forType: .string) }.disabled(model.secret.isEmpty)
            }
            if model.secret.isEmpty { Button("重试读取密钥") { model.retryPairing() } }
            HStack {
                Button(model.starting ? "正在检查…" : model.active ? "停止共享" : "开始共享") { if model.active { model.stop() } else { model.start(requestPermission: true) } }.buttonStyle(.borderedProminent).disabled(model.starting)
                Button("打开权限设置") { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button("退出") { NSApplication.shared.terminate(nil) }
            }
            Button("本机键盘自检") { model.testInput() }
            Button("复制启动诊断") { model.copyDiagnostics() }
            if !model.inputTestResult.isEmpty { Text(model.inputTestResult).font(.caption).frame(width: 450, alignment: .leading).textSelection(.enabled) }
            Text("仅支持正常登录后的用户会话；不支持 FileVault 预启动解锁。\n端口 47980；不传输音频、不写录像文件。").font(.caption).foregroundStyle(.secondary)
        }.padding(24).tint(cherry)
    }
}
