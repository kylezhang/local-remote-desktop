import Foundation
@main struct Check {
    static func main() throws {
        let suite="LocalRemoteDesktop.NameChecks.\(UUID().uuidString)"
        let preferences=UserDefaults(suiteName: suite)!
        defer { preferences.removePersistentDomain(forName: suite) }
        precondition(DeviceName.current(preferences)==DeviceName.system)
        for value in ["", "   ",String(repeating:"A",count:33),"Bad\nName","Bad\0Name"] {
            do { _ = try DeviceName.save(value, preferences:preferences); fatalError("Invalid name accepted") }
            catch { precondition(DeviceName.current(preferences)==DeviceName.system) }
        }
        _ = try DeviceName.save("  办公室 Mac  ",preferences:preferences)
        precondition(DeviceName.current(preferences)=="办公室 Mac")
        precondition(DeviceName.current(UserDefaults(suiteName:suite)!)=="办公室 Mac")
        _ = try DeviceName.save(DeviceName.system,preferences:preferences)
        precondition(preferences.string(forKey:DeviceName.preference)==nil)
        precondition(DeviceName.valid(String(repeating:"😀",count:32)) != nil)
        precondition(DeviceName.peer(["deviceName":"Office target"],fallback:"IP")=="Office target")
        precondition(DeviceName.peer(["deviceName":123],fallback:"IP")=="IP")
        print("PASS isolated Swift name persistence/restore, Unicode bounds and metadata fallback")
    }
}
