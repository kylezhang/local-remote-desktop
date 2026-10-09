// No Apple-framework dependency: the actual startup ordering can be exercised
// by the same Swift compiler on a developer test machine.
// Async mutations/callbacks stay on the UI actor. HostModel reads the cached
// value from UI callbacks; this helper does not create concurrent tasks.
final class SharingPreparation<Key> {
    private(set) var cachedKey: Key?

    @MainActor
    func readKey(using reader: @MainActor () async throws -> Key) async throws -> Key {
        try Task.checkCancellation()
        if let cachedKey { return cachedKey }
        let key = try await reader()
        try Task.checkCancellation()
        cachedKey = key
        return key
    }

    @MainActor
    func prepare(checkScreen: @MainActor () async throws -> Void,
                 readKey reader: @MainActor () async throws -> Key) async throws -> Key {
        try Task.checkCancellation()
        try await checkScreen()
        try Task.checkCancellation()
        return try await readKey(using: reader)
    }
}
