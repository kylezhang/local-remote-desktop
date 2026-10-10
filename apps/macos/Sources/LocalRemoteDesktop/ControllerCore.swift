import Foundation

enum ControllerError: Error { case address, capabilities, video, overloaded }

// Pure session/coordinate/key rules are shared by the native view and portable checks.
enum ControllerRules {
    static func address(_ value: String) throws -> String {
        let value = value.trimmingCharacters(in: .whitespacesAndNewlines)
        let parts = value.split(separator: ".", omittingEmptySubsequences: false)
        guard parts.count == 4, parts.allSatisfy({ !$0.isEmpty && $0.allSatisfy({ $0.isASCII && $0.isNumber }) }),
              parts.allSatisfy({ Int($0).map { (0...255).contains($0) } ?? false }) else { throw ControllerError.address }
        let bytes = parts.map { Int($0)! }
        guard bytes[0] == 10 || bytes[0] == 172 && (16...31).contains(bytes[1]) ||
              bytes[0] == 192 && bytes[1] == 168 || bytes[0] == 169 && bytes[1] == 254 || bytes[0] == 127 else { throw ControllerError.address }
        return bytes.map(String.init).joined(separator: ".")
    }
    static func capabilities(_ object: [String: Any]) throws -> Bool {
        guard object["platform"] as? String == "windows", object["inputModel"] as? String == "win-vk-v1",
              let enabled = object["inputEnabled"] as? Bool else { throw ControllerError.capabilities }
        return enabled
    }
    static func header(_ object: [String: Any]) throws -> (Int, Int) {
        guard object["codec"] as? String == "h264-annexb", let w = object["width"] as? Int,
              let h = object["height"] as? Int, let fps = object["fps"] as? Int,
              (2...4096).contains(w), (2...4096).contains(h), w*h <= 3840*2160, (1...120).contains(fps) else { throw ControllerError.video }
        return (w,h)
    }
    // NSView is flipped: normalized (0,0) is the target screen's upper left.
    static func position(x: Double, y: Double, width: Double, height: Double, videoWidth: Int, videoHeight: Int, clamp: Bool) -> (Double, Double)? {
        guard x.isFinite, y.isFinite, width > 0, height > 0, videoWidth > 0, videoHeight > 0 else { return nil }
        let scale = min(width/Double(videoWidth), height/Double(videoHeight))
        let w = Double(videoWidth)*scale, h = Double(videoHeight)*scale
        let px = (x-(width-w)/2)/w, py = (y-(height-h)/2)/h
        if !clamp && (px < 0 || py < 0 || px > 1 || py > 1) { return nil }
        return (min(1,max(0,px)), min(1,max(0,py)))
    }
}

enum ControllerKeys {
    private static let keys: [UInt16: Int] = [
        0:65,1:83,2:68,3:70,4:72,5:71,6:90,7:88,8:67,9:86,11:66,12:81,13:87,14:69,15:82,16:89,17:84,
        18:49,19:50,20:51,21:52,22:54,23:53,24:187,25:57,26:55,27:189,28:56,29:48,30:221,31:79,32:85,33:219,34:73,35:80,
        36:13,37:76,38:74,39:222,40:75,41:186,42:220,43:188,44:191,45:78,46:77,47:190,48:9,49:32,50:192,51:8,53:27,
        56:160,60:161,57:20,58:164,61:165,122:112,120:113,99:114,118:115,96:116,97:117,98:118,100:119,
        101:120,109:121,103:122,111:123,115:36,116:33,117:46,119:35,121:34,123:37,124:39,125:40,126:38,
        82:96,83:97,84:98,85:99,86:100,87:101,88:102,89:103,91:104,92:105,65:110,67:106,69:107,75:111,78:109,76:13]
    static func vk(_ code: UInt16, shortcuts: Bool) -> Int? {
        switch code {
        case 55: return shortcuts ? 162 : 91
        case 54: return shortcuts ? 163 : 92
        case 59: return shortcuts ? 91 : 162
        case 62: return shortcuts ? 92 : 163
        default: return keys[code]
        }
    }
    static func modifierMask(_ code: UInt16) -> UInt? {
        switch code {
        case 54,55: return 1 << 20
        case 59,62: return 1 << 18
        case 58,61: return 1 << 19
        case 56,60: return 1 << 17
        case 57: return 1 << 16
        default: return nil
        }
    }
}

