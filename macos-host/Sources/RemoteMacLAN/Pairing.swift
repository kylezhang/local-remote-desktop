import Foundation
import Security
import Darwin
enum Pairing {
    static let buildSigningMode: String = {
        guard let url = Bundle.main.url(forResource: "signing-mode", withExtension: "txt"),
              let text = try? String(contentsOf: url, encoding: .utf8) else { return "unknown" }
        let mode = text.trimmingCharacters(in: .whitespacesAndNewlines)
        return mode == "certificate" || mode == "ad-hoc" ? mode : "unknown"
    }()
    static var permitsAutomaticRead: Bool { buildSigningMode == "certificate" }
    private static let queue = DispatchQueue(label: "com.remotemaclan.host.keychain", qos: .userInitiated)
    private static let counterLock = NSLock()
    private static var reads = 0
    private static var creates = 0
    static var readAttempts: Int { counterLock.lock(); defer { counterLock.unlock() }; return reads }
    static var createAttempts: Int { counterLock.lock(); defer { counterLock.unlock() }; return creates }
    static let query: [String: Any] = [kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: "com.remotemaclan.host", kSecAttrAccount as String: "pairing-v1"]
    static func load(allowInteraction: Bool) async throws -> Data {
        // A second guard protects future startup callers too. Ad-hoc and
        // unmarked builds never call a credential API until a deliberate click.
        guard allowInteraction || permitsAutomaticRead else {
            throw keychainError(errSecInteractionNotAllowed)
        }
        // Serial background access keeps authentication off the UI thread and
        // scopes the file-based Keychain interaction setting to one operation.
        return try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<Data, Error>) in
            queue.async {
                do { continuation.resume(returning: try loadSynchronously(allowInteraction: allowInteraction)) }
                catch { continuation.resume(throwing: error) }
            }
        }
    }
    private static func loadSynchronously(allowInteraction: Bool) throws -> Data {
        // Preserve the legacy login-Keychain item and ACL. Migrating storage
        // here could lose the existing pairing key. These compatibility APIs
        // are deprecated but still required for the file-based store's UI.
        var previous = DarwinBoolean(false)
        var status = SecKeychainGetUserInteractionAllowed(&previous)
        guard status == errSecSuccess else { throw keychainError(status) }
        status = SecKeychainSetUserInteractionAllowed(allowInteraction)
        guard status == errSecSuccess else { throw keychainError(status) }
        defer { _ = SecKeychainSetUserInteractionAllowed(previous.boolValue) }
        var q = query; q[kSecReturnData as String] = true; var result: CFTypeRef?
        // Per-query UI policy complements the legacy file-Keychain switch.
        // Neither a denied nor an unavailable silent read falls back to UI.
        if !allowInteraction { q[kSecUseAuthenticationUI as String] = kSecUseAuthenticationUIFail }
        counterLock.lock(); reads += 1; counterLock.unlock()
        status = SecItemCopyMatching(q as CFDictionary, &result)
        if status == errSecSuccess {
            guard let d = result as? Data, d.count == 32 else {
                throw NSError(domain: NSOSStatusErrorDomain, code: Int(errSecDecode))
            }
            return d
        }
        guard status == errSecItemNotFound else { throw keychainError(status) }
        // Missing items are created only after a deliberate sharing/retry click.
        // No silent rotation on denial, cancellation, corruption or startup.
        guard allowInteraction else { throw keychainError(errSecItemNotFound) }
        let d = randomBytes(32); q = query; q[kSecValueData as String] = d
        q[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        counterLock.lock(); creates += 1; counterLock.unlock()
        let added = SecItemAdd(q as CFDictionary, nil)
        guard added == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(added)) }; return d
    }
    private static func keychainError(_ status: OSStatus) -> NSError {
        NSError(domain: NSOSStatusErrorDomain, code: Int(status))
    }
}
