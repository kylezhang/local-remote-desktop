import Foundation
final class Wire {
    var records: [(UInt8,Data)] = [], position = 0
    private let lock = NSLock()
    init() {}
    init(_ fd: Int32, controller: Bool = false, fileChannel: Bool = false) {}
    func connect(address: String, port: UInt16) throws {}
    func authenticateController(secret: String) throws {}
    func authenticate(key: Data, passwordSalt: Data? = nil) throws {}
    func close() {}
    func send(type: UInt8, payload: Data) throws { lock.lock(); records.append((type,payload)); lock.unlock() }
    func json(_ value: [String: Any]) throws { try send(type: 1, payload: JSONSerialization.data(withJSONObject: value)) }
    func receivePacket() throws -> (UInt8,Data) {
        guard position < records.count else { throw FileFailure.incomplete }
        defer { position += 1 }; return records[position]
    }
    func receive() throws -> [String:Any] {
        let (type,data) = try receivePacket()
        guard type == 1, let value = try JSONSerialization.jsonObject(with: data) as? [String:Any] else { throw FileFailure.invalid }
        return value
    }
    var messages: [[String: Any]] {
        lock.lock(); defer { lock.unlock() }
        return records.compactMap { try? JSONSerialization.jsonObject(with:$0.1) as? [String:Any] }
    }
}
func randomBytes(_ count: Int) -> Data { Data(repeating: 3, count: count) }
enum WireError: Error { case closed }
