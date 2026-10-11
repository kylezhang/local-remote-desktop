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
    private static let queue = DispatchQueue(label: "localremote.keychain", qos: .userInitiated)
    private static let counterLock = NSLock()
    private static var reads = 0
    private static var creates = 0
    static var readAttempts: Int { counterLock.lock(); defer { counterLock.unlock() }; return reads }
    static var createAttempts: Int { counterLock.lock(); defer { counterLock.unlock() }; return creates }
    // Previous versions created this item in the file-based login Keychain.
    // Explicit routing avoids the bridge querying a second storage backend.
    // Keep the original service/account and item ACL; only an explicit save rotates credentials.
    static let query: [String: Any] = [kSecClass as String: kSecClassGenericPassword, kSecAttrService as String: "com.remotemaclan.host", kSecAttrAccount as String: "pairing-v1", kSecUseDataProtectionKeychain as String: false]
    static func load(allowInteraction: Bool) async throws -> PairingCredential {
        // A second guard protects future startup callers too. Ad-hoc and
        // unmarked builds never call a credential API until a deliberate click.
        guard allowInteraction || permitsAutomaticRead else {
            throw keychainError(errSecInteractionNotAllowed)
        }
        // Serial background access keeps authentication off the UI thread and
        // scopes the file-based Keychain interaction setting to one operation.
        return try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<PairingCredential, Error>) in
            queue.async {
                do { continuation.resume(returning: try loadSynchronously(allowInteraction: allowInteraction)) }
                catch { continuation.resume(throwing: error) }
            }
        }
    }
    private static func loadSynchronously(allowInteraction: Bool) throws -> PairingCredential {
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
        // This explicitly file-based backend uses the scoped legacy switch.
        // Modern authentication-UI keys are not a substitute for that control.
        counterLock.lock(); reads += 1; counterLock.unlock()
        status = SecItemCopyMatching(q as CFDictionary, &result)
        if status == errSecSuccess {
            guard let d = result as? Data else {
                throw NSError(domain: NSOSStatusErrorDomain, code: Int(errSecDecode))
            }
            return try PairingCredential.decode(d)
        }
        guard status == errSecItemNotFound else { throw keychainError(status) }
        // Missing items are created only after a deliberate sharing/retry click.
        // No silent rotation on denial, cancellation, corruption or startup.
        guard allowInteraction else { throw keychainError(errSecItemNotFound) }
        let credential = try PairingCredential.password(PairingCredential.generated(using: randomBytes(8)), salt: randomBytes(16))
        q = query; q[kSecValueData as String] = credential.encoded
        q[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        counterLock.lock(); creates += 1; counterLock.unlock()
        let added = SecItemAdd(q as CFDictionary, nil)
        guard added == errSecSuccess else { throw NSError(domain: NSOSStatusErrorDomain, code: Int(added)) }; return credential
    }
    static func savePassword(_ password: String) async throws -> PairingCredential {
        try PairingCredential.validateNew(password)
        return try await withCheckedThrowingContinuation { (continuation: CheckedContinuation<PairingCredential, Error>) in
            queue.async {
                do {
                    let credential = try PairingCredential.password(password, salt: randomBytes(16))
                    var previous = DarwinBoolean(false)
                    let read = SecKeychainGetUserInteractionAllowed(&previous)
                    guard read == errSecSuccess else { throw keychainError(read) }
                    let interaction = SecKeychainSetUserInteractionAllowed(true)
                    guard interaction == errSecSuccess else { throw keychainError(interaction) }
                    defer { _ = SecKeychainSetUserInteractionAllowed(previous.boolValue) }
                    // Update the same item without reading/deleting it or changing its ACL.
                    let status = SecItemUpdate(query as CFDictionary, [kSecValueData as String:credential.encoded] as CFDictionary)
                    if status == errSecItemNotFound {
                        var q = query; q[kSecValueData as String] = credential.encoded
                        q[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
                        let added = SecItemAdd(q as CFDictionary, nil)
                        guard added == errSecSuccess else { throw keychainError(added) }
                    } else if status != errSecSuccess { throw keychainError(status) }
                    continuation.resume(returning: credential)
                } catch { continuation.resume(throwing:error) }
            }
        }
    }
    private static func keychainError(_ status: OSStatus) -> NSError {
        NSError(domain: NSOSStatusErrorDomain, code: Int(status))
    }
}
