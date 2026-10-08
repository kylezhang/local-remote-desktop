import SwiftUI
import Combine
import AppKit
import ApplicationServices
@main
struct RemoteMacLANApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var delegate
    var body: some Scene { WindowGroup("Remote Mac LAN") { HostView(model: delegate.model) }.windowResizability(.contentSize) }
}
final class AppDelegate: NSObject, NSApplicationDelegate {
    let model = HostModel()
    func applicationDidFinishLaunching(_ notification: Notification) { model.start() }
    func applicationWillTerminate(_ notification: Notification) { model.stop() }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
}
final class HostModel: ObservableObject {
    @Published var status = "准备启动"
    @Published var secret = ""
    @Published var active = false
    // Avoid SwiftUI.State's SDK macro dependency in Command Line Tools builds.
    @Published var revealSecret = false
    private var host: Host?
    private var pairingKey: Data?
    private var requestedScreenPermission = false
    init() { loadPairingKey() }
    private func loadPairingKey() {
        do {
            let key = try Pairing.load(); pairingKey = key
            secret = key.map { String(format:"%02x", $0) }.joined()
        } catch { status = "无法读取 Keychain 配对密钥：\(error.localizedDescription)" }
    }
    func start(requestPermission: Bool = false) {
        guard host == nil else { return }
        if pairingKey == nil { loadPairingKey() }
        guard let key = pairingKey else { return }
        if !CGPreflightScreenCaptureAccess() {
            if requestPermission && !requestedScreenPermission {
                requestedScreenPermission = true; _ = CGRequestScreenCaptureAccess()
            }
            guard CGPreflightScreenCaptureAccess() else {
                status = "屏幕权限尚未被系统认可；配对密钥仍可查看。授权后退出并重新打开应用。"
                return
            }
        }
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        let trusted = AXIsProcessTrustedWithOptions(options)
        do {
            let h = Host(key: key) { [weak self] text in DispatchQueue.main.async { self?.status = text } }
            try h.start(); host = h; active = true
            status = trusted ? "等待连接 · TCP 47980" : "等待连接（只能查看）· 请授予辅助功能权限"
        } catch { status = "启动失败：\(error)" }
    }
    func stop() { host?.stop(); host = nil; active = false; status = "已停止" }
}
struct HostView: View {
    @ObservedObject var model: HostModel
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Remote Mac LAN").font(.title)
            Text(model.status).textSelection(.enabled)
            Text("在 Windows 填写此 Mac 的局域网 IP 和配对密钥。当前桌面会被共享；停止共享即可收回控制。").frame(width: 450, alignment: .leading)
            Toggle("显示配对密钥（仅交给自己的 Windows）", isOn: $model.revealSecret)
            if model.revealSecret {
                Text(model.secret.isEmpty ? "密钥读取失败，请查看上方错误信息。" : model.secret)
                    .font(.system(.caption, design: .monospaced)).textSelection(.enabled).frame(width: 450)
                Button("复制配对密钥") { NSPasteboard.general.clearContents(); NSPasteboard.general.setString(model.secret, forType: .string) }.disabled(model.secret.isEmpty)
            }
            HStack {
                Button(model.active ? "停止共享" : "开始共享") { if model.active { model.stop() } else { model.start(requestPermission: true) } }
                Button("打开权限设置") { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button("退出") { NSApplication.shared.terminate(nil) }
            }
            Text("仅支持正常登录后的用户会话；不支持 FileVault 预启动解锁。\n端口 47980；不传输音频、不写录像文件。").font(.caption).foregroundStyle(.secondary)
        }.padding(24)
    }
}
