import Foundation
protocol ObservableObject {}
@propertyWrapper struct Published<Value> { var wrappedValue: Value }
final class CGImage { let id: Int; init(_ id: Int) { self.id = id } }
final class HostModel { var active = false, starting = false, controlling = false }
enum WireError: Error { case closed, invalid, authentication }
enum PairingCredential {
    static func validate(_ value: String) throws { if value.count < 8 || value.count > 64 || !value.utf8.allSatisfy({ $0 > 32 && $0 < 127 }) { throw WireError.invalid } }
}
final class ControllerSession {
    static var instances = [ControllerSession]()
    let status: ([String: Any]) -> Void, frame: (CGImage) -> Void, ended: (Error?) -> Void
    var started = false, stopped = false, messages = [[String: Any]]()
    init(address: String, password: String, quality: Int, status: @escaping ([String: Any]) -> Void,
         frame: @escaping (CGImage) -> Void, ended: @escaping (Error?) -> Void) {
        self.status = status; self.frame = frame; self.ended = ended; Self.instances.append(self)
    }
    func start() { started = true }
    func stop() { stopped = true }
    func send(_ message: [String: Any]) { messages.append(message) }
    func finish(_ error: Error) { stop(); ended(error) }
}
