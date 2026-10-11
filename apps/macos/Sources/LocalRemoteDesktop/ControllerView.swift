import SwiftUI
import AppKit
import Foundation

final class ControllerModel: ObservableObject {
    @Published var address = "" { didSet {
        if address != oldValue && !busy { password = cachedPasswords[address] ?? "" }
    } }
    @Published private(set) var devices = [NearbyDevice]()
    @Published private(set) var searching = false
    @Published var rememberPassword = true
    @Published var manualOpen = false
    @Published var manualAddress = "" { didSet { if manualAddress != oldValue { manualPassword = cachedPasswords[manualAddress] ?? "" } } }
    @Published var manualPassword = ""
    @Published var manualRemember = true
    @Published var stopSharingToConnect = false
    @Published private(set) var credentialBusy = false
    @Published private(set) var pairingNotice = ""
    @Published var password = ""
    @Published private(set) var targetName = ""
    @Published private(set) var targetSystem = ""
    @Published var quality = 1
    @Published var nextQuality = 0
    @Published var defaultQuality = 0 { didSet {
        preferences.set(defaultQuality, forKey: "controllerDefaultQuality")
        if !busy { nextQuality = defaultQuality }
    } }
    @Published var defaultShortcuts = true { didSet { preferences.set(defaultShortcuts, forKey: "controllerDefaultShortcuts") } }
    @Published var defaultAutoReconnect = true { didSet { preferences.set(defaultAutoReconnect, forKey: "controllerDefaultReconnect") } }
    @Published var shortcuts = true
    @Published var autoReconnect = true {
        didSet { if !autoReconnect && retry != nil && !qualityRestart { disconnect() } }
    }
    @Published private(set) var busy = false
    @Published private(set) var connected = false
    @Published private(set) var inputEnabled = false
    @Published private(set) var status = "输入 Windows 的目标IP和共享密码。"
    @Published private(set) var image: CGImage? = nil
    @Published private(set) var width = 0
    @Published private(set) var height = 0
    @Published private(set) var inputGeneration = 0
    private let host: HostModel
    private let preferences: UserDefaults
    private let credentials: ControllerCredentials
    private let browser: ControllerBrowsing
    private var known = [String: NearbyDevice](), found = [NearbyDevice]()
    private var cachedPasswords = [String: String](), savedPasswords = [String: String]()
    private var credentialTask: Task<Void, Never>?, credentialGeneration: UUID?
    private var sessionAddress = "", sessionPassword = "", sessionRemember = false
    private var session: ControllerSession?, token: UUID?, retry: DispatchWorkItem?, wantsConnection = false
    private var hasConnected = false
    private var qualityRestart = false
    private var presentation: DispatchSourceTimer?
    private let frameLock = NSLock()
    private var latest: CGImage?, frameToken: UUID?
    init(host: HostModel, preferences: UserDefaults = .standard, credentials: ControllerCredentials = ControllerKeychain(), browser: ControllerBrowsing = ControllerDiscovery()) {
        self.host = host; self.preferences = preferences; self.credentials = credentials; self.browser = browser
        for record in NearbyDevice.load(preferences) { known[record.address] = record }
        devices = Array(known.values).sorted { $0.name < $1.name }
        browser.changed = { [weak self] devices, searching in
            guard let self, !self.busy else { return }
            self.found = devices; self.searching = searching; self.updateDevices()
        }
        address = preferences.string(forKey: "controllerAddress") ?? ""
        let storedQuality = preferences.object(forKey: "controllerDefaultQuality") as? Int ?? 0
        defaultQuality = (0...2).contains(storedQuality) ? storedQuality : 0; nextQuality = defaultQuality
        defaultShortcuts = preferences.object(forKey: "controllerDefaultShortcuts") as? Bool ?? true
        defaultAutoReconnect = preferences.object(forKey: "controllerDefaultReconnect") as? Bool ?? true
    }
    var selectedDevice: NearbyDevice? { devices.first { $0.address == address } }
    var manualDevice: NearbyDevice? { devices.first { $0.address == manualAddress } }
    var hasRememberedPassword: Bool { known[address]?.remembered == true }
    func refreshDevices() { guard !busy else { return }; browser.start() }
    func pauseDiscovery() { browser.stop(); searching = false }
    private func updateDevices() {
        var merged = known.mapValues { value in var item = value; item.online = false; return item }
        for device in found.prefix(32) {
            var item = device; item.remembered = known[item.address]?.remembered == true
            merged[item.address] = item
        }
        devices = Array(merged.values.sorted { $0.name < $1.name }.prefix(32))
    }
    private func persistPeers() {
        if let data = try? JSONEncoder().encode(Array(known.values.prefix(32))) { preferences.set(data, forKey: "controllerPeers") }
        updateDevices()
    }
    func select(_ device: NearbyDevice) { guard !busy, !credentialBusy else { return }; address = device.address; password = cachedPasswords[address] ?? "" }
    func openManual() {
        guard !busy, !credentialBusy else { return }
        manualAddress = address; manualPassword = cachedPasswords[address] ?? password
        manualRemember = rememberPassword; stopSharingToConnect = false; pairingNotice = ""; manualOpen = true
    }
    func cancelManual() {
        if busy { disconnect() }; manualOpen = false; manualPassword = ""
    }
    func connectManual() {
        guard !busy, !credentialBusy else { return }
        do {
            _ = try ControllerRules.address(manualAddress); try PairingCredential.validate(manualPassword)
        } catch {
            if manualPassword.isEmpty && known[manualAddress]?.remembered == true && (try? ControllerRules.address(manualAddress)) != nil { }
            else { pairingNotice = "请输入局域网 IPv4 和 8–64 位配对密码。"; return }
        }
        if host.active || host.starting {
            guard stopSharingToConnect else { pairingNotice = "这将停止本机共享并断开当前控制者。"; return }
            host.stop()
        }
        address = manualAddress; password = manualPassword; rememberPassword = manualRemember; connect()
    }
    func connect() {
        guard !host.active, !host.starting else { status = "请先停止这台 Mac 的共享，再连接其他设备。"; return }
        guard !busy, !credentialBusy else { return }
        do { address = try ControllerRules.address(address) }
        catch { status = "请填写局域网 IPv4 地址。"; pairingNotice = status; return }
        pairingNotice = ""
        if password.isEmpty && hasRememberedPassword {
            let generation = UUID(), target = address
            credentialGeneration = generation; credentialBusy = true; busy = true; host.controlling = true
            status = "正在读取已记住的密码…"
            credentialTask = Task { @MainActor [weak self] in
                guard let self else { return }
                do {
                    let loaded = try await self.credentials.load(address: target)
                    guard self.credentialGeneration == generation, !Task.isCancelled else { return }
                    self.credentialGeneration = nil
                    self.credentialBusy = false; self.busy = false; self.host.controlling = false
                    guard let loaded else { self.pairingNotice = "已记住的密码不可用，请重新填写。"; return }
                    self.password = loaded; self.cachedPasswords[target] = loaded; self.savedPasswords[target] = loaded
                    self.connect()
                } catch {
                    guard self.credentialGeneration == generation else { return }
                    self.credentialGeneration = nil
                    self.credentialBusy = false; self.busy = false; self.host.controlling = false
                    self.pairingNotice = "无法读取已记住的密码，请手动填写或重试。"
                }
            }
            return
        }
        do { try PairingCredential.validate(password) }
        catch { status = "请输入局域网 IPv4 和 8–64 位配对密码。"; pairingNotice = status; return }
        sessionAddress = address; sessionPassword = password; sessionRemember = rememberPassword
        targetName = selectedDevice?.name ?? address; targetSystem = selectedDevice?.systemLabel ?? ""
        quality = nextQuality; shortcuts = defaultShortcuts; autoReconnect = defaultAutoReconnect
        wantsConnection = true; hasConnected = false; browser.stop(); searching = false; begin()
    }
    private func authenticated() {
        let target = sessionAddress, secret = sessionPassword
        let record = NearbyDevice(name: DeviceName.valid(targetName) ?? target, address: target, online: false, remembered: known[target]?.remembered == true)
        if known[target] == nil && known.count >= 32 { let removed = known.keys.sorted().first!; known.removeValue(forKey: removed); cachedPasswords.removeValue(forKey: removed); savedPasswords.removeValue(forKey: removed) }
        known[target] = record; cachedPasswords[target] = secret; preferences.set(target, forKey: "controllerAddress"); persistPeers()
        if manualOpen { manualOpen = false; manualPassword = "" }
        guard sessionRemember, savedPasswords[target] != secret else { return }
        credentialBusy = true
        credentialTask = Task { @MainActor [weak self] in
            guard let self else { return }
            defer { self.credentialBusy = false }
            do {
                try await self.credentials.save(address: target, password: secret)
                self.savedPasswords[target] = secret
                if var current = self.known[target] { current.remembered = true; self.known[target] = current }
                self.persistPeers()
            } catch { self.pairingNotice = "已连接，但无法记住密码。" }
        }
    }
    func forgetPassword() {
        guard !busy, !credentialBusy else { return }
        let target = address; credentialBusy = true
        credentialTask = Task { @MainActor [weak self] in
            guard let self else { return }; defer { self.credentialBusy = false }
            do {
                try await self.credentials.forget(address: target)
                self.cachedPasswords.removeValue(forKey: target); self.savedPasswords.removeValue(forKey: target)
                if var record = self.known[target] { record.remembered = false; self.known[target] = record }
                self.password = ""; self.persistPeers(); self.pairingNotice = "已忘记此设备的密码，目标地址保留。"
            } catch { self.pairingNotice = "无法忘记密码，请重试。" }
        }
    }
    private func begin() {
        retry = nil; qualityRestart = false
        let generation = UUID(); token = generation
        busy = true; connected = false; inputEnabled = false; host.controlling = true
        status = "正在连接…"
        frameLock.lock(); frameToken = generation; latest = nil; frameLock.unlock()
        let current = ControllerSession(address: sessionAddress, password: sessionPassword, quality: quality, status: { [weak self] message in
            DispatchQueue.main.async { guard let self, self.token == generation else { return }; self.receive(message) }
        }, frame: { [weak self] image in
            guard let self else { return }
            self.frameLock.lock(); if self.frameToken == generation { self.latest = image }; self.frameLock.unlock()
        }, ended: { [weak self] error in
            DispatchQueue.main.async { guard let self, self.token == generation else { return }; self.ended(error) }
        })
        session = current
        let timer = DispatchSource.makeTimerSource(queue: .main)
        timer.schedule(deadline: .now(), repeating: .milliseconds(16))
        timer.setEventHandler { [weak self] in
            guard let self else { return }
            self.frameLock.lock(); let frame = self.latest; self.latest = nil; self.frameLock.unlock()
            if let frame { self.image = frame }
        }
        presentation = timer; timer.resume(); current.start()
    }
    private func receive(_ message: [String: Any]) {
        switch message["op"] as? String {
        case "capabilities":
            targetName = DeviceName.peer(message, fallback: address); targetSystem = "Windows"
            inputEnabled = message["inputEnabled"] as? Bool == true
        case "video_header": width = message["width"] as? Int ?? 0; height = message["height"] as? Int ?? 0
        case "connected":
            if !hasConnected { authenticated() }
            connected = true; hasConnected = true; status = inputEnabled ? "已连接，点击画面开始操作。" : "已连接，只能查看。"

        case "heartbeat":
            targetName = DeviceName.peer(message, fallback: targetName.isEmpty ? address : targetName)
            if hasConnected, let previous = known[sessionAddress], previous.name != targetName {
                known[sessionAddress] = NearbyDevice(name: targetName, address: sessionAddress, online: false, remembered: previous.remembered)
                persistPeers()
            }
            if let enabled = message["inputEnabled"] as? Bool, enabled != inputEnabled {
                release(); inputEnabled = enabled
                status = enabled ? "已连接，点击画面开始操作。" : "已连接，只能查看。"
            }
            if let blocked = message["inputBlocked"] as? Int, blocked > 0 { status = "Windows 未接受部分输入；请检查目标应用权限。" }
        default: break
        }
    }
    private func ended(_ error: Error?) {
        token = nil; session = nil; connected = false; inputEnabled = false
        presentation?.cancel(); presentation = nil
        frameLock.lock(); frameToken = nil; latest = nil; frameLock.unlock()
        image = nil; width = 0; height = 0; inputGeneration += 1
        if wantsConnection && hasConnected && autoReconnect && error != nil && !(error is ControllerError) && !authentication(error) {
            status = "连接中断，正在重连…"
            let generation = UUID(); token = generation
            let job = DispatchWorkItem { [weak self] in
                guard let self, self.token == generation, self.wantsConnection, self.autoReconnect else { return }
                self.begin()
            }
            retry = job; DispatchQueue.main.asyncAfter(deadline: .now()+3, execute: job)
        } else {
            wantsConnection = false; busy = false; host.controlling = false
            if authentication(error) { status = "身份验证失败，请检查 Windows 的共享密码。"; pairingNotice = status }
            else if let failure = error as? ControllerError {
                switch failure {
                case .overloaded: status = "输入积压，已断开以释放按键。请重新连接。"
                case .video: status = "画面解码失败，请检查 Windows 的编码设置。"
                default: status = "目标能力不兼容，请更新两端应用。"
                }
            } else { status = "连接已断开。请确认 Windows 已开始共享。" }
        }
    }
    private func authentication(_ error: Error?) -> Bool {
        guard let error = error as? WireError else { return false }; if case .authentication = error { return true }; return false
    }
    func disconnect() {
        browser.stop(); searching = false
        if credentialGeneration != nil && !connected && session == nil {
            credentialGeneration = nil; credentialTask?.cancel(); credentialBusy = false
        }
        wantsConnection = false; hasConnected = false; qualityRestart = false; retry?.cancel(); retry = nil; token = nil
        session?.stop(); session = nil; presentation?.cancel(); presentation = nil
        frameLock.lock(); frameToken = nil; latest = nil; frameLock.unlock()
        connected = false; busy = false; inputEnabled = false; host.controlling = false
        nextQuality = defaultQuality
        image = nil; width = 0; height = 0; inputGeneration += 1; status = "未连接"
    }
    func restart() {
        guard busy else { return }
        // Give the one-controller host time to release its previous capture/input.
        // Rapid quality changes cancel the old retry and use the latest choice.
        disconnect(); wantsConnection = true; busy = true; host.controlling = true; qualityRestart = true
        status = "正在切换画质…"
        let generation = UUID(); token = generation
        let job = DispatchWorkItem { [weak self] in
            guard let self, self.token == generation, self.wantsConnection else { return }
            self.begin()
        }
        retry = job; DispatchQueue.main.asyncAfter(deadline: .now()+3, execute: job)
    }
    func send(_ object: [String: Any]) { if connected && inputEnabled { session?.send(object) } }
    func release() { if connected { session?.send(["op":"release_all"]) }; inputGeneration += 1 }
    deinit { browser.stop(); session?.stop(); retry?.cancel(); presentation?.cancel(); credentialTask?.cancel() }
}

