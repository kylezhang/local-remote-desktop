import AppKit
import ApplicationServices
import Darwin

// Explicit local self-test, inside the installed Host identity/TCC context.
// Only targets this temporary field; never types into the user's open documents.
final class InputSelfTest: NSObject, NSWindowDelegate {
    private var window: NSWindow?
    private var field: NSTextField?
    private var finish: ((String) -> Void)?
    // Explicitly target only this process even if focus changes during delivery.
    // Network controls retain the system HID tap; this checks mapping/delivery
    // into our process, not acceptance by other applications or secure fields.
    private let input = InputInjector(targetPID: getpid())
    func run(completion: @escaping (String) -> Void) {
        guard window == nil else { return }
        guard AXIsProcessTrusted() else { completion("自检未执行：系统尚未认可本应用的辅助功能权限。"); return }
        finish = completion
        let w = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 460, height: 130), styleMask: [.titled, .closable], backing: .buffered, defer: false)
        w.title = "LocalRemote · 本机键盘自检"; w.isReleasedWhenClosed = false; w.delegate = self
        let label = NSTextField(labelWithString: "将在下面的测试框注入字母和两组数字；不会输入到其他应用。")
        label.frame = NSRect(x: 16, y: 85, width: 428, height: 28)
        let f = NSTextField(frame: NSRect(x: 16, y: 34, width: 428, height: 30))
        w.contentView?.addSubview(label); w.contentView?.addSubview(f)
        window = w; field = f; w.center(); NSApplication.shared.activate(ignoringOtherApps: true); w.makeKeyAndOrderFront(nil); w.makeFirstResponder(f)
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) { [weak self] in self?.typeSample() }
    }
    private func typeSample() {
        guard let w = window, w.isKeyWindow, let f = field,
              let editor = f.currentEditor(), w.firstResponder === editor else {
            end("自检已取消：测试框失去焦点。"); return
        }
        // a + top-row 0..9 + keypad 0..9. Same InputInjector as network controls.
        let values = [65] + Array(48...57) + Array(96...105)
        for vk in values {
            input.handle(["op":"key_event", "vk":vk, "down":true, "flags":0])
            input.handle(["op":"key_event", "vk":vk, "down":false, "flags":0])
        }
        check(remaining: 40)
    }
    private func check(remaining: Int) {
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.05) { [weak self] in
            guard let self, let w = self.window, let f = self.field else { return }
            guard w.isKeyWindow else { self.end("自检已取消：窗口失去焦点。"); return }
            let actual = f.currentEditor()?.string ?? f.stringValue
            if actual == "a01234567890123456789" {
                self.end("本机键盘自检通过：字母、主键盘数字、小键盘数字均被测试框接收。")
            } else if remaining > 0 { self.check(remaining: remaining - 1) }
            else { self.end("本机键盘自检未通过：已提交 \(self.input.posted) 个事件，测试框收到 \(actual.count) 个字符。请确认 Mac 输入法为英文后复测；这不代表远程链路已通过。") }
        }
    }
    private func end(_ result: String) {
        let completion = finish; finish = nil
        input.release(); window?.orderOut(nil); window?.close(); window = nil; field = nil
        completion?(result)
    }
    func windowWillClose(_ notification: Notification) { if finish != nil { end("自检已取消。") } }
}
