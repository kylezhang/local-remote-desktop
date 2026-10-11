import Foundation
import Security
import CryptoKit

// Separate accounts in the existing file-based Keychain backend. Never read or
// delete the Mac sharing credential (pairing-v1), change its ACL or reset TCC.
struct ControllerKeychain: ControllerCredentials {
    private func query(_ address: String) -> [String: Any] {
        let hash = SHA256.hash(data: Data(address.utf8)).map { String(format: "%02x", $0) }.joined()
        return [kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: "com.remotemaclan.host", kSecAttrAccount as String: "controller-v1-"+hash,
            kSecUseDataProtectionKeychain as String: false]
    }
    private func perform<T>(_ operation: @escaping () throws -> T) async throws -> T {
        try await withCheckedThrowingContinuation { continuation in
            // Share the existing serialized queue, including its scoped legacy
            // interaction setting. User Connect/Save/Forget actions are the only callers.
            Pairing.queue.async {
                do {
                    var previous = DarwinBoolean(false)
                    var status = SecKeychainGetUserInteractionAllowed(&previous)
                    guard status == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status)) }
                    status = SecKeychainSetUserInteractionAllowed(true)
                    guard status == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status)) }
                    defer { _ = SecKeychainSetUserInteractionAllowed(previous.boolValue) }
                    continuation.resume(returning: try operation())
                } catch { continuation.resume(throwing: error) }
            }
        }
    }
    func load(address: String) async throws -> String? {
        try await perform {
            var request = query(address); request[kSecReturnData as String] = true
            var value: CFTypeRef?; let status = SecItemCopyMatching(request as CFDictionary, &value)
            if status == errSecItemNotFound { return nil }
            guard status == errSecSuccess, let data = value as? Data,
                  let password = String(data: data, encoding: .utf8) else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status == errSecSuccess ? errSecDecode : status)) }
            try PairingCredential.validate(password); return password
        }
    }
    func save(address: String, password: String) async throws {
        try PairingCredential.validate(password)
        try await perform {
            let data = Data(password.utf8), request = query(address)
            var status = SecItemUpdate(request as CFDictionary, [kSecValueData as String: data] as CFDictionary)
            if status == errSecItemNotFound {
                var added = request; added[kSecValueData as String] = data
                added[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
                status = SecItemAdd(added as CFDictionary, nil)
            }
            guard status == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status)) }
        }
    }
    func forget(address: String) async throws {
        try await perform {
            let status = SecItemDelete(query(address) as CFDictionary)
            guard status == errSecSuccess || status == errSecItemNotFound else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(status)) }
        }
    }
}