struct ControllerView: View {
    @ObservedObject var model: ControllerModel
    @ObservedObject var host: HostModel
    var fullScreen: () -> Void
    var body: some View {
        VStack(spacing: 0) {
            HStack(spacing: 8) {
                Text(model.targetName.isEmpty ? model.address : model.targetName).font(.caption).lineLimit(1)
                if !model.targetSystem.isEmpty { Text(model.targetSystem).font(.caption).foregroundStyle(.secondary) }
                Picker(UiText.text("画质"), selection: $model.quality) {
                    Text(UiText.text("流畅")).tag(0); Text(UiText.text("标准")).tag(1); Text(UiText.text("高清")).tag(2)
                }.labelsHidden().frame(width: 100)
                Spacer(minLength: 4)
                Button(UiText.text(model.busy ? "断开" : "连接")) { if model.busy { model.disconnect() } else { model.connect() } }
                    .disabled(!model.busy && (host.active || host.starting))
                Button(UiText.text("全屏"), action: fullScreen)
                Menu(UiText.text("会话选项")) {
                    Toggle(UiText.text("Mac 快捷键"), isOn: $model.shortcuts)
                    Toggle(UiText.text("自动重连"), isOn: $model.autoReconnect)
                }.fixedSize()
            }.textFieldStyle(.roundedBorder).padding(.horizontal, 12).frame(height: 42)
            Divider()
            RemoteScreen(model: model, fullScreen: fullScreen).frame(maxWidth: .infinity, maxHeight: .infinity)
            Divider()
            Text(UiText.text(host.active || host.starting ? "请先停止这台 Mac 的共享，再连接其他设备。" : model.status))
                .font(.caption).frame(maxWidth: .infinity, alignment: .leading).padding(8)
        }.frame(minWidth: 720, minHeight: 440)
        .onChange(of: model.quality) { _ in model.restart() }
        .onChange(of: model.shortcuts) { _ in model.release() }
    }
}

