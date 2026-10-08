import AppKit
import ApplicationServices
final class InputInjector {
    static var bounds = CGRect.zero
    private var keys = Set<CGKeyCode>(), buttons = Set<Int>()
    private var point = CGPoint.zero
    private var lastClick = Date.distantPast, lastClickPoint = CGPoint.zero, lastButton = -1, clickCount = 0
    private let source = CGEventSource(stateID: .hidSystemState)
    private let map: [Int: CGKeyCode] = [
        65:0,83:1,68:2,70:3,72:4,71:5,90:6,88:7,67:8,86:9,66:11,81:12,87:13,69:14,82:15,89:16,84:17,
        49:18,50:19,51:20,52:21,54:22,53:23,187:24,57:25,55:26,189:27,56:28,48:29,221:30,79:31,85:32,219:33,73:34,80:35,
        13:36,76:37,74:38,222:39,75:40,186:41,220:42,188:43,191:44,78:45,77:46,190:47,9:48,32:49,192:50,8:51,27:53,
        91:55,92:54,16:56,160:56,161:60,20:57,18:58,164:58,165:61,17:59,162:59,163:62,
        112:122,113:120,114:99,115:118,116:96,117:97,118:98,119:100,120:101,121:109,122:103,123:111,
        36:115,33:116,46:117,35:119,34:121,37:123,39:124,40:125,38:126,
        96:82,97:83,98:84,99:85,100:86,101:87,102:88,103:89,104:91,105:92,110:65,106:67,107:69,111:75,109:78]
    func handle(_ m: [String: Any]) {
        guard AXIsProcessTrusted(), let op = m["op"] as? String else { return }
        if let x = m["x"] as? Double, let y = m["y"] as? Double, x.isFinite, y.isFinite {
            point = CGPoint(x: Self.bounds.minX + min(max(x,0),1)*max(0,Self.bounds.width-1), y: Self.bounds.minY + min(max(y,0),1)*max(0,Self.bounds.height-1))
        }
        switch op {
        case "mouse_move":
            let type: CGEventType = buttons.contains(0) ? .leftMouseDragged : buttons.contains(1) ? .rightMouseDragged : buttons.contains(2) ? .otherMouseDragged : .mouseMoved
            CGEvent(mouseEventSource: source, mouseType: type, mouseCursorPosition: point, mouseButton: buttons.contains(1) ? .right : buttons.contains(2) ? .center : .left)?.post(tap: .cghidEventTap)
        case "mouse_button":
            guard let b = m["button"] as? Int, (0...2).contains(b), let down = m["down"] as? Bool else { return }
            if down {
                let now = Date()
                let nearby = hypot(point.x-lastClickPoint.x, point.y-lastClickPoint.y) < 5
                clickCount = b == lastButton && nearby && now.timeIntervalSince(lastClick) <= NSEvent.doubleClickInterval ? min(clickCount+1,3) : 1
                lastClick = now; lastClickPoint = point; lastButton = b; buttons.insert(b)
            } else { buttons.remove(b) }
            let type: CGEventType = b == 0 ? (down ? .leftMouseDown : .leftMouseUp) : b == 1 ? (down ? .rightMouseDown : .rightMouseUp) : (down ? .otherMouseDown : .otherMouseUp)
            let event = CGEvent(mouseEventSource: source, mouseType: type, mouseCursorPosition: point, mouseButton: b == 0 ? .left : b == 1 ? .right : .center)
            event?.setIntegerValueField(.mouseEventClickState, value: Int64(clickCount)); event?.post(tap: .cghidEventTap)
        case "scroll":
            guard let delta = m["delta"] as? Int else { return }
            CGEvent(scrollWheelEvent2Source: source, units: .pixel, wheelCount: 1, wheel1: Int32(min(max(delta,-1200),1200)), wheel2: 0, wheel3: 0)?.post(tap: .cghidEventTap)
        case "key_event":
            guard let vk = m["vk"] as? Int, let code = map[vk], let down = m["down"] as? Bool else { return }
            let repeated = down && keys.contains(code)
            if down { keys.insert(code) } else { keys.remove(code) }
            let e = CGEvent(keyboardEventSource: source, virtualKey: code, keyDown: down)
            e?.setIntegerValueField(.keyboardEventAutorepeat, value: repeated ? 1 : 0)
            let f = m["flags"] as? Int ?? 0; var flags: CGEventFlags = []
            if f&1 != 0 { flags.insert(.maskShift) }; if f&2 != 0 { flags.insert(.maskControl) }; if f&4 != 0 { flags.insert(.maskAlternate) }; if f&8 != 0 { flags.insert(.maskCommand) }
            // Preserve keypad identity when replacing the CGEvent's default flags.
            // Without this flag a keypad key code is not a complete keypad event.
            if (96...111).contains(vk) { flags.insert(.maskNumericPad) }
            e?.flags = flags; e?.post(tap: .cghidEventTap)
        case "release_all": release()
        default: break
        }
    }
    func release() {
        for key in keys { let e = CGEvent(keyboardEventSource: source, virtualKey: key, keyDown: false); e?.flags = []; e?.post(tap: .cghidEventTap) }; keys.removeAll()
        for b in Array(buttons) { handle(["op":"mouse_button","button":b,"down":false]) }; buttons.removeAll()
    }
}
