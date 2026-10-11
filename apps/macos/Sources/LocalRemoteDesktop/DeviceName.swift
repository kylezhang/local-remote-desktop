import Foundation

// Display names may change; pairing credentials remain keyed by their address.
enum DeviceName {
    static let preference = "deviceDisplayName"
    static func valid(_ raw: String?) -> String? {
        guard let raw, raw.utf8.count <= 128 else { return nil }
        let value = raw.trimmingCharacters(in: .whitespacesAndNewlines)
        guard (1...32).contains(value.unicodeScalars.count),
              !value.unicodeScalars.contains(where: { CharacterSet.controlCharacters.contains($0) || $0.value == 0xfffd }) else { return nil }
        return value
    }
    static var system: String {
        let raw = ProcessInfo.processInfo.hostName.components(separatedBy: ".").first ?? "Mac"
        let clean = String(String.UnicodeScalarView(raw.unicodeScalars.filter { !CharacterSet.controlCharacters.contains($0) }.prefix(32)))
        return valid(clean) ?? "Mac"
    }
    static func current(_ preferences: UserDefaults = .standard) -> String {
        valid(preferences.string(forKey: preference)) ?? system
    }
    static func save(_ draft: String, preferences: UserDefaults = .standard) throws -> String {
        guard let value = valid(draft) else {
            throw NSError(domain: "LocalRemoteDesktop.DeviceName", code: 1,
                userInfo: [NSLocalizedDescriptionKey: "请输入 1–32 个字符的名称，不含控制字符。"])
        }
        if value == system { preferences.removeObject(forKey: preference) }
        else { preferences.set(value, forKey: preference) }
        return value
    }
    static func peer(_ message: [String: Any], fallback: String) -> String {
        valid(message["deviceName"] as? String) ?? fallback
    }
}
