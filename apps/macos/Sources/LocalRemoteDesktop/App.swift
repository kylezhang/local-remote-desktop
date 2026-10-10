import SwiftUI
import AppKit
import ApplicationServices
enum AppBrand {
    static let icon: NSImage? = {
        for ext in ["png", "icns"] {
            if let url = Bundle.main.url(forResource: "LocalRemoteDesktop", withExtension: ext),
               let image = NSImage(contentsOf: url), image.isValid { return image }
        }
        return nil
    }()
    static func applyDockIcon() {
        if let icon { NSApplication.shared.applicationIconImage = icon }
    }
}
@main
struct LocalRemoteDesktopApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var delegate
    var body: some Scene { WindowGroup("Local Remote Desktop") { HostView(model: delegate.model, connect: { delegate.openController() }) }.windowResizability(.contentSize) }
}
final class AppDelegate: NSObject, NSApplicationDelegate {
    let model = HostModel()
    private lazy var controller = ControllerCoordinator(host: model)
    func openController() { controller.open() }
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
    func applicationWillTerminate(_ notification: Notification) { controller.stop(); model.stop() }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
}
struct HostView: View {
    @ObservedObject var model: HostModel
    var connect: () -> Void = {}
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
        Menu(UiText.text("设置")) {
            Button(UiText.text("连接设备（预览）…"), action: connect)
            Divider()
            Section(UiText.text("剪贴板")) {
                Toggle(UiText.text("同步文字"), isOn: $model.allowClipboard)
                Toggle(UiText.text("同步文件"), isOn: $model.allowFiles)
                Button(UiText.text("打开接收目录")) { model.openReceivedFiles() }
            }
            Menu(UiText.text("权限")) {
                Button(UiText.text("屏幕录制权限")) { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button(UiText.text("辅助功能权限")) { model.openAccessibilitySettings() }
            }
            Picker(UiText.text("语言"), selection: $model.languageCode) {
                Text("English").tag("en")
                Text("简体中文").tag("zh-Hans")
            }
            Menu(UiText.text("诊断")) {
                Button(UiText.text("本机键盘自检")) { model.testInput() }
                Button(UiText.text("复制启动诊断")) { model.copyDiagnostics() }
            }
            Divider()
            Button(UiText.text("退出 Local Remote Desktop")) { NSApplication.shared.terminate(nil) }
        }.fixedSize()
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            HStack(spacing: 10) {
                if let icon = brandIcon {
                    Image(nsImage: icon).resizable().interpolation(.high).scaledToFit()
                        .frame(width: 36, height: 36).accessibilityHidden(true)
                }
                Text(UiText.text("Local Remote Desktop 0.3.0-alpha.2")).font(.title2).foregroundStyle(brandText)
                Spacer()
                settingsMenu
            }
            VStack(alignment: .leading, spacing: 6) {
                Text(UiText.text(sharingTitle)).font(.headline)
                Text(UiText.text(model.status)).font(.callout).foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true).textSelection(.enabled)
                if model.active && !AXIsProcessTrusted() {
                    Button(UiText.text("允许键盘和鼠标控制")) { model.openAccessibilitySettings() }
                }
            }.frame(maxWidth: .infinity, alignment: .leading)
            Button(UiText.text(model.starting ? "正在启动…" : model.active ? "停止共享" : "开始共享")) {
                if model.active { model.stop() } else { model.start(requestPermission: true) }
            }.buttonStyle(.borderedProminent).disabled(model.credentialBusy || model.controlling)
            Divider()
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    Text(UiText.text(model.legacyCredential ? "当前密码（旧版密钥）" : "当前密码")).font(.headline)
                    Spacer()
                    Button(UiText.text("修改密码…")) { model.openPasswordEditor() }.disabled(model.credentialBusy)
                }
                HStack(alignment: .top, spacing: 12) {
                    Text(model.secret.isEmpty ? UiText.text(passwordText) : passwordText).font(.system(.body, design: .monospaced))
                        .fixedSize(horizontal: false, vertical: true).textSelection(.enabled)
                        .frame(maxWidth: .infinity, alignment: .leading)
                        .accessibilityLabel(UiText.text(model.revealSecret ? "当前配对密码" : "密码已隐藏"))
                    Button(UiText.text(model.readingPassword ? "读取中…" : model.revealSecret ? "隐藏" : "查看")) {
                        model.togglePasswordVisibility()
                    }.disabled(model.credentialBusy)
                    Button(UiText.text("复制")) { model.copyPassword() }.disabled(model.secret.isEmpty || model.credentialBusy)
                }
                if model.legacyCredential {
                    Text(UiText.text("旧版密钥仍然有效；点击“修改密码”可设置短密码。"))
                        .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
                }
                if !model.pairingMessage.isEmpty {
                    Text(UiText.text(model.pairingMessage)).font(.caption).fixedSize(horizontal: false, vertical: true)
                }
                Text(UiText.text("在 Windows 填写这台 Mac 的局域网 IP 和当前密码。"))
                    .font(.callout).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
            }
            if hasFileNotice {
                HStack {
                    Text(UiText.text(model.fileStatus)).font(.caption).frame(maxWidth: .infinity, alignment: .leading)
                    if fileTransferring { Button(UiText.text("取消传输")) { model.cancelFiles() } }
                }
            }
            if !model.inputTestResult.isEmpty {
                Text(UiText.text(model.inputTestResult)).font(.caption).fixedSize(horizontal: false, vertical: true).textSelection(.enabled)
            }
            Text(UiText.text("Mac 需保持唤醒并登录。剪贴板共享可在设置中调整。"))
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
            Text(UiText.text("修改配对密码")).font(.title2)
            Text(UiText.text("设置后，在 Windows 填写同一个密码。"))
                .font(.callout).foregroundStyle(.secondary)
            VStack(alignment: .leading, spacing: 8) {
                Text(UiText.text("新密码")).font(.headline)
                HStack {
                    TextField(UiText.text("输入新密码"), text: $model.passwordEditor.draft)
                        .textFieldStyle(.roundedBorder).disabled(model.passwordEditor.saving)
                    Button(UiText.text("自动生成")) { model.generatePassword() }.disabled(model.passwordEditor.saving)
                }
                Text(UiText.text("8–64 位字母、数字或符号，不含空格；自动生成 12 位。"))
                    .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
                if let message = model.passwordEditor.validation(current: model.secret) {
                    Text(UiText.text(message)).font(.caption).foregroundStyle(.red).fixedSize(horizontal: false, vertical: true)
                }
                if !model.passwordEditor.error.isEmpty {
                    Text(UiText.text(model.passwordEditor.error)).font(.caption).foregroundStyle(.red)
                        .fixedSize(horizontal: false, vertical: true)
                }
            }
            Text(UiText.text("保存后原密码失效，当前连接会断开。取消则不作修改。"))
                .font(.callout).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
            HStack {
                if model.passwordEditor.saving { Text(UiText.text("正在安全保存…")).font(.caption).foregroundStyle(.secondary) }
                Spacer()
                Button(UiText.text("取消")) { model.cancelPasswordEditor() }
                    .keyboardShortcut(.cancelAction).disabled(model.passwordEditor.saving)
                Button(UiText.text("保存密码")) { model.savePassword() }
                    .keyboardShortcut(.defaultAction).buttonStyle(.borderedProminent)
                    .disabled(!model.passwordEditor.canSave(current: model.secret))
            }
        }.frame(width: 420, alignment: .leading).padding(24)
        .interactiveDismissDisabled(model.passwordEditor.saving)
    }
}
