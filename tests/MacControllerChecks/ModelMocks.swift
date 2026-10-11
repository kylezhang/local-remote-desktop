import Foundation
protocol ObservableObject {}
@propertyWrapper struct Published<Value> { var wrappedValue: Value }
final class CGImage { let id: Int; init(_ id: Int) { self.id = id } }
final class HostModel { var active = false, starting = false, controlling = false; var stops = 0; func stop() { stops += 1; active = false; starting = false } }
enum WireError: Error { case closed, invalid, authentication }
enum PairingCredential {
    static func validate(_ value: String) throws { if value.count < 8 || value.count > 64 || !value.utf8.allSatisfy({ $0 > 32 && $0 < 127 }) { throw WireError.invalid } }
}
final class ControllerSession {
    static var instances = [ControllerSession]()
    let status: ([String: Any]) -> Void, frame: (CGImage) -> Void, ended: (Error?) -> Void
    var started = false, stopped = false, messages = [[String: Any]]()
    let address: String, password: String
    init(address: String, password: String, quality: Int, status: @escaping ([String: Any]) -> Void,
         frame: @escaping (CGImage) -> Void, ended: @escaping (Error?) -> Void) {
        self.address = address; self.password = password
        self.status = status; self.frame = frame; self.ended = ended; Self.instances.append(self)
    }
    func start() { started = true }
    func stop() { stopped = true }
    func send(_ message: [String: Any]) { messages.append(message) }
    func finish(_ error: Error) { stop(); ended(error) }
}

final class ControllerKeychain: ControllerCredentials {
    var values = [String: String](), loads = [String](), saves = [String](), forgotten = [String]()
    var failLoad = false, failSave = false, failForget = false
    var holdLoad = false, pending: CheckedContinuation<String?, Error>?
    func load(address: String) async throws -> String? {
        loads.append(address)
        if failLoad { throw WireError.invalid }
        if holdLoad { return try await withCheckedThrowingContinuation { pending = $0 } }
        return values[address]
    }
    func save(address: String, password: String) async throws {
        saves.append(address); if failSave { throw WireError.invalid }; values[address] = password
    }
    func forget(address: String) async throws {
        forgotten.append(address); if failForget { throw WireError.invalid }; values.removeValue(forKey: address)
    }
}
final class ControllerDiscovery: ControllerBrowsing {
    var changed: (([NearbyDevice], Bool) -> Void)?
    var starts = 0, stops = 0
    func start() { starts += 1; changed?([], true) }
    func stop() { stops += 1 }
}