struct RemoteScreen: NSViewRepresentable {
    @ObservedObject var model: ControllerModel
    var fullScreen: () -> Void
    func makeNSView(context: Context) -> RemoteScreenView { RemoteScreenView(model: model, fullScreen: fullScreen) }
    func updateNSView(_ view: RemoteScreenView, context: Context) { view.update() }
}

final class RemoteScreenView: NSView {
    private let model: ControllerModel, fullScreen: () -> Void
    private var tracking: NSTrackingArea?, modifiers = ControllerModifiers(), buttons = Set<Int>(), generation = 0
    init(model: ControllerModel, fullScreen: @escaping () -> Void) {
        self.model = model; self.fullScreen = fullScreen
        super.init(frame: .zero); wantsLayer = true
        layer?.backgroundColor = NSColor.black.cgColor; layer?.contentsGravity = .resizeAspect
        setAccessibilityLabel(UiText.text("远程桌面")); setAccessibilityRole(.image)
        NotificationCenter.default.addObserver(self, selector: #selector(lostFocus), name: NSWindow.didResignKeyNotification, object: nil)
    }
    required init?(coder: NSCoder) { fatalError("Programmatic view") }
    override var isFlipped: Bool { true }
    override var acceptsFirstResponder: Bool { true }
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
    func update() {
        layer?.contents = model.image
        if generation != model.inputGeneration { generation = model.inputGeneration; modifiers.reset(); buttons.removeAll() }
    }
    override func updateTrackingAreas() {
        if let tracking { removeTrackingArea(tracking) }
        tracking = NSTrackingArea(rect: .zero, options: [.mouseMoved,.activeInKeyWindow,.inVisibleRect], owner: self)
        addTrackingArea(tracking!); super.updateTrackingAreas()
    }
    @objc private func lostFocus(_ notification: Notification) { if notification.object as? NSWindow === window { model.release(); modifiers.reset(); buttons.removeAll() } }
    override func resignFirstResponder() -> Bool { model.release(); modifiers.reset(); buttons.removeAll(); return super.resignFirstResponder() }
    private func position(_ event: NSEvent, clamp: Bool) -> (Double,Double)? {
        let p = convert(event.locationInWindow, from: nil)
        return ControllerRules.position(x: p.x, y: p.y, width: bounds.width, height: bounds.height,
                                        videoWidth: model.width, videoHeight: model.height, clamp: clamp)
    }
    private func mouse(_ event: NSEvent, button: Int, down: Bool) {
        guard model.connected, model.inputEnabled, let p = position(event, clamp: !down || !buttons.isEmpty) else { return }
        if down { window?.makeFirstResponder(self); buttons.insert(button) } else { buttons.remove(button) }
        model.send(["op":"mouse_button","x":p.0,"y":p.1,"button":button,"down":down])
    }
    override func mouseDown(with event: NSEvent) { mouse(event, button: 0, down: true) }
    override func mouseUp(with event: NSEvent) { mouse(event, button: 0, down: false) }
    override func rightMouseDown(with event: NSEvent) { mouse(event, button: 1, down: true) }
    override func rightMouseUp(with event: NSEvent) { mouse(event, button: 1, down: false) }
    override func otherMouseDown(with event: NSEvent) { if event.buttonNumber == 2 { mouse(event, button: 2, down: true) } }
    override func otherMouseUp(with event: NSEvent) { if event.buttonNumber == 2 { mouse(event, button: 2, down: false) } }
    override func mouseMoved(with event: NSEvent) {
        guard let p = position(event, clamp: !buttons.isEmpty) else { return }
        model.send(["op":"mouse_move","x":p.0,"y":p.1])
    }
    override func mouseDragged(with event: NSEvent) { mouseMoved(with: event) }
    override func rightMouseDragged(with event: NSEvent) { mouseMoved(with: event) }
    override func otherMouseDragged(with event: NSEvent) { mouseMoved(with: event) }
    override func scrollWheel(with event: NSEvent) {
        guard let p = position(event, clamp: false) else { return }
        let value = min(1200,max(-1200,Int(event.scrollingDeltaY*(event.hasPreciseScrollingDeltas ? 10 : 120))))
        if value != 0 { model.send(["op":"scroll","x":p.0,"y":p.1,"delta":value]) }
    }
    override func keyDown(with event: NSEvent) {
        if event.keyCode == 53 && event.modifierFlags.contains([.control,.option]) { window?.makeFirstResponder(nil); return }
        if event.keyCode == 103 { model.release(); fullScreen(); return }
        for edge in modifiers.synchronize(flags: event.modifierFlags.rawValue, shortcuts: model.shortcuts) { model.send(edge) }
        if let vk = ControllerKeys.vk(event.keyCode, shortcuts: model.shortcuts) { model.send(["op":"key_event","vk":vk,"down":true]) }
    }
    override func keyUp(with event: NSEvent) {
        if event.keyCode == 103 || event.keyCode == 53 && event.modifierFlags.contains([.control,.option]) { return }
        if let vk = ControllerKeys.vk(event.keyCode, shortcuts: model.shortcuts) { model.send(["op":"key_event","vk":vk,"down":false]) }
    }
    override func performKeyEquivalent(with event: NSEvent) -> Bool {
        if window?.firstResponder === self && model.connected && model.inputEnabled { keyDown(with: event); return true }
        return super.performKeyEquivalent(with: event)
    }
    override func flagsChanged(with event: NSEvent) {
        for edge in modifiers.changed(code: event.keyCode, flags: event.modifierFlags.rawValue, shortcuts: model.shortcuts) { model.send(edge) }
    }
    deinit { NotificationCenter.default.removeObserver(self) }
}

final class ControllerWindow: NSWindow {
    override func sendEvent(_ event: NSEvent) {
        if event.type == .keyUp, event.modifierFlags.contains(.command), let screen = firstResponder as? RemoteScreenView {
            screen.keyUp(with: event); return
        }
        super.sendEvent(event)
    }
}
