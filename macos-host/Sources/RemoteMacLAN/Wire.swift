import Foundation
import CryptoKit
import Darwin
import Security

enum WireError: Error { case closed, invalid, authentication }
final class Wire {
    private var fd: Int32
    private let state = NSLock(), writes = NSLock()
    private var tx = SymmetricKey(size: .bits256), rx = SymmetricKey(size: .bits256)
    private var sent: UInt64 = 0, received: UInt64 = 0
    private var closed = false
    init(_ socket: Int32) {
        fd = socket
        var one: Int32 = 1
        setsockopt(fd, SOL_SOCKET, SO_NOSIGPIPE, &one, socklen_t(MemoryLayout<Int32>.size))
        setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, &one, socklen_t(MemoryLayout<Int32>.size))
        var timeout = timeval(tv_sec: 15, tv_usec: 0)
        setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &timeout, socklen_t(MemoryLayout<timeval>.size))
        timeout.tv_sec = 3
        setsockopt(fd, SOL_SOCKET, SO_SNDTIMEO, &timeout, socklen_t(MemoryLayout<timeval>.size))
    }
    // Shutdown interrupts blocked I/O. Keep descriptor allocated until deinit, avoiding reuse races.
    func close() { state.lock(); defer { state.unlock() }; if !closed { shutdown(fd, SHUT_RDWR); closed = true } }
    private func socket() throws -> Int32 { state.lock(); defer { state.unlock() }; if closed { throw WireError.closed }; return fd }
    private func read(_ count: Int) throws -> Data {
        let s = try socket(); var data = Data(count: count)
        try data.withUnsafeMutableBytes { raw in var offset = 0; while offset < count { let n = recv(s, raw.baseAddress!.advanced(by: offset), count-offset, 0); if n <= 0 { throw WireError.closed }; offset += n } }
        return data
    }
    private func write(_ data: Data) throws {
        let s = try socket()
        try data.withUnsafeBytes { raw in var offset = 0; while offset < data.count { let n = Darwin.send(s, raw.baseAddress!.advanced(by: offset), data.count-offset, 0); if n <= 0 { throw WireError.closed }; offset += n } }
    }
    static func mac(_ key: Data, _ data: Data) -> Data { Data(HMAC<SHA256>.authenticationCode(for: data, using: SymmetricKey(data: key))) }
    func authenticate(key: Data) throws {
        let server = randomBytes(32); try write(Data("RML1".utf8) + server)
        let response = try read(64), client = response.prefix(32), transcript = server + client
        let proof = response.suffix(32)
        guard HMAC<SHA256>.isValidAuthenticationCode(proof, authenticating: Data("client".utf8) + transcript, using: SymmetricKey(data: key)) else { throw WireError.authentication }
        try write(Self.mac(key, Data("server".utf8) + transcript))
        tx = SymmetricKey(data: Self.mac(key, Data("h2c".utf8) + transcript)); rx = SymmetricKey(data: Self.mac(key, Data("c2h".utf8) + transcript))
    }
    private func nonce(_ value: UInt64) throws -> AES.GCM.Nonce { var n = value.bigEndian; return try AES.GCM.Nonce(data: Data(repeating: 0, count: 4) + withUnsafeBytes(of: &n) { Data($0) }) }
    func send(type: UInt8, payload: Data) throws {
        writes.lock(); defer { writes.unlock() }
        let plain = Data([type]) + payload
        guard plain.count <= 8*1024*1024-16 else { throw WireError.invalid }
        var size = UInt32(plain.count+16).bigEndian
        let header = withUnsafeBytes(of: &size) { Data($0) }
        let box = try AES.GCM.seal(plain, using: tx, nonce: nonce(sent), authenticating: header); sent += 1
        try write(header + box.ciphertext + box.tag)
    }
    func json(_ object: [String: Any]) throws { try send(type: 1, payload: JSONSerialization.data(withJSONObject: object)) }
    func receive() throws -> [String: Any] {
        let header = try read(4); let size = header.reduce(UInt32(0)) { ($0 << 8) | UInt32($1) }
        guard size >= 17 && size <= 65536 else { throw WireError.invalid }
        let body = try read(Int(size))
        let box = try AES.GCM.SealedBox(nonce: nonce(received), ciphertext: body.dropLast(16), tag: body.suffix(16)); received += 1
        let plain = try AES.GCM.open(box, using: rx, authenticating: header)
        guard plain.first == 1, let object = try JSONSerialization.jsonObject(with: plain.dropFirst()) as? [String: Any] else { throw WireError.invalid }
        return object
    }
    deinit { close(); Darwin.close(fd) }
}
func randomBytes(_ count: Int) -> Data { var d = Data(count: count); let status = d.withUnsafeMutableBytes { SecRandomCopyBytes(kSecRandomDefault, count, $0.baseAddress!) }; precondition(status == errSecSuccess); return d }
