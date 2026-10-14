import Foundation
@main struct Check {
    static func rejects(_ action: () throws -> Void) { do { try action(); fatalError("Invalid input accepted") } catch {} }
    static func main() throws {
        for ip in ["10.1.2.3","172.16.0.2","172.31.255.3","192.168.8.2","169.254.1.3","127.0.0.1"] { let result = try ControllerRules.address(ip); precondition(result == ip) }
        let address = try ControllerRules.address(" 192.168.001.010 \n"); precondition(address == "192.168.1.10")
        for ip in ["8.8.8.8","172.32.0.1","172.15.0.1","192.168.2.256","192.168.-1.2","192.168.1","192.168..1","localhost","::1","192.168.１.2"] { rejects { _ = try ControllerRules.address(ip) } }
        let enabled = try ControllerRules.capabilities(["platform":"windows","inputModel":"win-vk-v1","inputEnabled":true]); precondition(enabled)
        rejects { _ = try ControllerRules.capabilities(["platform":"macos","inputModel":"win-vk-v1","inputEnabled":true]) }
        rejects { _ = try ControllerRules.capabilities(["platform":"windows","inputModel":"other"]) }
        let header: [String: Any] = ["codec":"h264-annexb","width":1920,"height":1080,"fps":60]
        let size = try ControllerRules.header(header); precondition(size.0 == 1920 && size.1 == 1080)
        for bad in [["codec":"raw","width":1920,"height":1080,"fps":60], ["codec":"h264-annexb","width":4096,"height":4096,"fps":60], ["codec":"h264-annexb","width":1920,"height":1080,"fps":1000]] as [[String: Any]] { rejects { _ = try ControllerRules.header(bad) } }
        let p = ControllerRules.position(x:500,y:300,width:1000,height:600,videoWidth:1920,videoHeight:1080,clamp:false)!
        precondition(abs(p.0-0.5)<0.001 && abs(p.1-0.5)<0.001)
        precondition(ControllerRules.position(x:0,y:0,width:1000,height:600,videoWidth:1920,videoHeight:1080,clamp:false) == nil)
        let drag = ControllerRules.position(x:-100,y:900,width:1000,height:600,videoWidth:1920,videoHeight:1080,clamp:true)!
        precondition(drag.0 == 0 && drag.1 == 1)
        print("PASS LAN address/capability/video bounds and aspect-fit pointer/drag rules")
        precondition(ControllerKeys.vk(55,shortcuts:true) == 162 && ControllerKeys.vk(54,shortcuts:true) == 163)
        precondition(ControllerKeys.vk(59,shortcuts:true) == 91 && ControllerKeys.vk(62,shortcuts:true) == 92)
        precondition(ControllerKeys.vk(55,shortcuts:false) == 91 && ControllerKeys.vk(59,shortcuts:false) == 162)
        for pair: (UInt16,Int) in [(8,67),(9,86),(18,49),(29,48),(82,96),(92,105),(36,13),(76,13),(58,164),(61,165)] { precondition(ControllerKeys.vk(pair.0,shortcuts:true) == pair.1) }
        precondition(ControllerKeys.vk(255,shortcuts:true) == nil)
        var mods = ControllerModifiers()
        let command: UInt = 1 << 20
        let down = mods.synchronize(flags:command,shortcuts:true)
        precondition(down.count == 1 && down[0]["vk"] as? Int == 162 && down[0]["down"] as? Bool == true)
        precondition(mods.synchronize(flags:command,shortcuts:true).isEmpty)
        precondition(mods.changed(code:54,flags:command,shortcuts:true)[0]["down"] as? Bool == true)
        precondition(mods.changed(code:55,flags:command,shortcuts:true)[0]["down"] as? Bool == false)
        precondition(mods.synchronize(flags:0,shortcuts:true)[0]["vk"] as? Int == 163)
        let caps = mods.changed(code:57,flags:1 << 16,shortcuts:true)
        precondition(caps.count == 2 && caps[0]["down"] as? Bool == true && caps[1]["down"] as? Bool == false)
        print("PASS target-aware Command/Control mapping, digits/keypad and held/missed modifier edges")
        let box = ControllerOutbox()
        for i in 0..<10_000 { try box.put(["op":"mouse_move","x":i]) }
        precondition(box.count == 1 && box.take()?["x"] as? Int == 9999)
        try box.put(["op":"mouse_move","x":1]); try box.put(["op":"key_event","vk":65,"down":true]); try box.put(["op":"mouse_move","x":2])
        precondition(box.count == 3 && box.take()?["x"] as? Int == 1 && box.take()?["op"] as? String == "key_event")
        try box.put(["op":"release_all"]); precondition(box.count == 1 && box.take()?["op"] as? String == "release_all")
        for _ in 0..<256 { try box.put(["op":"key_event"]) }; rejects { try box.put(["op":"key_event"]) }
        precondition(box.count == 256); box.stop(); try box.put(["op":"key_event"]); precondition(box.count == 0)
        print("PASS bounded outbox, movement coalescing, edge order, release purge and stop")
        let mixed = ControllerOutbox()
        for value: [String: Any] in [
            ["op":"key_event","vk":162,"down":true],
            ["op":"clipboard_sync","enabled":false,"requestId":7],
            ["op":"mouse_button","button":0,"down":true],
            ["op":"file_sync","enabled":false,"requestId":8],
            ["op":"mouse_move","x":0.2,"y":0.3],
            ["op":"file_offer","requestId":9],
            ["op":"scroll","delta":120],
            ["op":"clipboard_text","text":"fixture"]
        ] { try mixed.put(value) }
        for _ in 0..<20 { try mixed.put(["op":"release_all"]) }
        try mixed.put(["op":"key_event","vk":65,"down":true])
        var retained = [[String: Any]]()
        while let value = mixed.take() { retained.append(value) }
        precondition(retained.compactMap { $0["op"] as? String } ==
            ["release_all","clipboard_sync","file_sync","file_offer","clipboard_text","key_event"],
            "Focus release lost transfer control, kept stale input or changed order")
        precondition(retained[1]["requestId"] as? Int == 7 && retained[1]["enabled"] as? Bool == false &&
            retained[3]["requestId"] as? Int == 9 && retained[4]["text"] as? String == "fixture")
        let controls = ControllerOutbox()
        for id in 0..<256 { try controls.put(["op":"file_sync","enabled":false,"requestId":id]) }
        rejects { try controls.put(["op":"release_all"]) }
        precondition(controls.count == 256)
        for id in 0..<256 { precondition(controls.take()?["requestId"] as? Int == id) }
        print("PASS focus release preserves clipboard/file consent and payload order; repeated release stays bounded; control-only overflow reported")
        let unit = try H264AccessUnit(Data([0,0,0,1,0x67,0x42,0xaa,0,0,1,0x68,0xbb,0,0,0,1,9,0xf0,0,0,1,0x65,0xcc,0xdd,0,0]))
        precondition(unit.sps == Data([0x67,0x42,0xaa]) && unit.pps == Data([0x68,0xbb]) && unit.idr && unit.picture)
        precondition(unit.avcc == Data([0,0,0,3,0x65,0xcc,0xdd]))
        let aud = try H264AccessUnit(Data([0,0,1,9,0xf0])); precondition(!aud.picture)
        for bad in [Data(),Data([1,2,3]),Data([0,0,1]),Data([1,0,0,1,0x65]),Data([0,0,1,0xe5,1])] { rejects { _ = try H264AccessUnit(bad) } }
        rejects { _ = try H264AccessUnit(Data(Array(repeating: [UInt8](arrayLiteral: 0,0,1,0x65,1), count: 1025).flatMap { $0 })) }
        print("PASS Annex-B SPS/PPS/IDR parsing, AVCC conversion and malformed stream rejection")
    }
}
