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
    var body: some Scene {
        Settings { EmptyView() }
            .commands {
                CommandGroup(replacing: .appSettings) {
                    Button(UiText.text("设置")) { delegate.openSettings() }.keyboardShortcut(",")
                }
            }
    }
}
final class DesktopNavigation: ObservableObject { @Published var page = "connect" }
final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate {
    let model = HostModel()
    private lazy var controller = ControllerModel(host: model)
    private let navigation = DesktopNavigation()
    private var window: ControllerWindow?
    func openSettings() { navigation.page = "settings"; openWindow() }
    private func openWindow() {
        if let window { window.makeKeyAndOrderFront(nil); return }
        let next = ControllerWindow(contentRect: NSRect(x: 0,y: 0,width: 1000,height: 660),
            styleMask: [.titled,.closable,.miniaturizable,.resizable], backing: .buffered, defer: false)
        next.title = "Local Remote Desktop"; next.isReleasedWhenClosed = false
        next.minSize = NSSize(width: 820,height: 520); next.acceptsMouseMovedEvents = true
        next.contentView = NSHostingView(rootView: DesktopShell(host: model, controller: controller, navigation: navigation,
            fullScreen: { [weak next] in next?.toggleFullScreen(nil) }))
        next.delegate = self; next.center(); window = next; next.makeKeyAndOrderFront(nil)
    }
    func applicationDidFinishLaunching(_ notification: Notification) {
        let current = ProcessInfo.processInfo.processIdentifier
        if let first = NSRunningApplication.runningApplications(withBundleIdentifier: "com.remotemaclan.host")
            .filter({ !$0.isTerminated }).min(by: { $0.processIdentifier < $1.processIdentifier }), first.processIdentifier < current {
            first.activate(options: [.activateIgnoringOtherApps])
            NSApplication.shared.terminate(nil); return
        }
        AppBrand.applyDockIcon(); openWindow(); model.start()
    }
    func windowWillClose(_ notification: Notification) { controller.disconnect() }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool { openWindow(); return false }
    func applicationWillTerminate(_ notification: Notification) { controller.disconnect(); model.stop() }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
}
struct DesktopShell: View {
    @ObservedObject var host: HostModel
    @ObservedObject var controller: ControllerModel
    @ObservedObject var navigation: DesktopNavigation
    var fullScreen: () -> Void
    private let accent = Color(red: 82.0/255,green: 101.0/255,blue: 236.0/255)
    var body: some View {
        Group {
            if controller.busy { ControllerView(model: controller, host: host, fullScreen: fullScreen) }
            else {
                HStack(spacing: 0) {
                    VStack(alignment: .leading, spacing: 8) {
                        Text(host.deviceName).font(.caption).foregroundStyle(.secondary).padding(.bottom, 18)
                        navigationButton("连接", symbol: "rectangle.connected.to.line.below", page: "connect")
                        navigationButton("共享此设备", symbol: "arrow.left.arrow.right", page: "share")
                        navigationButton("设置", symbol: "gearshape", page: "settings")
                        Spacer()
                    }.padding(16).frame(width: 165).background(Color(nsColor: .windowBackgroundColor))
                    Divider()
                    Group {
                        switch navigation.page {
                        case "share": ScrollView { HostView(model: host) }.frame(maxWidth: .infinity,maxHeight: .infinity)
                        case "settings": DesktopSettings(host: host, controller: controller)
                        default: ConnectionPage(model: controller, host: host, showSharing: { navigation.page = "share" })
                        }
                    }.frame(maxWidth: .infinity,maxHeight: .infinity)
                }
            }
        }.tint(accent).frame(minWidth: 820,minHeight: 480)
    }
    private func navigationButton(_ title: String, symbol: String, page: String) -> some View {
        Button { navigation.page = page } label: {
            Label(UiText.text(title), systemImage: symbol).frame(maxWidth: .infinity,alignment: .leading).padding(12)
                .background(navigation.page == page ? accent.opacity(0.12) : Color.clear)
                .cornerRadius(6)
        }.buttonStyle(.plain)
    }
}
struct ConnectionPage: View {
    @ObservedObject var model: ControllerModel
    @ObservedObject var host: HostModel
    var showSharing: () -> Void
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 24) {
                Text(UiText.text("连接")).font(.largeTitle)
                Text(UiText.text("选择你要连接并操作的设备。")).foregroundStyle(.secondary)
                HStack(alignment: .top, spacing: 28) {
                    VStack(alignment: .leading, spacing: 12) {
                        Text(UiText.text("附近设备")).font(.headline)
                        Text(UiText.text("Mac 控制端暂不支持自动发现；请填写目标IP。"))
                            .foregroundStyle(.secondary).fixedSize(horizontal: false,vertical: true)
                    }.frame(maxWidth: .infinity,alignment: .leading)
                    Divider()
                    VStack(alignment: .leading, spacing: 14) {
                        Text(UiText.text("输入目标IP")).font(.title2)
                        Text(UiText.text("目标IP")).font(.caption)
                        TextField("192.168.1.10", text: $model.address).textFieldStyle(.roundedBorder)
                        Text(UiText.text("配对密码")).font(.caption)
                        SecureField(UiText.text("配对密码"), text: $model.password).textFieldStyle(.roundedBorder)
                        Picker(UiText.text("画质"), selection: $model.quality) {
                            Text(UiText.text("流畅")).tag(0); Text(UiText.text("标准")).tag(1); Text(UiText.text("高清")).tag(2)
                        }
                        Button(UiText.text("连接")) { model.connect() }.buttonStyle(.borderedProminent)
                            .disabled(host.active || host.starting)
                        if host.active || host.starting {
                            Text(UiText.text("先停止共享，再连接另一台设备。")).font(.caption).foregroundStyle(.secondary)
                            Button(UiText.text("共享此设备"), action: showSharing)
                        } else {
                            Text(UiText.text("首次连接需要目标设备的配对密码。")).font(.caption).foregroundStyle(.secondary)
                        }
                        Text(UiText.text(model.status)).font(.caption).foregroundStyle(.secondary).textSelection(.enabled)
                    }.frame(width: 280)
                }
                Spacer(minLength: 10)
            }.padding(32).frame(maxWidth: .infinity,alignment: .leading)
        }
    }
}
struct DesktopSettings: View {
    @ObservedObject var host: HostModel
    @ObservedObject var controller: ControllerModel
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 18) {
                Text(UiText.text("设置")).font(.largeTitle)
                Text(UiText.text("剪贴板")).font(.headline)
                Toggle(UiText.text("同步文字"), isOn: $host.allowClipboard)
                Toggle(UiText.text("同步文件"), isOn: $host.allowFiles)
                Text(UiText.text("Mac 控制端的剪贴板和文件同步将在下一阶段补齐。"))
                    .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false,vertical: true)
                Button(UiText.text("打开接收目录")) { host.openReceivedFiles() }
                Divider()
                Text(UiText.text("键盘与连接")).font(.headline)
                Toggle(UiText.text("Mac 快捷键"), isOn: $controller.shortcuts)
                Toggle(UiText.text("自动重连"), isOn: $controller.autoReconnect)
                Divider()
                Picker(UiText.text("语言"), selection: $host.languageCode) {
                    Text("English").tag("en"); Text("简体中文").tag("zh-Hans")
                }.pickerStyle(.segmented)
                Divider()
                Text(UiText.text("权限")).font(.headline)
                Button(UiText.text("屏幕录制权限")) { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                Button(UiText.text("辅助功能权限")) { host.openAccessibilitySettings() }
                Divider()
                Text(UiText.text("诊断")).font(.headline)
                Text("Local Remote Desktop 0.3.0-alpha.2").font(.caption).foregroundStyle(.secondary)
                Button(UiText.text("复制启动诊断")) { host.copyDiagnostics() }
                if !host.inputTestResult.isEmpty { Text(UiText.text(host.inputTestResult)).font(.caption) }
            }.frame(maxWidth: 520,alignment: .leading).padding(32).frame(maxWidth: .infinity,alignment: .leading)
        }
    }
}
struct HostView: View {
    @ObservedObject var model: HostModel
    private let blueberry = Color(red: 82.0 / 255.0, green: 101.0 / 255.0, blue: 236.0 / 255.0)
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
    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            Text(UiText.text("共享此设备")).font(.largeTitle)
            Text(UiText.text("让另一台设备连接并操作你的当前屏幕。")).foregroundStyle(.secondary)
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
            VStack(alignment: .leading, spacing: 12) {
                Text(UiText.text("被另一台设备来连接")).font(.headline)
                HStack {
                    Text(UiText.text("本机名称")).foregroundStyle(.secondary)
                    Text(model.deviceName).textSelection(.enabled)
                    Spacer()
                    Button(UiText.text("修改名称")) { model.openNameEditor() }
                }
            }
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
                Text(UiText.text("在另一台设备上输入这台 Mac 的 IP 和配对密码。"))
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
        }.frame(maxWidth: 600, alignment: .leading).padding(32).frame(maxWidth: .infinity,alignment: .leading).tint(blueberry)
        .sheet(isPresented: $model.editingName, onDismiss: { model.cancelNameEditor() }) {
            DeviceNameEditorView(model: model).tint(blueberry)
        }
        .sheet(isPresented: $model.editingPassword, onDismiss: { model.cancelPasswordEditor() }) {
            PasswordEditorView(model: model).tint(blueberry)
        }
    }
}
struct DeviceNameEditorView: View {
    @ObservedObject var model: HostModel
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text(UiText.text("修改本机名称")).font(.title2)
            Text(UiText.text("显示在其他设备的列表中，默认使用系统本机名称。")).font(.callout).foregroundStyle(.secondary)
            TextField(UiText.text("本机名称"), text: $model.nameDraft).textFieldStyle(.roundedBorder)
            Button(UiText.text("使用系统名称")) { model.restoreSystemName() }
            if !model.nameError.isEmpty { Text(UiText.text(model.nameError)).font(.caption).foregroundStyle(.red) }
            HStack { Spacer(); Button(UiText.text("取消")) { model.cancelNameEditor() }
                Button(UiText.text("保存名称")) { model.saveName() }.buttonStyle(.borderedProminent)
            }
        }.padding(24).frame(width: 400)
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
