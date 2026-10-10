import Foundation
@main enum PreferenceCheck {
    static func main() {
        let suite = "LocalRemoteDesktopPreferenceCheck." + UUID().uuidString
        let defaults = UserDefaults(suiteName: suite)!
        defer { defaults.removePersistentDomain(forName: suite) }
        for key in ["textClipboardAllowed", "fileClipboardAllowed"] {
            precondition(SharingPreferences.enabled(key, defaults: defaults))
            defaults.set(false, forKey: key)
            precondition(!SharingPreferences.enabled(key, defaults: defaults))
            defaults.set(true, forKey: key)
            precondition(SharingPreferences.enabled(key, defaults: defaults))
        }
        print("PASS Mac sharing defaults on; explicit off/on persisted in isolated preferences")
    }
}
