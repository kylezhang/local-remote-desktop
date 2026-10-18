import SwiftUI
import AppKit
import ApplicationServices
import Darwin

// SDK 27 exposes @State as a macro whose plugin may be absent in Command Line Tools.
// A type alias selects the existing State property wrapper on macOS 13+ and
// preserves SwiftUI DynamicProperty/Binding behavior without a compiler plugin.
typealias ViewState<Value> = SwiftUI.State<Value>
enum AppBrand {
    static let icon: NSImage? = {
        for ext in ["png", "icns"] {
            if let url = Bundle.main.url(forResource: "LocalRemoteDesktop", withExtension: ext),
               let image = NSImage(contentsOf: url), image.isValid { return image }
        }
        return nil
    }()
    static let connectionStage: NSImage? = {
        guard let url = Bundle.main.url(forResource: "connection-stage", withExtension: "png") else { return nil }
        return NSImage(contentsOf: url)
    }()
    static let deviceDisplay: NSImage? = {
        guard let url = Bundle.main.url(forResource: "device-display", withExtension: "png") else { return nil }
        return NSImage(contentsOf: url)
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
final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate {
    let model = HostModel()
    private lazy var controller = ControllerModel(host: model)
    private let navigation = DesktopNavigation()
    private var window: ControllerWindow?
    func openSettings() { navigation.show("settings", controller: controller); openWindow() }
    private func openWindow() {
        if let window { window.makeKeyAndOrderFront(nil); return }
        let next = ControllerWindow(contentRect: NSRect(x: 0,y: 0,width: 600,height: 658),
            styleMask: [.titled,.closable,.miniaturizable,.resizable], backing: .buffered, defer: false)
        next.title = "Local Remote Desktop"; next.isReleasedWhenClosed = false
        let visible = NSScreen.main?.visibleFrame ?? NSRect(x: 0,y: 0,width: 1440,height: 900)
        let size = NSSize(width: min(600,max(1,visible.width-48)),height: min(680,max(1,visible.height-48)))
        next.minSize = NSSize(width: min(520,size.width),height: min(480,size.height)); next.acceptsMouseMovedEvents = true
        next.contentView = NSHostingView(rootView: DesktopShell(host: model, controller: controller, navigation: navigation,
            fullScreen: { [weak self, weak next] in guard let self, self.controller.busy else { return }; self.controller.release(); next?.toggleFullScreen(nil) }))
        next.setFrame(NSRect(x: visible.midX-size.width/2,y: visible.midY-size.height/2,width: size.width,height: size.height),display: false)
        next.delegate = self; window = next; next.makeKeyAndOrderFront(nil)
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
    func windowWillEnterFullScreen(_ notification: Notification) { controller.release() }
    func windowWillExitFullScreen(_ notification: Notification) { controller.release() }
    func windowDidEnterFullScreen(_ notification: Notification) { restoreRemoteFocus() }
    func windowDidExitFullScreen(_ notification: Notification) { restoreRemoteFocus() }
    private func restoreRemoteFocus() {
        guard controller.screenVisible, controller.connected, controller.inputEnabled, let window, window.isKeyWindow else { return }
        func screen(in view: NSView) -> RemoteScreenView? {
            if let screen = view as? RemoteScreenView { return screen }
            for child in view.subviews { if let found = screen(in: child) { return found } }
            return nil
        }
        if let content = window.contentView, let target = screen(in: content) { window.makeFirstResponder(target) }
    }
    func windowWillClose(_ notification: Notification) { controller.disconnect() }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool { openWindow(); return false }
    func applicationWillTerminate(_ notification: Notification) { controller.disconnect(); model.stop() }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
}
enum EntryBuild {
    static let revision = "screen-desk-2026-10-18-session"
    static var source: String {
        guard let url = Bundle.main.url(forResource: "source-revision", withExtension: "txt"),
            let text = try? String(contentsOf: url, encoding: .utf8) else { return "unknown" }
        return text.trimmingCharacters(in: .whitespacesAndNewlines)
    }
}
// Enumerate local interfaces only; never infer a peer name/system from its IP.
enum LocalInterfaces {
    static var addresses: [String] {
        var list: UnsafeMutablePointer<ifaddrs>?
        guard getifaddrs(&list) == 0 else { return [] }
        defer { freeifaddrs(list) }
        var cursor = list, found: [(String, String)] = []
        while let item = cursor {
            defer { cursor = item.pointee.ifa_next }
            let info = item.pointee
            guard let address = info.ifa_addr, address.pointee.sa_family == UInt8(AF_INET),
                info.ifa_flags & UInt32(IFF_UP) != 0, info.ifa_flags & UInt32(IFF_LOOPBACK) == 0 else { continue }
            var buffer = [CChar](repeating: 0, count: Int(NI_MAXHOST))
            if getnameinfo(address, socklen_t(MemoryLayout<sockaddr_in>.size), &buffer, socklen_t(buffer.count), nil, 0, NI_NUMERICHOST) == 0 {
                found.append((String(cString: info.ifa_name), String(cString: buffer)))
            }
        }
        let sorted = found.sorted { a, b in
            let ap = a.0.hasPrefix("en") ? 0 : 1, bp = b.0.hasPrefix("en") ? 0 : 1
            return ap == bp ? a.0 < b.0 : ap < bp
        }.map { $0.1 }
        var seen = Set<String>()
        return sorted.filter { seen.insert($0).inserted }
    }
}
enum ScreenDeskTheme {
    static let ink = Color(red: 32.0/255,green: 45.0/255,blue: 70.0/255)
    static let accent = Color(red: 82.0/255,green: 101.0/255,blue: 236.0/255)
    static let rule = Color(red: 223.0/255,green: 229.0/255,blue: 239.0/255)
    static let surface = Color(red: 250.0/255,green: 251.0/255,blue: 254.0/255)
}
struct EntryTileStyle: ButtonStyle {
    @ViewState private var hovering = false
    @Environment(\.isEnabled) private var enabled
    @Environment(\.isFocused) private var focused
    @Environment(\.colorSchemeContrast) private var contrast
    func makeBody(configuration: Configuration) -> some View {
        configuration.label.frame(maxWidth: .infinity, minHeight: 76)
            .contentShape(Rectangle())
            .background(configuration.isPressed ? Color(red: 0.914,green: 0.929,blue: 1) :
                hovering ? Color(red: 0.933,green: 0.949,blue: 1) : ScreenDeskTheme.surface)
            .overlay(RoundedRectangle(cornerRadius: 12)
                .stroke(contrast == .increased ? Color.primary : Color.accentColor,lineWidth: focused ? 2 : 0).padding(3))
            .opacity(enabled ? 1 : 0.45)
            .onHover { hovering = $0 }
    }
}
struct EntryHomeView<Actions: View>: View {
    @Environment(\.colorSchemeContrast) private var contrast
    @ViewBuilder var actions: () -> Actions
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text(UiText.text("你想如何使用？")).font(.system(size: 27,weight: .medium))
                .foregroundStyle(contrast == .increased ? Color.primary : ScreenDeskTheme.ink)
                .padding(.bottom,6)
            if let image = AppBrand.connectionStage {
                Image(nsImage: image).resizable().scaledToFit().frame(maxWidth: .infinity).frame(height: 260)
                    .accessibilityHidden(true)
            }
            actions().padding(.top,28)
        }
    }
}
struct DesktopShell: View {
    @ObservedObject var host: HostModel
    @ObservedObject var controller: ControllerModel
    @ObservedObject var navigation: DesktopNavigation
    var fullScreen: () -> Void
    @Environment(\.colorSchemeContrast) private var contrast
    private let accent = ScreenDeskTheme.accent
    var body: some View {
        Group {
            if navigation.page == "session" {
                ControllerView(model: controller, host: host, fullScreen: fullScreen,
                    home: { navigation.show("home", controller: controller) },
                    settings: { navigation.show("settings", controller: controller) })
            }
            else {
                VStack(spacing: 0) {
                    HStack {
                        if navigation.page != "home" {
                            Button { navigation.show("home", controller: controller) } label: { Label(UiText.text("返回"),systemImage: "chevron.left") }
                        }
                        if controller.busy {
                            Button(UiText.text("返回会话")) { navigation.show("session", controller: controller) }
                        }
                        Spacer()
                        Button { navigation.show("settings", controller: controller) } label: { Label(UiText.text("设置"),systemImage: "gearshape") }
                            .font(.system(size: 12,weight: .medium))
                    }.controlSize(.regular).padding(.horizontal,28).padding(.top,16)
                    Group {
                        switch navigation.page {
                        case "home":
                            GeometryReader { viewport in ScrollView {
                                VStack(alignment: .leading,spacing: 0) {
                                    EntryHomeView {
                                        HStack(spacing: 0) {
                                            entry("发起连接",outgoing: true,page: "connect")
                                            Rectangle().fill(contrast == .increased ? Color.primary : ScreenDeskTheme.rule).frame(width: 1)
                                            entry("接受连接",outgoing: false,page: "share")
                                        }.fixedSize(horizontal: false,vertical: true)
                                            .background(ScreenDeskTheme.surface)
                                            .clipShape(RoundedRectangle(cornerRadius: 16))
                                            .overlay(RoundedRectangle(cornerRadius: 16).stroke(contrast == .increased ? Color.primary : ScreenDeskTheme.rule,lineWidth: 1))
                                    }
                                    if host.active || host.starting {
                                        Button(UiText.text("正在接受连接 · 查看或停止")) { navigation.page = "share" }
                                            .padding(.top,22)
                                    }
                                    if controller.busy {
                                        Text(UiText.text("请先断开当前连接，再切换用途。")).font(.caption).foregroundStyle(.secondary).padding(.top,16)
                                    }
                                }.padding(.horizontal,32).padding(.top,18).padding(.bottom,45)
                                    .frame(maxWidth: .infinity,minHeight: viewport.size.height,alignment: .center)
                            }}
                        case "share":
                            VStack(spacing: 0) {
                                ScrollView { HostView(model: host) }.frame(maxWidth: .infinity,maxHeight: .infinity)
                                Divider().padding(.horizontal,28)
                                SharingActionView(model: host).padding(.horizontal,28).padding(.top,16).padding(.bottom,24)
                            }.frame(maxWidth: .infinity,maxHeight: .infinity)
                        case "settings": DesktopSettings(host: host, controller: controller)
                        default: ConnectionPage(model: controller, host: host)
                        }
                    }.frame(maxWidth: .infinity,maxHeight: .infinity)
                }.background(Color.white)
            }
        }.font(.system(size: 14)).foregroundStyle(Color(red: 41.0/255,green: 50.0/255,blue: 71.0/255)).tint(accent)
        .frame(minWidth: min(520,max(1,(NSScreen.main?.visibleFrame.width ?? 1440)-48)),minHeight: min(450,max(1,(NSScreen.main?.visibleFrame.height ?? 900)-80)))
        .sheet(isPresented: $controller.manualOpen) { ManualConnectionView(model: controller, host: host) }
        .onChange(of: host.active) { active in if active && navigation.page == "home" { navigation.page = "share" } }
        .onChange(of: controller.busy) { _ in navigation.sessionChanged(controller) }
        .onChange(of: controller.manualOpen) { open in if !open && controller.busy { navigation.show("session", controller: controller) } }
        .onChange(of: host.allowClipboard) { _ in controller.refreshSessionClipboard() }
        .onChange(of: host.allowFiles) { _ in controller.refreshSessionClipboard() }
    }
    private func entry(_ title: String, outgoing: Bool, page: String) -> some View {
        Button { navigation.show(page, controller: controller) } label: {
            VStack(spacing: 7) {
                Image(systemName: outgoing ? "arrow.up.right" : "arrow.down.left")
                    .font(.system(size: 18,weight: .regular)).foregroundStyle(contrast == .increased ? Color.primary : accent).accessibilityHidden(true)
                Text(UiText.text(title)).font(.system(size: 17,weight: .medium)).multilineTextAlignment(.center)
            }.foregroundStyle(contrast == .increased ? Color.primary : ScreenDeskTheme.ink).padding(.horizontal,10).padding(.vertical,15)
        }.buttonStyle(EntryTileStyle()).accessibilityLabel(UiText.text(title)).disabled(controller.busy)
    }
}
struct DevicePicture: View {
    var width: CGFloat = 110, height: CGFloat = 74
    var body: some View {
        Group {
            if let image = AppBrand.deviceDisplay { Image(nsImage: image).resizable().scaledToFit() }
            else { Image(systemName: "display").resizable().scaledToFit().foregroundStyle(.secondary) }
        }.frame(width: width,height: height).accessibilityHidden(true)
    }
}
struct DeviceTileStyle: ButtonStyle {
    @ViewState private var hovering = false
    @Environment(\.isFocused) private var focused
    func makeBody(configuration: Configuration) -> some View {
        configuration.label.padding(15).frame(maxWidth: .infinity,minHeight: 179,alignment: .topLeading)
            .background(configuration.isPressed ? Color.accentColor.opacity(0.12) : hovering ? Color.accentColor.opacity(0.05) : ScreenDeskTheme.surface)
            .clipShape(RoundedRectangle(cornerRadius: 16))
            .overlay(RoundedRectangle(cornerRadius: 16).stroke(focused ? Color.accentColor : hovering ? Color.accentColor.opacity(0.5) : ScreenDeskTheme.rule,lineWidth: focused ? 2 : 1))
            .contentShape(RoundedRectangle(cornerRadius: 16)).onHover { hovering = $0 }
    }
}
struct ConnectionPage: View {
    @ObservedObject var model: ControllerModel
    @ObservedObject var host: HostModel
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                Text(UiText.text("发起连接")).font(.system(size: 26,weight: .medium))
                Text(UiText.text("选择你要连接并操作的设备。")).font(.system(size: 12)).foregroundStyle(.secondary).padding(.top,8).padding(.bottom,24)
                HStack {
                    Text(UiText.text("附近设备")).font(.system(size: 12)).foregroundStyle(.secondary)
                    Spacer()
                    Button { model.refreshDevices() } label: { Image(systemName: "arrow.clockwise") }
                        .help(UiText.text("刷新设备")).accessibilityLabel(UiText.text("刷新设备")).disabled(model.searching)
                    Button { model.openManual() } label: { Label(UiText.text("添加目标"),systemImage: "plus") }.disabled(model.credentialBusy)
                }.controlSize(.small).padding(.bottom,16)
                if model.devices.isEmpty {
                    VStack(spacing: 12) {
                        DevicePicture()
                        Text(UiText.text(model.searching ? "正在搜索局域网设备…" : "暂无设备记录")).font(.system(size: 14,weight: .medium))
                        Text(UiText.text("让目标设备开始接受连接后刷新，也可以添加目标IP。"))
                            .font(.system(size: 12)).foregroundStyle(.secondary).multilineTextAlignment(.center)
                            .fixedSize(horizontal: false,vertical: true)
                    }.padding(28).frame(maxWidth: .infinity,minHeight: 215)
                        .background(ScreenDeskTheme.surface).cornerRadius(16)
                } else {
                    if model.searching { Text(UiText.text("正在搜索局域网设备…")).font(.caption).foregroundStyle(.secondary).padding(.bottom,12) }
                    LazyVGrid(columns: [GridItem(.flexible(),spacing: 12),GridItem(.flexible())],spacing: 12) {
                        ForEach(model.devices) { device in
                            Button { model.select(device); model.openManual() } label: {
                                VStack(alignment: .leading,spacing: 5) {
                                    HStack { DevicePicture(width: 110,height: 70); Spacer(minLength: 0); Image(systemName: "chevron.right").font(.caption).foregroundStyle(.secondary) }
                                        .padding(.bottom,5)
                                    Text(device.name).font(.system(size: 14,weight: .medium)).lineLimit(2)
                                    Text(device.systemLabel).font(.system(size: 12)).foregroundStyle(.secondary)
                                    Text(UiText.text(device.online ? "本次已发现" : "最近连接 · 本次未发现")).font(.system(size: 11)).foregroundStyle(.secondary)
                                }.frame(maxWidth: .infinity,alignment: .leading).opacity(device.online ? 1 : 0.65)
                            }.buttonStyle(DeviceTileStyle()).disabled(model.credentialBusy)
                                .accessibilityLabel(device.name + ", " + device.systemLabel + ", " + UiText.text(device.online ? "本次已发现" : "最近连接 · 本次未发现"))
                        }
                    }
                }
            }.padding(.horizontal,32).padding(.top,18).padding(.bottom,24).frame(maxWidth: .infinity,alignment: .leading)
        }.onAppear { model.refreshDevices() }.onDisappear { model.pauseDiscovery() }
    }
}
struct ManualConnectionView: View {
    @ObservedObject var model: ControllerModel
    @ObservedObject var host: HostModel
    var body: some View {
        VStack(alignment: .leading,spacing: 0) {
            Text(UiText.text("连接设备")).font(.system(size: 24,weight: .medium)).padding(.horizontal,24).padding(.top,24).padding(.bottom,14)
            ScrollView {
                VStack(alignment: .leading,spacing: 10) {
                    if let device = model.manualDevice {
                        HStack(spacing: 14) {
                            DevicePicture(width: 64,height: 44)
                            VStack(alignment: .leading,spacing: 4) {
                                Text(device.name).fontWeight(.medium).fixedSize(horizontal: false,vertical: true)
                                Text(device.systemLabel).font(.caption).foregroundStyle(.secondary)
                            }
                        }.padding(.bottom,8)
                    } else {
                        Text(UiText.text("连接后显示目标设备名称和系统。")).font(.caption).foregroundStyle(.secondary).padding(.bottom,4)
                    }
                    Text(UiText.text("目标IP")).font(.caption)
                    TextField("192.168.1.10",text: $model.manualAddress).textFieldStyle(.roundedBorder).disabled(model.busy || model.credentialBusy)
                    Text(UiText.text("配对密码")).font(.caption)
                    SecureField(UiText.text(model.manualDevice?.remembered == true ? "已记住密码" : "配对密码"),text: $model.manualPassword)
                        .textFieldStyle(.roundedBorder).disabled(model.busy || model.credentialBusy)
                    Toggle(UiText.text("记住密码"),isOn: $model.manualRemember).disabled(model.busy || model.credentialBusy)
                    Picker(UiText.text("画质"),selection: $model.nextQuality) {
                        Text(UiText.text("流畅")).tag(0); Text(UiText.text("标准")).tag(1); Text(UiText.text("高清")).tag(2)
                    }.disabled(model.busy || model.credentialBusy)
                    if model.manualDevice?.remembered == true {
                        Button(UiText.text("忘记密码")) { model.forgetManualPassword() }.disabled(model.busy || model.credentialBusy)
                    }
                    if host.active || host.starting {
                        Toggle(UiText.text("停止接受连接并连接"),isOn: $model.stopSharingToConnect).disabled(model.busy)
                        Text(UiText.text("这将停止本机接受连接并断开当前控制者。")).font(.caption).foregroundStyle(.secondary)
                    }
                    if model.busy || !model.pairingNotice.isEmpty {
                        Text(UiText.text(model.pairingNotice.isEmpty ? model.status : model.pairingNotice))
                            .font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false,vertical: true)
                    }
                }.padding(.horizontal,24).padding(.bottom,12).frame(maxWidth: .infinity,alignment: .leading)
            }
            Divider()
            HStack {
                Spacer()
                Button(UiText.text("取消")) { model.cancelManual() }.keyboardShortcut(.cancelAction)
                Button(UiText.text(model.busy ? "正在连接…" : "连接")) { model.connectManual() }
                    .buttonStyle(.borderedProminent).keyboardShortcut(.defaultAction)
                    .disabled(model.busy || model.credentialBusy || ((host.active || host.starting) && !model.stopSharingToConnect))
            }.padding(.horizontal,24).padding(.vertical,18)
        }.frame(width: 476).frame(maxHeight: 600).interactiveDismissDisabled(model.busy || model.credentialBusy)
    }
}
struct PreferenceSection<Content: View>: View {
    let title: String
    var last = false
    @ViewBuilder var content: () -> Content
    var body: some View {
        VStack(alignment: .leading,spacing: 8) {
            Text(UiText.text(title)).font(.system(size: 12,weight: .medium)).foregroundStyle(.secondary)
            content().font(.system(size: 13)).controlSize(.small)
            if !last { Divider().padding(.top,2) }
        }.padding(.bottom,last ? 0 : 10)
    }
}
struct DesktopSettings: View {
    @ObservedObject var host: HostModel
    @ObservedObject var controller: ControllerModel
    var body: some View {
        VStack(alignment: .leading,spacing: 20) {
            Text(UiText.text("设置")).font(.system(size: 26,weight: .medium))
            ScrollView {
                VStack(alignment: .leading,spacing: 0) {
                    PreferenceSection(title: "通用") {
                        HStack {
                            Text(UiText.text("语言")); Spacer()
                            Picker(UiText.text("语言"),selection: $host.languageCode) {
                                Text("English").tag("en"); Text("简体中文").tag("zh-Hans")
                            }.labelsHidden().frame(width: 170)
                        }
                    }
                    PreferenceSection(title: "连接设置") {
                        Picker(UiText.text("画质"),selection: $controller.defaultQuality) {
                            Text(UiText.text("流畅")).tag(0); Text(UiText.text("标准")).tag(1); Text(UiText.text("高清")).tag(2)
                        }
                        Toggle(UiText.text("自动重连"),isOn: $controller.defaultAutoReconnect).toggleStyle(.switch)
                        Toggle(UiText.text("Mac 快捷键"),isOn: $controller.defaultShortcuts).toggleStyle(.switch)
                            .help(UiText.text("Mac 本机快捷键映射到目标 Windows 对应按键。"))
                        Text(UiText.text("用于下一次连接；当前会话在画面工具栏中调整。")).font(.system(size: 11)).foregroundStyle(.secondary)
                            .fixedSize(horizontal: false,vertical: true)
                    }
                    PreferenceSection(title: "剪贴板") {
                        Toggle(UiText.text("同步文字"),isOn: $host.allowClipboard).toggleStyle(.switch)
                        Toggle(UiText.text("同步文件"),isOn: $host.allowFiles).toggleStyle(.switch)
                        Button(UiText.text("打开接收目录")) { host.openReceivedFiles() }
                        Text(UiText.text("关闭后立即停止对应同步；会话选项只能进一步限制共享。")).font(.system(size: 11)).foregroundStyle(.secondary)
                            .fixedSize(horizontal: false,vertical: true)
                    }
                    PreferenceSection(title: "关于",last: true) {
                        Text("Local Remote Desktop 0.3.0-alpha.5")
                        Text("GPL-3.0-or-later").font(.system(size: 11)).foregroundStyle(.secondary)
                        DisclosureGroup(UiText.text("故障排查")) {
                            VStack(alignment: .leading,spacing: 10) {
                                Text("\(UiText.text("构建")) \(Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? "unknown") · \(EntryBuild.revision)").font(.caption).foregroundStyle(.secondary)
                                Text("\(UiText.text("源码提交")): \(EntryBuild.source)").font(.caption).foregroundStyle(.secondary).textSelection(.enabled)
                                Button(UiText.text("复制启动诊断")) { host.copyDiagnostics() }
                                if !host.inputTestResult.isEmpty { Text(UiText.text(host.inputTestResult)).font(.caption) }
                            }.padding(.top,10)
                        }
                    }
                }.frame(maxWidth: .infinity,alignment: .leading)
            }
        }.padding(.horizontal,32).padding(.top,18).padding(.bottom,24).frame(maxWidth: .infinity,maxHeight: .infinity,alignment: .topLeading)
    }
}
struct SharingActionView: View {
    @ObservedObject var model: HostModel
    var body: some View {
        Button {
            if model.active { model.stop() } else { model.start(requestPermission: true) }
        } label: {
            Text(UiText.text(model.starting ? "正在启动…" : model.active ? "停止接受连接" : model.inputAccess.canControl ? "开始接受连接" : "开始接受连接（仅查看）"))
                .frame(maxWidth: .infinity,minHeight: 24)
        }.buttonStyle(.borderedProminent).controlSize(.large)
            .disabled(!model.sharingActionAvailable)
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
        model.active ? "正在接受连接" : model.starting ? "正在启动…" : "尚未接受连接"
    }
    private var passwordText: String {
        if model.secret.isEmpty { return "尚未读取" }
        return model.revealSecret ? PairingEditor.displayed(model.secret) : "••••••••"
    }
    var body: some View {
        VStack(alignment: .leading,spacing: 12) {
            Text(UiText.text("接受连接")).font(.system(size: 26,weight: .medium))
            Text(UiText.text("让另一台设备连接并操作你的当前屏幕。")).font(.system(size: 12)).foregroundStyle(.secondary)
            Text(UiText.text(sharingTitle)).font(.system(size: 11)).padding(.horizontal,12).padding(.vertical,5)
                .background(model.active ? Color.green.opacity(0.06) : Color.clear)
                .clipShape(Capsule()).overlay(Capsule().stroke(ScreenDeskTheme.rule,lineWidth: 1))
                .frame(maxWidth: .infinity)
            if model.status != "点击开始接受连接。会先检查屏幕权限，再读取配对密码。" && model.status != "准备启动" {
                Text(UiText.text(model.status)).font(.caption).foregroundStyle(.secondary)
                    .fixedSize(horizontal: false,vertical: true).textSelection(.enabled)
            }
            VStack(spacing: 3) {
                DevicePicture(width: 112,height: 64)
                Text(UiText.text("本机名称")).font(.system(size: 11)).foregroundStyle(.secondary)
                HStack(spacing: 6) {
                    Text(model.deviceName).font(.system(size: 19,weight: .medium)).textSelection(.enabled).fixedSize(horizontal: false,vertical: true)
                    Button(UiText.text("修改名称")) { model.openNameEditor() }
                        .font(.system(size: 11)).controlSize(.small)
                        .disabled(model.credentialBusy || model.editingPassword || model.controlling)
                }.frame(maxWidth: .infinity)
            }.frame(maxWidth: .infinity)
            VStack(alignment: .leading,spacing: 10) {
                Text(UiText.text("本机 IP")).font(.system(size: 11)).foregroundStyle(.secondary)
                ForEach(LocalInterfaces.addresses,id: \.self) { address in
                    HStack {
                        Text(address).font(.system(size: 15,design: .monospaced)).textSelection(.enabled)
                        Spacer()
                        Button(UiText.text("复制")) { NSPasteboard.general.clearContents(); NSPasteboard.general.setString(address,forType: .string) }
                            .accessibilityLabel(UiText.text("复制") + " " + address)
                    }.controlSize(.small)
                }
                Divider()
                Text(UiText.text("连接本设备时，使用下方密码。")).font(.system(size: 11)).foregroundStyle(.secondary)
                HStack {
                    Text(UiText.text(model.legacyCredential ? "当前密码（旧版密钥）" : "当前密码")).font(.system(size: 12,weight: .medium))
                    Spacer()
                    Button(UiText.text("修改密码…")) { model.openPasswordEditor() }.disabled(model.credentialBusy)
                }.controlSize(.small)
                HStack(alignment: .center,spacing: 10) {
                    Text(model.secret.isEmpty ? UiText.text(passwordText) : passwordText).font(.system(size: model.legacyCredential ? 13 : 20,design: .monospaced))
                        .fixedSize(horizontal: false,vertical: true).textSelection(.enabled)
                        .frame(maxWidth: .infinity,alignment: .leading)
                        .accessibilityLabel(UiText.text(model.revealSecret ? "当前配对密码" : "密码已隐藏"))
                    Button(UiText.text(model.readingPassword ? "读取中…" : model.revealSecret ? "隐藏" : "查看")) { model.togglePasswordVisibility() }
                        .disabled(model.credentialBusy)
                    Button(UiText.text("复制")) { model.copyPassword() }.disabled(model.secret.isEmpty || model.credentialBusy)
                }.controlSize(.small)
                if model.legacyCredential {
                    Text(UiText.text("旧版密钥仍然有效；点击“修改密码”可设置短密码。")).font(.caption).foregroundStyle(.secondary).fixedSize(horizontal: false,vertical: true)
                }
                if !model.pairingMessage.isEmpty { Text(UiText.text(model.pairingMessage)).font(.caption).fixedSize(horizontal: false,vertical: true) }
            }.padding(.horizontal,16).padding(.vertical,12)
                .overlay(RoundedRectangle(cornerRadius: 15).stroke(ScreenDeskTheme.rule,lineWidth: 1))
            if !model.inputAccess.canControl {
                HStack {
                    Text(UiText.text("辅助功能权限")).font(.system(size: 12))
                    Spacer()
                    Button(UiText.text("允许键盘和鼠标控制")) { model.openAccessibilitySettings() }
                }.controlSize(.small)
            }
            if hasFileNotice {
                HStack {
                    Text(UiText.text(model.fileStatus)).font(.caption).frame(maxWidth: .infinity,alignment: .leading)
                    if fileTransferring { Button(UiText.text("取消传输")) { model.cancelFiles() } }
                }
            }
            if !model.inputTestResult.isEmpty { Text(UiText.text(model.inputTestResult)).font(.caption).fixedSize(horizontal: false,vertical: true).textSelection(.enabled) }
            DisclosureGroup(UiText.text("权限")) {
                if !model.inputAccess.canControl {
                    Text(UiText.text("未允许，连接后仅可查看屏幕。")).font(.system(size: 11)).foregroundStyle(.secondary)
                    Button(UiText.text("定位当前应用")) { model.revealInstalledApplication() }.controlSize(.small)
                }
                HStack {
                    Button(UiText.text("屏幕录制权限")) { NSWorkspace.shared.open(URL(string:"x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture")!) }
                    Button(UiText.text("辅助功能权限")) { model.openAccessibilitySettings() }
                }.controlSize(.small).padding(.top,10)
            }.font(.system(size: 12))
        }.padding(.horizontal,32).padding(.top,18).padding(.bottom,16).frame(maxWidth: .infinity,alignment: .leading).tint(blueberry)
        .onAppear { model.refreshInputAccess() }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didBecomeActiveNotification)) { _ in model.refreshInputAccess() }
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
                    Button { model.generatePassword() } label: {
                        Label(UiText.text("自动生成"), systemImage: "arrow.clockwise")
                    }.buttonStyle(.bordered).disabled(model.passwordEditor.saving)
                }
                Text(UiText.text("8 位英文字母、数字或符号，不含空格。"))
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
