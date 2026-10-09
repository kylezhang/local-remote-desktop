// Executes the production SharingPreparation helper without accessing a real
// credential store, screen, network, clipboard or input device.
enum CheckFailure: Error { case screenDenied, readDenied }
func require(_ value: @autoclosure () -> Bool, _ message: String) {
    if !value() { fatalError(message) }
}
@main
struct Check {
    @MainActor static func main() async throws {
        do {
            let preparation = SharingPreparation<Int>(); var reads = 0
            do {
                _ = try await preparation.prepare(checkScreen: { throw CheckFailure.screenDenied }, readKey: { reads += 1; return 1 })
                fatalError("denied screen unexpectedly started")
            } catch CheckFailure.screenDenied { }
            require(reads == 0 && preparation.cachedKey == nil, "screen denial touched credentials")
            print("PASS screen denial: zero credential reads")
        }
        do {
            let preparation = SharingPreparation<Int>(); var sequence: [String] = []
            let key = try await preparation.prepare(checkScreen: { sequence.append("screen") }, readKey: { sequence.append("key"); return 7 })
            require(key == 7 && sequence == ["screen", "key"], "incorrect permission/credential order")
            _ = try await preparation.prepare(checkScreen: { sequence.append("screen-again") }, readKey: { fatalError("cached credential read twice") })
            require(sequence == ["screen", "key", "screen-again"], "cached key skipped screen recheck")
            print("PASS permission first, cached key reuse, screen recheck")
        }
        do {
            let preparation = SharingPreparation<Int>()
            _ = try await preparation.readKey { 9 }
            let key = try await preparation.prepare(checkScreen: {}, readKey: { fatalError("explicit read was not cached") })
            require(key == 9, "explicit key read changed pairing")
            print("PASS standalone key read reused by sharing")
        }
        do {
            let preparation = SharingPreparation<Int>(); var reads = 0
            do {
                _ = try await preparation.prepare(checkScreen: {}, readKey: { reads += 1; throw CheckFailure.readDenied })
                fatalError("denied credential unexpectedly succeeded")
            } catch CheckFailure.readDenied { }
            require(reads == 1 && preparation.cachedKey == nil, "read failure retried or cached a failed key")
            _ = try await preparation.prepare(checkScreen: {}, readKey: { reads += 1; return 3 })
            require(reads == 2, "deliberate retry did not read exactly once")
            print("PASS denied read: no automatic retry, deliberate retry succeeds")
        }
        do {
            let preparation = SharingPreparation<Int>(); var screens = 0; var reads = 0
            let task = Task { @MainActor in
                try await preparation.prepare(checkScreen: { screens += 1 }, readKey: { reads += 1; return 1 })
            }
            task.cancel()
            do { _ = try await task.value; fatalError("cancelled startup succeeded") }
            catch is CancellationError { }
            require(screens == 0 && reads == 0, "cancel-before-start caused side effects")
            print("PASS cancellation before startup: zero permission/credential calls")
        }
        do {
            let preparation = SharingPreparation<Int>(); var reads = 0
            let task = Task { @MainActor in
                try await preparation.prepare(checkScreen: { withUnsafeCurrentTask { $0?.cancel() } }, readKey: { reads += 1; return 1 })
            }
            do { _ = try await task.value; fatalError("cancelled permission check proceeded") }
            catch is CancellationError { }
            require(reads == 0, "cancel-after-permission accessed credentials")
            print("PASS cancellation after permission: zero credential reads")
        }
        do {
            let preparation = SharingPreparation<Int>()
            let task = Task { @MainActor in
                try await preparation.prepare(checkScreen: {}, readKey: { withUnsafeCurrentTask { $0?.cancel() }; return 1 })
            }
            do { _ = try await task.value; fatalError("cancelled read proceeded to listener") }
            catch is CancellationError { }
            require(preparation.cachedKey == nil, "cancelled operation cached a credential")
            print("PASS cancellation after read: no prepared key/listener continuation")
        }
        print("All startup checks passed. No native Keychain/TCC behavior is simulated as proven.")
    }
}
