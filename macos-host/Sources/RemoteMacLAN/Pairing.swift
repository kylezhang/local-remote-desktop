import Foundation
import Security
enum Pairing {
    static let query: [String: Any] = [kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: "com.remotemaclan.host", kSecAttrAccount as String: "pairing-v1"]
    static func load() throws -> Data {
        var q = query; q[kSecReturnData as String] = true; var result: CFTypeRef?
        let status = SecItemCopyMatching(q as CFDictionary, &result)
        if status == errSecSuccess, let d = result as? Data, d.count == 32 { return d }
        guard status == errSecItemNotFound else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status)) }
        let d = randomBytes(32); q = query; q[kSecValueData as String] = d
        q[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        let added = SecItemAdd(q as CFDictionary, nil)
        guard added == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(added)) }; return d
    }
}
