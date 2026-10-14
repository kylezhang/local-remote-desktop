import Foundation
import Combine
import AppKit
import ApplicationServices
import ScreenCaptureKit
final class HostModel: ObservableObject {
    @Published var languageCode = UiText.preferredLanguage { didSet {
        UiText.language = languageCode; UserDefaults.standard.set(languageCode, forKey: "language")
    } }
    @Published var status = "准备启动"
    @Published var deviceName = ""
    private let namePreferences: UserDefaults
    @Published var editingName = false
    @Published var nameDraft = ""
    @Published var nameError = ""
    func openNameEditor() { guard !credentialBusy, !editingPassword, !controlling else { return }; nameDraft = deviceName; nameError = ""; editingName = true }
    func cancelNameEditor() { nameDraft = ""; nameError = ""; editingName = false }
    func restoreSystemName() { nameDraft = DeviceName.system; nameError = "" }
    func saveName() {
        do {
            deviceName = try DeviceName.save(nameDraft, preferences: namePreferences); host?.setDeviceName(deviceName)
            if active { discovery.start(name: deviceName) }
            cancelNameEditor()
        } catch { nameError = error.localizedDescription }
    }
    @Published var secret = ""
    @Published var active = false
    @Published var controlling = false
    @Published var starting = false
    @Published var screenCheck = "尚未检查"
    @Published var keychainCheck = "尚未读取"
    // Avoid SwiftUI.State's SDK macro dependency in Command Line Tools builds.
    @Published var revealSecret = false
    @Published var readingPassword = false
    @Published var pairingMessage = ""
    @Published var editingPassword = false
    @Published var passwordEditor = PairingEditor()
    @Published var allowClipboard = SharingPreferences.enabled("textClipboardAllowed") { didSet { UserDefaults.standard.set(allowClipboard, forKey: "textClipboardAllowed"); host?.setClipboardAllowed(allowClipboard) } }
    @Published var allowFiles = SharingPreferences.enabled("fileClipboardAllowed") { didSet { UserDefaults.standard.set(allowFiles, forKey: "fileClipboardAllowed"); host?.setFilesAllowed(allowFiles) } }
    @Published var fileStatus = "文件同步：等待连接"
    private var host: Host?
    private let discovery = HostDiscovery()
    private let preparation = SharingPreparation<PairingCredential>()
    private let screenAccess = ScreenSharingAccess()
    private var pairingKey: PairingCredential? { preparation.cachedKey }
    private var startup: Task<Void, Never>?
    private var pairingOperation: Task<Void, Never>?
    private var passwordOperation: Task<Void, Never>?
    private var hostGeneration: UUID?
    private let inputSelfTest = InputSelfTest()
    @Published var inputTestResult = ""
    @Published var inputAccess = MacInputAccess.snapshot()
    func refreshInputAccess() { inputAccess = MacInputAccess.snapshot() }
    func testInput() { inputSelfTest.run { [weak self] result in self?.inputTestResult = result } }
    func openAccessibilitySettings() {
        MacInputAccess.request()
        refreshInputAccess()
        NSWorkspace.shared.open(URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")!)
    }
    func revealInstalledApplication() { NSWorkspace.shared.activateFileViewerSelecting([Bundle.main.bundleURL]) }
    init(namePreferences: UserDefaults = .standard) {
        self.namePreferences = namePreferences; deviceName = DeviceName.current(namePreferences)
        UiText.language = languageCode
    }
    func cancelFiles() { host?.cancelFiles() }
    func openReceivedFiles() {
        let root = MacFiles.receivedDirectory
        do { try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true); NSWorkspace.shared.open(root) }
        catch { fileStatus = "无法打开接收目录：\(error.localizedDescription)" }
    }
    var credentialBusy: Bool { starting || readingPassword || passwordEditor.saving }
    var sharingActionAvailable: Bool {
        !controlling && (active || (!credentialBusy && !editingName && !editingPassword))
    }
    var legacyCredential: Bool { pairingKey?.salt == nil && !secret.isEmpty }
    func openPasswordEditor() {
        guard !credentialBusy, !editingName, !controlling else { return }
        passwordEditor.reset(); pairingMessage = ""; editingPassword = true
    }
    func cancelPasswordEditor() {
        guard !passwordEditor.saving else { return }
        passwordEditor.reset(); editingPassword = false
    }
    func generatePassword() {
        passwordEditor.generate(PairingCredential.generated(using: randomBytes(8)))
    }
    func savePassword() {
        guard !starting, !readingPassword,
              let password = passwordEditor.beginSave(current: secret) else { return }
        passwordOperation = Task { @MainActor [weak self] in
            guard let self else { return }
            do {
                try Task.checkCancellation()
                let credential = try await Pairing.savePassword(password)
                // Persistence succeeded. Adopt it even if stop/quit cancelled the
                // waiting task; cancellation must never restart sharing afterward.
                self.hostGeneration = nil
                self.discovery.stop(); self.host?.stop(); self.host = nil; self.active = false
                self.fileStatus = "文件同步：等待连接"
                self.preparation.replaceCachedKey(credential); self.displayPairingKey(credential)
                self.revealSecret = false
                self.passwordEditor.reset(); self.editingPassword = false
                if !Task.isCancelled {
                    self.status = "密码已保存，原连接已断开；点击开始接受连接后使用新密码连接。"
                    self.pairingMessage = ""
                }
            } catch is CancellationError {
                self.passwordEditor.reset(); self.editingPassword = false
            } catch {
                // Keep the draft and current session; errors belong to this editor.
                self.passwordEditor.failed("未能保存，原密码仍有效：\(error.localizedDescription)")
            }
        }
    }
    func togglePasswordVisibility() {
        guard !credentialBusy else { return }
        if secret.isEmpty { retryPairing() } else { revealSecret.toggle() }
    }
    func copyPassword() {
        guard !secret.isEmpty else { return }
        NSPasteboard.general.clearContents(); NSPasteboard.general.setString(secret, forType: .string)
        pairingMessage = "密码已复制。"
    }
    func retryPairing() {
        guard !credentialBusy, pairingKey == nil else { return }
        readingPassword = true; pairingMessage = "正在读取密码…"
        pairingOperation = Task { @MainActor [weak self] in
            guard let self else { return }
            defer { self.readingPassword = false }
            do {
                let key = try await self.preparation.readKey { try await self.fetchPairingKey(allowInteraction: true) }
                self.displayPairingKey(key); self.revealSecret = true; self.pairingMessage = ""
            } catch is CancellationError { }
            catch {
                let e = error as NSError
                self.keychainCheck = "\(e.domain) / \(e.code)"
                self.pairingMessage = "无法读取密码：\(e.localizedDescription)。可点击查看密码重试。"
            }
        }
    }
    @MainActor private func fetchPairingKey(allowInteraction: Bool) async throws -> PairingCredential {
        keychainCheck = allowInteraction ? "用户主动读取" : "启动静默读取"
        return try await Pairing.load(allowInteraction: allowInteraction)
    }
    private func displayPairingKey(_ key: PairingCredential) {
        secret = key.text
        keychainCheck = "读取成功（本次运行已缓存）"
    }
    private func reportKeychainError(_ error: Error) {
        let e = error as NSError
        keychainCheck = "\(e.domain) / \(e.code)"
        status = "无法读取配对密码：\(e.localizedDescription)。请在“当前密码”旁点击“查看”重试。"
    }
    func start(requestPermission: Bool = false) {
        guard !controlling else { status = "正在控制其他设备；请先断开连接，再共享这台 Mac。"; return }
        guard host == nil, !credentialBusy, !editingName, !editingPassword else { return }
        if !requestPermission && pairingKey == nil && !Pairing.permitsAutomaticRead {
            keychainCheck = "等待用户操作（启动未访问钥匙串）"
            status = "点击开始接受连接。会先检查屏幕权限，再读取配对密码。"
            return
        }
        starting = true; status = "正在检查屏幕访问…"
        startup = Task { @MainActor [weak self] in
          guard let self else { return }
          defer { self.starting = false }
          // Explicit sharing checks ScreenCaptureKit even if the legacy
          // preflight reports false. Login must not request screen permission.
          if !requestPermission && !CGPreflightScreenCaptureAccess() {
            self.status = "尚未启动：点击开始接受连接，检查屏幕访问。授权后只需退出重开，不要重新安装。"
            return
          }
          var checkingScreen = true
          do {
            var selectedScreen: ScreenCaptureSource?
            let key = try await self.preparation.prepare(checkScreen: {
                self.status = "请在系统选择器中选择这台 Mac 的主屏幕。"
                self.screenCheck = "等待屏幕选择"
                selectedScreen = try await self.screenAccess.select(interactive: requestPermission)
                self.screenCheck = "屏幕选择已授权"
            }, readKey: {
                checkingScreen = false
                self.status = "正在读取配对密码…"
                return try await self.fetchPairingKey(allowInteraction: requestPermission)
            })
            // prepare can reuse an already cached key without invoking readKey.
            checkingScreen = false
            self.displayPairingKey(key)
            self.refreshInputAccess()
            let trusted = self.inputAccess.canControl
            let generation = UUID(); self.hostGeneration = generation
            let h = Host(key: key.key, deviceName: self.deviceName, screenSource: selectedScreen, screenAccessEnded: { [weak self] in
                DispatchQueue.main.async {
                    guard let self, self.hostGeneration == generation else { return }
                    self.screenAccess.invalidate(); self.stop()
                    self.status = "屏幕共享已结束，请重新选择屏幕。"
                }
            }, passwordSalt: key.salt, clipboardAllowed: self.allowClipboard, filesAllowed: self.allowFiles,
                fileStatus: { [weak self] text in DispatchQueue.main.async {
                    guard let self, self.hostGeneration == generation else { return }
                    self.fileStatus = text
                } }) { [weak self] text in DispatchQueue.main.async {
                    guard let self, self.hostGeneration == generation else { return }
                    self.status = text
                } }
            try h.start(); self.host = h; self.active = true; self.discovery.start(name: self.deviceName)
            self.status = trusted ? "共享已启动，等待设备连接 · TCP 47980" : "共享已启动（只能查看）· 系统尚未允许键盘和鼠标控制"
          } catch is CancellationError { if !Task.isCancelled && !self.active { self.status = "未选择屏幕，尚未接受连接。" } }
          catch {
            let e = error as NSError
            if checkingScreen {
                self.screenCheck = "失败：\(e.domain) / \(e.code)"
                if error as? ScreenSharingError == .chooseMainDisplay {
                    self.status = "请选择主显示器；macOS 14 至 15.1 需使用单显示器。"
                } else {
                    self.status = "屏幕选择失败：\(e.domain) / \(e.code)：\(e.localizedDescription)。可重试屏幕选择；本次没有读取钥匙串。"
                }
            } else if self.pairingKey == nil { self.reportKeychainError(error) }
            else { self.status = "共享启动失败：\(e.domain) / \(e.code)：\(e.localizedDescription)" }
          }
        }
    }
    func stop() {
        startup?.cancel(); pairingOperation?.cancel(); passwordOperation?.cancel()
        screenAccess.cancelPending()
        discovery.stop(); hostGeneration = nil; host?.stop(); host = nil; active = false
        revealSecret = false; status = "已停止"; fileStatus = "文件同步：等待连接"
    }
    func copyDiagnostics() {
        let text = """
        LocalRemoteDesktop \(Bundle.main.object(forInfoDictionaryKey: "LocalRemoteDesktopReleaseVersion") as? String ?? Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "unknown")
        Build: \(Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? "unknown")
        SourceCommit: \(Bundle.main.url(forResource: "source-revision", withExtension: "txt").flatMap { try? String(contentsOf: $0, encoding: .utf8).trimmingCharacters(in: .whitespacesAndNewlines) } ?? "unknown")
        InterfaceRevision: entry-2026-10-12-r3
        Executable: \(Bundle.main.executablePath ?? "unknown")
        macOS: \(ProcessInfo.processInfo.operatingSystemVersionString)
        InstalledAtApplications: \(Bundle.main.bundlePath == "/Applications/Local Remote Desktop.app")
        AppIconLoaded: \(AppBrand.icon != nil)
        BuildSigningMode: \(Pairing.buildSigningMode)
        LegacyScreenPreflight: \(CGPreflightScreenCaptureAccess())
        ScreenAuthorization: \(screenAccess.mode)
        ScreenCaptureKit: \(screenCheck)
        Accessibility: \(AXIsProcessTrusted())
        EventPosting: \(MacInputAccess.snapshot().eventPosting)
        Listening: \(active)
        Discovery: \(discovery.state)
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
