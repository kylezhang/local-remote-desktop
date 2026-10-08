import Foundation
import Darwin
import ApplicationServices
final class Host {
    private let lock = NSLock()
    private var listener: Int32 = -1, current: Wire?
    private var running = false
    private let key: Data
    private let status: (String) -> Void
    init(key: Data, status: @escaping (String) -> Void) { self.key = key; self.status = status }
    func start() throws {
        let s = Darwin.socket(AF_INET, SOCK_STREAM, 0); guard s >= 0 else { throw POSIXError(.EIO) }
        var one: Int32 = 1; setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &one, socklen_t(MemoryLayout<Int32>.size))
        var address = sockaddr_in(); address.sin_len = UInt8(MemoryLayout<sockaddr_in>.size); address.sin_family = sa_family_t(AF_INET); address.sin_port = UInt16(47980).bigEndian
        let bound = withUnsafePointer(to: &address) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { Darwin.bind(s, $0, socklen_t(MemoryLayout<sockaddr_in>.size)) } }
        guard bound == 0, listen(s, 4) == 0 else { Darwin.close(s); throw POSIXError(POSIXErrorCode(rawValue: errno) ?? .EIO) }
        lock.lock(); listener = s; running = true; lock.unlock()
        DispatchQueue.global(qos: .userInitiated).async { [self] in acceptLoop(s) }
    }
    private func acceptLoop(_ socket: Int32) {
        while true {
            lock.lock(); let active = running && listener == socket; lock.unlock()
            if !active { break }
            var peer = sockaddr_in(), length = socklen_t(MemoryLayout<sockaddr_in>.size)
            let fd = withUnsafeMutablePointer(to: &peer) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { accept(socket, $0, &length) } }
            if fd < 0 { break }
            let ip = UInt32(bigEndian: peer.sin_addr.s_addr)
            let local = ip >> 24 == 10 || ip >> 20 == 0xac1 || ip >> 16 == 0xc0a8 || ip >> 16 == 0xa9fe || ip >> 24 == 127
            lock.lock()
            guard running, current == nil, local else { lock.unlock(); Darwin.close(fd); continue }
            let wire = Wire(fd); current = wire; lock.unlock()
            // One authenticated controller; unauthenticated sockets time out before capture starts.
            DispatchQueue.global(qos: .userInitiated).async { [self] in serve(wire) }
        }
    }
    private func serve(_ wire: Wire) {
        let input = InputInjector()
        var capture: Capture?
        defer {
            wire.close(); input.release()
            if let capture { wait { await capture.stop() } }
            lock.lock(); if current === wire { current = nil }; let active = running; lock.unlock()
            status(active ? "等待连接 · TCP 47980" : "已停止")
        }
        do {
            try wire.authenticate(key: key)
            let hello = try wire.receive()
            guard hello["op"] as? String == "handshake", hello["version"] as? Int == 1 else { throw WireError.invalid }
            let quality = hello["quality"] as? Int ?? 1; guard (0...2).contains(quality) else { throw WireError.invalid }
            let c = Capture(wire: wire, failure: { wire.close() }); capture = c
            var startupError: Error?
            wait { do { try await c.start(quality: quality) } catch { startupError = error } }
            if let startupError { throw startupError }
            var trusted = AXIsProcessTrusted()
            try wire.json(["op":"capabilities","accessibility":trusted])
            status(trusted ? "正在共享当前桌面 · 可控制" : "正在共享当前桌面 · 辅助功能未授权，只能查看")
            var lastHeartbeat = Date()
            while true {
                let message = try wire.receive()
                if message["op"] as? String == "heartbeat" {
                    lastHeartbeat = Date()
                    let currentTrust = AXIsProcessTrusted()
                    try wire.json(["op":"heartbeat","accessibility":currentTrust])
                    if currentTrust != trusted {
                        trusted = currentTrust
                        status(trusted ? "正在共享当前桌面 · 可控制" : "正在共享当前桌面 · 辅助功能未授权，只能查看")
                    }
                }
                if Date().timeIntervalSince(lastHeartbeat) > 15 { throw WireError.closed }
                if message["op"] as? String == "disconnect" { break }
                input.handle(message)
            }
        } catch { status("连接结束：\(error)") }
    }
    private func wait(_ action: @escaping () async -> Void) { let done = DispatchSemaphore(value: 0); Task { await action(); done.signal() }; done.wait() }
    func stop() {
        lock.lock(); running = false; let fd = listener; listener = -1; let wire = current; lock.unlock()
        wire?.close(); if fd >= 0 { shutdown(fd, SHUT_RDWR); Darwin.close(fd) }
    }
    deinit { stop() }
}
