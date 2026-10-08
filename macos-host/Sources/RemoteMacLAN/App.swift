import SwiftUI
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
    @Published var status = "准备启动", secret = "", active = false
    private var host: Host?
    func start() {
        guard host == nil else { return }
        guard CGPreflightScreenCaptureAccess() else { _ = CGRequestScreenCaptureAccess(); status = "请在系统设置授权屏幕录制，然后退出并重新打开 Host。"; return }
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        let trusted = AXIsProcessTrustedWithOptions(options)
        do {
            let key = try Pairing.load(); secret = key.map { String(format:"%02x", $0) }.joined()
            let h = Host(key: key) { [weak self] text in DispatchQueue.main.async { self?.status = text } }
            try h.start(); host = h; active = true
            status = trusted ? "等待连接 · TCP 47980" : "等待连接（只能查看）· 请授予辅助功能权限"
        } catch { status = "启动失败：\(error)" }
    }
    func stop() { host?.stop(); host = nil; active = false; status = "已停止" }
}
struct HostView: View {
    @ObservedObject var model: HostModel
    @State private var reveal = false
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text("Remote Mac LAN").font(.title)
            Text(model.status).textSelection(.enabled)
            Text("在 Windows 填写此 Mac 的局域网 IP 和配对密钥。当前桌面会被共享；停止共享即可收回控制。").frame(width: 450, alignment: .leading)
            Toggle("显示配对密钥（仅交给自己的 Windows）", isOn: $reveal)
            if reveal { Text(model.secret).font(.system(.caption, design: .monospaced)).textSelection(.enabled).frame(width: 450) }
            HStack {
                Button(model.active ? "停止共享" : "开始共享") { if model.active { model.stop() } else { model.start() } }
                Button("打开权限设置") { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button("退出") { NSApplication.shared.terminate(nil) }
            }
            Text("仅支持正常登录后的用户会话；不支持 FileVault 预启动解锁。\n端口 47980；不传输音频、不写录像文件。").font(.caption).foregroundStyle(.secondary)
        }.padding(24)
    }
}
