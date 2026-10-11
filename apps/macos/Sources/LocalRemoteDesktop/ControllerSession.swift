import Foundation
import Darwin
import CoreGraphics

final class ControllerSession {
    private let lock = NSLock(), wake = DispatchSemaphore(value: 0)
    private let outbox = ControllerOutbox()
    private var wire: Wire?, canceled = false, started = false, didFinish = false
    private let address: String, password: String, quality: Int
    private let port: UInt16
    private let status: ([String: Any]) -> Void, frame: (CGImage) -> Void, ended: (Error?) -> Void
    init(address: String, password: String, quality: Int, port: UInt16 = 47980, status: @escaping ([String: Any]) -> Void,
         frame: @escaping (CGImage) -> Void, ended: @escaping (Error?) -> Void) {
        self.address = address; self.password = password; self.quality = quality
        self.port = port
        self.status = status; self.frame = frame; self.ended = ended
    }
    func start() {
        lock.lock(); let launch = !started && !canceled; started = true; lock.unlock()
        if launch { DispatchQueue.global(qos: .userInitiated).async { [self] in run() } }
    }
    func send(_ message: [String: Any]) {
        do { if try outbox.put(message) { wake.signal() } } catch { stop(); finish(error) }
    }
    func stop() { lock.lock(); canceled = true; let current = wire; lock.unlock(); outbox.stop(); current?.close(); wake.signal() }
    private var stopped: Bool { lock.lock(); defer { lock.unlock() }; return canceled }
    private func finish(_ error: Error?) {
        lock.lock(); let notify = !didFinish; didFinish = true; lock.unlock()
        if notify { ended(error) }
    }
    private func run() {
        var error: Error?, heartbeat: DispatchSourceTimer?
        let writerFinished = DispatchSemaphore(value: 0)
        var writerStarted = false
        defer {
            heartbeat?.cancel(); stop()
            if writerStarted { writerFinished.wait() }
            finish(error)
        }
        do {
            let fd = Darwin.socket(AF_INET, SOCK_STREAM, 0)
            guard fd >= 0 else { throw WireError.closed }
            let transport = Wire(fd, controller: true)
            lock.lock(); wire = transport; let alreadyStopped = canceled; lock.unlock()
            if alreadyStopped { transport.close(); return }
            // Bound the entire connect/auth/handshake, including a slow peer.
            let deadline = DispatchWorkItem { transport.close() }
            DispatchQueue.global().asyncAfter(deadline: .now()+12, execute: deadline)
            defer { deadline.cancel() }
            try transport.connect(address: address, port: port)
            try transport.authenticateController(secret: password)
            try transport.json(["op":"handshake","version":1,"quality":quality])
            let caps = try transport.receive()
            guard caps["op"] as? String == "capabilities" else { throw ControllerError.capabilities }
            _ = try ControllerRules.capabilities(caps)
            let header = try transport.receive()
            guard header["op"] as? String == "video_header" else { throw ControllerError.video }
            let (width,height) = try ControllerRules.header(header)
            deadline.cancel(); status(caps); status(header)
            let decoder = ViewerDecoder(width: width, height: height, image: frame)
            writerStarted = true
            DispatchQueue.global(qos: .userInitiated).async { [self] in
                defer { writerFinished.signal() }
                while !stopped {
                    while let message = outbox.take() {
                        do { try transport.json(message) } catch { transport.close(); return }
                    }
                    _ = wake.wait(timeout: .now()+1)
                }
            }
            let timer = DispatchSource.makeTimerSource(queue: DispatchQueue.global())
            timer.schedule(deadline: .now(), repeating: 3)
            timer.setEventHandler { [weak self] in self?.send(["op":"heartbeat"]) }
            heartbeat = timer; timer.resume()
            status(["op":"connected"])
            while !stopped {
                let packet = try transport.receivePacket()
                if packet.0 == 2 { try decoder.decode(packet.1) }
                else if packet.0 == 1, let message = try JSONSerialization.jsonObject(with: packet.1) as? [String: Any],
                        ["heartbeat", "clipboard_sync", "clipboard_text"].contains(message["op"] as? String ?? "") { status(message) }
                else { throw WireError.invalid }
            }
        } catch let failure { if !stopped { error = failure } }
    }
    deinit { stop() }
}