struct ControllerModifiers {
    private var held = Set<UInt16>()
    mutating func reset() { held.removeAll() }
    private func edge(_ code: UInt16, _ down: Bool, _ shortcuts: Bool) -> [String: Any] {
        ["op":"key_event","vk":ControllerKeys.vk(code, shortcuts: shortcuts)!,"down":down]
    }
    mutating func changed(code: UInt16, flags: UInt, shortcuts: Bool) -> [[String: Any]] {
        guard let mask = ControllerKeys.modifierMask(code) else { return [] }
        if code == 57 { return [edge(code,true,shortcuts),edge(code,false,shortcuts)] }
        let down = flags & mask != 0 && !held.contains(code)
        if down { held.insert(code) } else { held.remove(code) }
        return [edge(code,down,shortcuts)]
    }
    // Handles modifiers already held when the remote picture receives focus,
    // and repairs a missed modifier-up before an ordinary key is sent.
    mutating func synchronize(flags: UInt, shortcuts: Bool) -> [[String: Any]] {
        var result = [[String: Any]]()
        for group: [UInt16] in [[55,54],[59,62],[58,61],[56,60]] {
            let mask = ControllerKeys.modifierMask(group[0])!
            let existing = group.filter { held.contains($0) }
            if flags & mask != 0 && existing.isEmpty { held.insert(group[0]); result.append(edge(group[0],true,shortcuts)) }
            if flags & mask == 0 {
                for code in existing { held.remove(code); result.append(edge(code,false,shortcuts)) }
            }
        }
        return result
    }
}

// One writer worker, bounded edge queue and a coalesced pointer. Edges are never
// silently lost: overload terminates the session so the host releases held input.
final class ControllerOutbox {
    private let lock = NSLock()
    private var queue = [[String: Any]](), stopped = false
    @discardableResult func put(_ value: [String: Any]) throws -> Bool {
        lock.lock(); defer { lock.unlock() }
        if stopped { return false }
        if value["op"] as? String == "release_all" { queue.removeAll() }
        if value["op"] as? String == "mouse_move", queue.last?["op"] as? String == "mouse_move" { queue[queue.count-1] = value; return false }
        guard queue.count < 256 else { throw ControllerError.overloaded }
        let wasEmpty = queue.isEmpty; queue.append(value); return wasEmpty
    }
    func take() -> [String: Any]? { lock.lock(); defer { lock.unlock() }; return queue.isEmpty ? nil : queue.removeFirst() }
    func stop() { lock.lock(); stopped = true; queue.removeAll(); lock.unlock() }
    var count: Int { lock.lock(); defer { lock.unlock() }; return queue.count }
}

struct H264AccessUnit {
    let nals: [Data]
    var sps: Data? { nals.last { $0.first.map { $0 & 31 == 7 } ?? false } }
    var pps: Data? { nals.last { $0.first.map { $0 & 31 == 8 } ?? false } }
    var idr: Bool { nals.contains { $0.first.map { $0 & 31 == 5 } ?? false } }
    var picture: Bool { nals.contains { $0.first.map { (1...5).contains($0 & 31) } ?? false } }
    init(_ annexB: Data) throws {
        guard !annexB.isEmpty, annexB.count <= 8*1024*1024-17 else { throw ControllerError.video }
        let bytes = [UInt8](annexB)
        var starts = [(Int,Int)](), i = 0
        while i+2 < bytes.count {
            if bytes[i] == 0 && bytes[i+1] == 0 {
                if bytes[i+2] == 1 {
                    guard starts.count < 1024 else { throw ControllerError.video }
                    starts.append((i,3)); i += 3; continue
                }
                if i+3 < bytes.count && bytes[i+2] == 0 && bytes[i+3] == 1 {
                    guard starts.count < 1024 else { throw ControllerError.video }
                    starts.append((i,4)); i += 4; continue
                }
            }
            i += 1
        }
        guard !starts.isEmpty, starts[0].0 <= 3, bytes.prefix(starts[0].0).allSatisfy({ $0 == 0 }), starts.count <= 1024 else { throw ControllerError.video }
        var units = [Data]()
        for j in starts.indices {
            let start = starts[j].0+starts[j].1
            var end = j+1 < starts.count ? starts[j+1].0 : bytes.count
            while end > start && bytes[end-1] == 0 { end -= 1 }
            guard end > start, bytes[start] & 0x80 == 0, (1...23).contains(bytes[start] & 31) else { throw ControllerError.video }
            units.append(Data(bytes[start..<end]))
        }
        nals = units
    }
    var avcc: Data {
        var value = Data()
        for nal in nals where ![7,8,9].contains(nal[0] & 31) {
            var length = UInt32(nal.count).bigEndian
            withUnsafeBytes(of: &length) { value.append(contentsOf: $0) }; value.append(nal)
        }
        return value
    }
}
