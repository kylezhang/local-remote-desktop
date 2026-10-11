import Foundation
final class Wire {
    var records: [(UInt8,Data)] = [], position = 0
    private let lock = NSLock()
    private var fixture: FileWireFixture?
    private static let peerLock = NSLock()
    private static var peers: [Int32:FileWireFixture] = [:]
    static func peer(_ fd: Int32, messages: [[String:Any]], stall: Bool = false) throws -> FileWireFixture {
        let peer = try FileWireFixture(messages: messages, stall: stall)
        peerLock.lock(); peers[fd] = peer; peerLock.unlock(); return peer
    }
    init() {}
    init(_ fd: Int32, controller: Bool = false, fileChannel: Bool = false) { Self.peerLock.lock(); fixture = Self.peers.removeValue(forKey:fd); Self.peerLock.unlock() }
    func connect(address: String, port: UInt16) throws {}
    func authenticateController(secret: String) throws {}
    func authenticate(key: Data, passwordSalt: Data? = nil) throws {}
    func close() { fixture?.close() }
    func send(type: UInt8, payload: Data) throws { if let fixture { try fixture.send(type, payload); return }; lock.lock(); records.append((type,payload)); lock.unlock() }
    func json(_ value: [String: Any]) throws { try send(type: 1, payload: JSONSerialization.data(withJSONObject: value)) }
    func receivePacket() throws -> (UInt8,Data) {
        if let fixture { return try fixture.read() }
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

// Recording peer with a cancellable wait, used only in portable checks.
final class FileWireFixture {
    private let condition = NSCondition()
    private var incoming: [(UInt8,Data)], outgoing: [[String:Any]] = [], stopped = false
    private let stall: Bool
    init(messages: [[String:Any]], stall: Bool) throws {
        incoming = try messages.map { (1, try JSONSerialization.data(withJSONObject:$0)) }; self.stall = stall
    }
    func read() throws -> (UInt8,Data) {
        condition.lock(); defer { condition.unlock() }
        while incoming.isEmpty && stall && !stopped { condition.wait() }
        guard !stopped, !incoming.isEmpty else { throw WireError.closed }
        return incoming.removeFirst()
    }
    func send(_ type: UInt8, _ data: Data) throws {
        condition.lock(); defer { condition.unlock() }; guard !stopped else { throw WireError.closed }
        if type == 1, let message = try JSONSerialization.jsonObject(with:data) as? [String:Any] { outgoing.append(message) }
    }
    func close() { condition.lock(); stopped = true; condition.broadcast(); condition.unlock() }
    var closed: Bool { condition.lock(); defer { condition.unlock() }; return stopped }
    var messages: [[String:Any]] { condition.lock(); defer { condition.unlock() }; return outgoing }
}
