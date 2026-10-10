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
    private let maximumRead: Int, maximumWrite: Int
    init(_ socket: Int32, controller: Bool = false) {
        fd = socket
        maximumRead = controller ? 8*1024*1024 : 65536
        maximumWrite = controller ? 65536 : 8*1024*1024
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
    // Nonblocking connect keeps cancellation bounded without closing/reusing the
    // descriptor underneath a worker. Only private/link-local/loopback IPv4.
    func connect(address: String, port: UInt16 = 47980) throws {
        let ip = try ControllerRules.address(address), s = try socket()
        var peer = sockaddr_in()
        peer.sin_len = UInt8(MemoryLayout<sockaddr_in>.size)
        peer.sin_family = sa_family_t(AF_INET); peer.sin_port = port.bigEndian
        guard inet_pton(AF_INET, ip, &peer.sin_addr) == 1 else { throw ControllerError.address }
        let original = fcntl(s, F_GETFL)
        guard original >= 0, fcntl(s, F_SETFL, original | O_NONBLOCK) == 0 else { throw WireError.closed }
        defer { _ = fcntl(s, F_SETFL, original) }
        let result = withUnsafePointer(to: &peer) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) {
            Darwin.connect(s, $0, socklen_t(MemoryLayout<sockaddr_in>.size))
        } }
        if result == 0 { return }
        guard errno == EINPROGRESS else { throw WireError.closed }
        let deadline = Date().addingTimeInterval(8)
        while Date() < deadline {
            _ = try socket()
            var item = pollfd(fd: s, events: Int16(POLLOUT), revents: 0)
            let ready = poll(&item, 1, 100)
            if ready < 0 { if errno == EINTR { continue }; throw WireError.closed }
            if ready > 0 {
                var error: Int32 = 0, size = socklen_t(MemoryLayout<Int32>.size)
                guard getsockopt(s, SOL_SOCKET, SO_ERROR, &error, &size) == 0, error == 0 else { throw WireError.closed }
                return
            }
        }
        throw WireError.closed
    }
    func authenticateController(secret: String) throws {
        try PairingCredential.validate(secret)
        let hello = try read(36), magic = hello.prefix(4), server = hello.suffix(32)
        let key: Data, transcript: Data, client = randomBytes(32)
        if magic == Data("RMP1".utf8) {
            let salt = try read(16)
            key = try PairingCredential.password(secret, salt: salt).key
            transcript = magic + salt + server + client
        } else if magic == Data("RML1".utf8) {
            guard secret.utf8.count == 64 else { throw WireError.authentication }
            let text = Array(secret.utf8); var bytes = Data()
            for i in stride(from: 0, to: text.count, by: 2) {
                guard let byte = UInt8(String(bytes: text[i...i+1], encoding: .utf8)!, radix: 16) else { throw WireError.authentication }
                bytes.append(byte)
            }
            key = bytes; transcript = server + client
        } else { throw WireError.invalid }
        try write(client + Self.mac(key, Data("client".utf8) + transcript))
        let proof: Data
        do { proof = try read(32) } catch { throw WireError.authentication }
        guard HMAC<SHA256>.isValidAuthenticationCode(proof, authenticating: Data("server".utf8) + transcript,
                                                   using: SymmetricKey(data: key)) else { throw WireError.authentication }
        tx = SymmetricKey(data: Self.mac(key, Data("c2h".utf8) + transcript))
        rx = SymmetricKey(data: Self.mac(key, Data("h2c".utf8) + transcript))
    }
    func authenticate(key: Data, passwordSalt: Data? = nil) throws {
        let server = randomBytes(32), magic = Data((passwordSalt == nil ? "RML1" : "RMP1").utf8)
        if let salt = passwordSalt { guard salt.count == 16 else { throw WireError.invalid }; try write(magic + server + salt) }
        else { try write(magic + server) }
        let response = try read(64), client = response.prefix(32)
        let transcript = passwordSalt.map { magic + $0 + server + client } ?? (server + client)
        let proof = response.suffix(32)
        guard HMAC<SHA256>.isValidAuthenticationCode(proof, authenticating: Data("client".utf8) + transcript, using: SymmetricKey(data: key)) else { throw WireError.authentication }
        try write(Self.mac(key, Data("server".utf8) + transcript))
        tx = SymmetricKey(data: Self.mac(key, Data("h2c".utf8) + transcript)); rx = SymmetricKey(data: Self.mac(key, Data("c2h".utf8) + transcript))
    }
    private func nonce(_ value: UInt64) throws -> AES.GCM.Nonce { var n = value.bigEndian; return try AES.GCM.Nonce(data: Data(repeating: 0, count: 4) + withUnsafeBytes(of: &n) { Data($0) }) }
    func send(type: UInt8, payload: Data) throws {
        writes.lock(); defer { writes.unlock() }
        let plain = Data([type]) + payload
        guard plain.count <= maximumWrite-16, sent < UInt64.max else { throw WireError.invalid }
        var size = UInt32(plain.count+16).bigEndian
        let header = withUnsafeBytes(of: &size) { Data($0) }
        let box = try AES.GCM.seal(plain, using: tx, nonce: nonce(sent), authenticating: header); sent += 1
        try write(header + box.ciphertext + box.tag)
    }
    func json(_ object: [String: Any]) throws { try send(type: 1, payload: JSONSerialization.data(withJSONObject: object)) }
    func receivePacket() throws -> (UInt8, Data) {
        let header = try read(4); let size = header.reduce(UInt32(0)) { ($0 << 8) | UInt32($1) }
        guard size >= 17 && size <= maximumRead, received < UInt64.max else { throw WireError.invalid }
        let body = try read(Int(size))
        let box = try AES.GCM.SealedBox(nonce: nonce(received), ciphertext: body.dropLast(16), tag: body.suffix(16)); received += 1
        let plain = try AES.GCM.open(box, using: rx, authenticating: header)
        guard let type = plain.first else { throw WireError.invalid }
        return (type, Data(plain.dropFirst()))
    }
    func receive() throws -> [String: Any] {
        let (type, payload) = try receivePacket()
        guard type == 1, let object = try JSONSerialization.jsonObject(with: payload) as? [String: Any] else { throw WireError.invalid }
        return object
    }
    deinit { close(); Darwin.close(fd) }
}
func randomBytes(_ count: Int) -> Data { var d = Data(count: count); let status = d.withUnsafeMutableBytes { SecRandomCopyBytes(kSecRandomDefault, count, $0.baseAddress!) }; precondition(status == errSecSuccess); return d }
