import Foundation
import Security
enum Pairing {
    private(set) static var readAttempts = 0
    private(set) static var createAttempts = 0
    static let query: [String: Any] = [kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: "com.remotemaclan.host", kSecAttrAccount as String: "pairing-v1"]
    static func load() throws -> Data {
        var q = query; q[kSecReturnData as String] = true; var result: CFTypeRef?
        readAttempts += 1
        let status = SecItemCopyMatching(q as CFDictionary, &result)
        if status == errSecSuccess {
            guard let d = result as? Data, d.count == 32 else {
                throw NSError(domain: NSOSStatusErrorDomain, code: Int(errSecDecode))
            }
            return d
        }
        guard status == errSecItemNotFound else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status)) }
        let d = randomBytes(32); q = query; q[kSecValueData as String] = d
        q[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        createAttempts += 1
        let added = SecItemAdd(q as CFDictionary, nil)
        guard added == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(added)) }; return d
    }
}
