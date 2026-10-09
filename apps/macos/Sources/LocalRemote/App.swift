import SwiftUI
import AppKit
import ApplicationServices
enum AppBrand {
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
    private var hasFileNotice: Bool {
        !model.fileStatus.hasPrefix("文件同步：") || model.fileStatus.contains("不可用")
    }
    private var fileTransferring: Bool {
        model.fileStatus.hasPrefix("文件传输：") || model.fileStatus.hasPrefix("发送文件：") || model.fileStatus.hasPrefix("接收文件：") || model.fileStatus.hasPrefix("文件已复制")
    }
    private var sharingTitle: String {
        model.active ? "正在共享" : model.starting ? "正在启动共享…" : "尚未共享"
    }
    private var passwordText: String {
        if model.secret.isEmpty { return "尚未读取" }
        return model.revealSecret ? PairingEditor.displayed(model.secret) : "••••••••••••"
    }
    private var settingsMenu: some View {
        Menu("设置") {
            Section("剪贴板") {
                Toggle("同步文字", isOn: $model.allowClipboard)
                Toggle("同步文件", isOn: $model.allowFiles)
                Button("打开接收目录") { model.openReceivedFiles() }
            }
            Menu("权限") {
                Button("屏幕录制权限") { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button("辅助功能权限") { model.openAccessibilitySettings() }
            }
            Menu("诊断") {
                Button("本机键盘自检") { model.testInput() }
                Button("复制启动诊断") { model.copyDiagnostics() }
            }
            Divider()
            Button("退出 LocalRemote") { NSApplication.shared.terminate(nil) }
        }.fixedSize()
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            HStack(spacing: 10) {
                if let icon = brandIcon {
                    Image(nsImage: icon).resizable().interpolation(.high).scaledToFit()
                        .frame(width: 36, height: 36).accessibilityHidden(true)
                }
                Text("LocalRemote 0.1.0").font(.title2).foregroundStyle(brandText)
                Spacer()
                settingsMenu
            }
            VStack(alignment: .leading, spacing: 6) {
                Text(sharingTitle).font(.headline)
                Text(model.status).font(.callout).foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true).textSelection(.enabled)
                if model.active && !AXIsProcessTrusted() {
                    Button("允许键盘和鼠标控制") { model.openAccessibilitySettings() }
                }
            }.frame(maxWidth: .infinity, alignment: .leading)
            Button(model.starting ? "正在启动…" : model.active ? "停止共享" : "开始共享") {
                if model.active { model.stop() } else { model.start(requestPermission: true) }
            }.buttonStyle(.borderedProminent).disabled(model.credentialBusy)
            Divider()
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    Text(model.legacyCredential ? "当前密码（旧版密钥）" : "当前密码").font(.headline)
                    Spacer()
                    Button("修改密码…") { model.openPasswordEditor() }.disabled(model.credentialBusy)
                }
                HStack(alignment: .top, spacing: 12) {
                    Text(passwordText).font(.system(.body, design: .monospaced))
                        .fixedSize(horizontal: false, vertical: true).textSelection(.enabled)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .accessibilityLabel(model.revealSecret ? "当前配对密码" : "密码已隐藏")
                    Button(model.readingPassword ? "读取中…" : model.revealSecret ? "隐藏" : "查看") {
                        model.togglePasswordVisibility()
                    }.disabled(model.credentialBusy)
                    Button("复制") { model.copyPassword() }.disabled(model.secret.isEmpty || model.credentialBusy)
                }
                if model.legacyCredential {
                    Text("旧版密钥仍然有效；点击“修改密码”可设置短密码。")
                        .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
                }
                if !model.pairingMessage.isEmpty {
                    Text(model.pairingMessage).font(.caption).fixedSize(horizontal: false, vertical: true)
                }
                Text("在 Windows 填写这台 Mac 的局域网 IP 和当前密码。")
                    .font(.callout).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
            }
            if hasFileNotice {
                HStack {
                    Text(model.fileStatus).font(.caption).frame(maxWidth: .infinity, alignment: .leading)
                    if fileTransferring { Button("取消传输") { model.cancelFiles() } }
                }
            }
            if !model.inputTestResult.isEmpty {
                Text(model.inputTestResult).font(.caption).fixedSize(horizontal: false, vertical: true).textSelection(.enabled)
            }
            Text("Mac 需保持唤醒并登录。剪贴板共享可在设置中调整。")
                .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
        }.frame(width: 450, alignment: .leading).padding(24).tint(blueberry)
        .sheet(isPresented: $model.editingPassword, onDismiss: { model.cancelPasswordEditor() }) {
            PasswordEditorView(model: model).tint(blueberry)
        }
    }
}
struct PasswordEditorView: View {
    @ObservedObject var model: HostModel
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text("修改配对密码").font(.title2)
            Text("设置后，在 Windows 填写同一个密码。")
                .font(.callout).foregroundStyle(.secondary)
            VStack(alignment: .leading, spacing: 8) {
                Text("新密码").font(.headline)
                HStack {
                    TextField("输入新密码", text: $model.passwordEditor.draft)
                        .textFieldStyle(.roundedBorder).disabled(model.passwordEditor.saving)
                    Button("自动生成") { model.generatePassword() }.disabled(model.passwordEditor.saving)
                }
                Text("8–64 位字母、数字或符号，不含空格；自动生成 12 位。")
                    .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
                if let message = model.passwordEditor.validation(current: model.secret) {
                    Text(message).font(.caption).foregroundStyle(.red).fixedSize(horizontal: false, vertical: true)
                }
                if !model.passwordEditor.error.isEmpty {
                    Text(model.passwordEditor.error).font(.caption).foregroundStyle(.red)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
            Text("保存后原密码失效，当前连接会断开。取消则不作修改。")
                .font(.callout).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
            HStack {
                if model.passwordEditor.saving { Text("正在安全保存…").font(.caption).foregroundStyle(.secondary) }
                Spacer()
                Button("取消") { model.cancelPasswordEditor() }
                    .keyboardShortcut(.cancelAction).disabled(model.passwordEditor.saving)
                Button("保存密码") { model.savePassword() }
                    .keyboardShortcut(.defaultAction).buttonStyle(.borderedProminent)
                    .disabled(!model.passwordEditor.canSave(current: model.secret))
            }
        }.frame(width: 420, alignment: .leading).padding(24)
        .interactiveDismissDisabled(model.passwordEditor.saving)
    }
}
