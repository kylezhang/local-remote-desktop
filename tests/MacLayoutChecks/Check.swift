import AppKit
import SwiftUI

// Render only this harness's unshown views. No AppDelegate, real browser,
// credentials, pasteboard, connection or sharing operation is started.
final class LayoutCredentials: ControllerCredentials {
    func load(address: String) async throws -> String? { fatalError("Unexpected credential read") }
    func save(address: String, password: String) async throws { fatalError("Unexpected credential save") }
    func forget(address: String) async throws { fatalError("Unexpected credential delete") }
}
final class LayoutBrowser: ControllerBrowsing {
    var changed: (([NearbyDevice], Bool) -> Void)?
    func start() { changed?([], false) }
    func stop() {}
}
struct LayoutBoard: ControllerFileClipboard {
    var revision: Int { fatalError("Unexpected clipboard access") }
    func read() -> (Bool, [String]?) { fatalError("Unexpected clipboard read") }
    func write(_ paths: [String]) -> Bool { fatalError("Unexpected clipboard write") }
}
struct LayoutTransport: ControllerFileTransport {
    func start(_ request: ControllerFileRequest, publish: @escaping ([String]) -> Bool,
               progress: @escaping (String) -> Void, completion: @escaping (Result<Bool, Error>) -> Void) -> ControllerFileTask {
        fatalError("Unexpected file transfer")
    }
}

@main struct MacLayoutCheck {
    static var category = "general"
    @MainActor static func render<V: View>(_ view: V, name: String, size: NSSize, output: URL) throws {
        let window = NSWindow(contentRect: NSRect(origin: .zero, size: size), styleMask: [.borderless], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.appearance = NSAppearance(named: .aqua)
        let hosting = NSHostingView(rootView: view.frame(width: size.width, height: size.height, alignment: .topLeading)
            .background(Color.white).foregroundStyle(Color.primary)
            .tint(Color(red: 82.0/255, green: 101.0/255, blue: 236.0/255)).environment(\.colorScheme, .light))
        window.contentView = hosting
        hosting.frame = NSRect(origin: .zero, size: size)
        for _ in 0..<8 {
            hosting.needsLayout = true; hosting.layoutSubtreeIfNeeded()
            RunLoop.main.run(until: Date().addingTimeInterval(0.025))
        }
        precondition(!window.isVisible && !window.isKeyWindow && !window.isMainWindow, "Background check showed a window")
        guard let bitmap = hosting.bitmapImageRepForCachingDisplay(in: hosting.bounds) else { fatalError("Cannot render native view") }
        hosting.cacheDisplay(in: hosting.bounds, to: bitmap)
        guard let png = bitmap.representation(using: .png, properties: [:]) else { fatalError("Cannot encode native view") }
        try png.write(to: output.appendingPathComponent(name + ".png"))
        // Blank images are not native layout evidence.
        var visible = 0
        for y in stride(from: 0, to: bitmap.pixelsHigh, by: 4) {
            for x in stride(from: 0, to: bitmap.pixelsWide, by: 4) {
                if let color = bitmap.colorAt(x: x, y: y)?.usingColorSpace(.deviceRGB),
                   min(color.redComponent, color.greenComponent, color.blueComponent) < 0.90 { visible += 1 }
            }
        }
        precondition(visible > 50, "Native image is empty")
        var scrolls = [(CGFloat, CGFloat)]()
        func inspect(_ node: NSView) {
            if let scroll = node as? NSScrollView, let document = scroll.documentView {
                scrolls.append((document.frame.height, scroll.contentView.bounds.height))
            }
            for child in node.subviews { inspect(child) }
        }
        inspect(hosting)
        let overflow = scrolls.filter { $0.0 > $0.1 + 1 }.count
        print("RENDER \(name): \(bitmap.pixelsWide)x\(bitmap.pixelsHigh), native scroll containers=\(scrolls.count), overflowing=\(overflow)")
        // SwiftUI can use non-NSScrollView containers; only inspected containers
        // are measured. Saved native renders need visual review as well.
        precondition(overflow == 0, "Routine content exceeds its native viewport")
        window.contentView = nil
    }
    @MainActor static func main() throws {
        NSApplication.shared.setActivationPolicy(.prohibited)
        let output = URL(fileURLWithPath: CommandLine.arguments[1], isDirectory: true)
        try FileManager.default.createDirectory(at: output, withIntermediateDirectories: true)
        let suite = "LocalRemoteDesktop.LayoutChecks." + UUID().uuidString
        let prefs = UserDefaults(suiteName: suite)!
        defer { prefs.removePersistentDomain(forName: suite) }
        let arguments = UserDefaults.standard.volatileDomain(forName: UserDefaults.argumentDomain)
        defer { UserDefaults.standard.setVolatileDomain(arguments, forName: UserDefaults.argumentDomain) }
        precondition(AppBrand.entryArt.count == 2, "Approved entry art not bundled")
        let size = NSSize(width: 600, height: 658) // 680 frame less native title bar.
        for language in ["en", "zh-Hans"] {
            UserDefaults.standard.setVolatileDomain(["language": language], forName: UserDefaults.argumentDomain)
            let host = HostModel(namePreferences: prefs)
            host.deviceName = "Studio Mac"
            host.status = "点击开始接受连接。会先检查屏幕权限，再读取配对密码。"
            let text = TextClipboardSync(revision: { fatalError("Unexpected pasteboard access") },
                read: { fatalError("Unexpected pasteboard read") }, write: { _ in fatalError("Unexpected pasteboard write") })
            let controller = ControllerModel(host: host, preferences: prefs, credentials: LayoutCredentials(),
                browser: LayoutBrowser(), textSync: text, fileBoard: LayoutBoard(), fileTransport: LayoutTransport())
            UiText.language = language
            for page in ["home", "connect", "share"] {
                let navigation = DesktopNavigation(); navigation.page = page
                try render(DesktopShell(host: host, controller: controller, navigation: navigation, fullScreen: {}),
                    name: language + "-" + page, size: size, output: output)
                precondition(!host.active && !host.starting && !controller.busy, "Rendering started a role")
            }
            for next in ["general", "connection", "clipboard", "about"] {
                category = next
                let navigation = DesktopNavigation(); navigation.page = "settings"
                try render(DesktopShell(host: host, controller: controller, navigation: navigation, fullScreen: {}),
                    name: language + "-settings-" + next, size: size, output: output)
            }
            try render(ManualConnectionView(model: controller, host: host), name: language + "-target",
                size: NSSize(width: 476, height: 500), output: output)
            try render(PasswordEditorView(model: host), name: language + "-password",
                size: NSSize(width: 468, height: 380), output: output)
            precondition(host.secret.isEmpty && host.keychainCheck == "尚未读取", "Rendering read a host credential")
        }
        print("PASS native unshown EN/ZH entry/list/accept/four settings/target/password renders; no role or credential access")
    }
}
