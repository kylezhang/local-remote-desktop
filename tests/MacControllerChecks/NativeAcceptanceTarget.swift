import AppKit
import CoreGraphics

// Opt-in dedicated target for the installed Host's real HID input. This is not
// a substitute for TCC/Keychain approval, and never reads the user's documents.
@main enum NativeAcceptanceTarget {
    static func main() {
        let root = URL(fileURLWithPath: CommandLine.arguments[1],isDirectory: true)
        let app = NSApplication.shared, previous = NSWorkspace.shared.frontmostApplication
        let w = NSWindow(contentRect: NSRect(x:0,y:0,width:480,height:180),styleMask:[.titled,.closable],backing:.buffered,defer:false)
        w.title = "Local Remote Desktop · Mac acceptance target"; w.isReleasedWhenClosed = false
        let label = NSTextField(labelWithString:"Temporary remote input test — letters, digits, click and wheel")
        label.frame = NSRect(x:16,y:122,width:448,height:32); w.contentView?.addSubview(label)
        let field = NSTextField(frame:NSRect(x:16,y:65,width:448,height:32)); w.contentView?.addSubview(field)
        var keys = [UInt16](), clicks = 0, wheels = 0
        let monitor = NSEvent.addLocalMonitorForEvents(matching:[.keyDown,.leftMouseDown,.scrollWheel]) { event in
            if event.window === w {
                if event.type == .keyDown { keys.append(event.keyCode) }
                if event.type == .leftMouseDown { clicks += 1 }
                if event.type == .scrollWheel { wheels += 1 }
            }
            return event
        }
        app.setActivationPolicy(.regular); w.center(); app.activate(ignoringOtherApps:true); w.makeKeyAndOrderFront(nil); w.makeFirstResponder(field)
        let point = w.convertPoint(toScreen: field.convert(NSPoint(x:field.bounds.midX,y:field.bounds.midY),to:nil))
        let screen = NSScreen.main!.frame, bounds = CGDisplayBounds(CGMainDisplayID())
        let x = (point.x-bounds.minX)/bounds.width, y = (screen.maxY-point.y-bounds.minY)/bounds.height
        let start = Date()
        _ = Timer.scheduledTimer(withTimeInterval:0.1,repeats:true) { timer in
            let value = field.currentEditor()?.string ?? field.stringValue
            let matched = value.lowercased() == "a11"
            let success = matched && clicks > 0 && wheels > 0
            let record: [String:Any] = ["owned":w.isKeyWindow && field.currentEditor() != nil,"x":x,"y":y,
                "textMatched":matched,"keys":keys,"clicks":clicks,"wheels":wheels,"pass":success,
                "timestamp":Date().timeIntervalSince1970,"pid":ProcessInfo.processInfo.processIdentifier]
            if let data = try? JSONSerialization.data(withJSONObject:record) { try? data.write(to:root.appendingPathComponent("target.json"),options:.atomic) }
            if success || Date().timeIntervalSince(start)>120 {
                timer.invalidate(); if let monitor { NSEvent.removeMonitor(monitor) }
                let owned = w.isKeyWindow; w.orderOut(nil); w.close()
                var final = record; final["owned"] = false
                if let data = try? JSONSerialization.data(withJSONObject:final) { try? data.write(to:root.appendingPathComponent("target.json"),options:.atomic) }
                if owned { previous?.activate(options:[.activateIgnoringOtherApps]) }
                app.terminate(nil)
            }
        }
        app.run()
    }
}
