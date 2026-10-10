import Foundation
import AppKit
import Darwin

// One file job per authenticated desktop session. A separate TCP socket keeps
// file bodies out of the video/control writer. Clipboard access stays on main.
final class MacFiles {
    let session = randomBytes(32).map { String(format: "%02x", $0) }.joined()
    static var receivedDirectory: URL {
        FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("LocalRemote/ReceivedFiles", isDirectory: true)
    }
    private let key: Data, control: Wire, allowed: () -> Bool, status: (String) -> Void
    private let passwordSalt: Data?
    private let lock = NSLock(), output = DispatchQueue(label: "localremote.files.poll")
    private var listener: Int32 = -1, current: Wire?, stopped = false
    private var requested = false, enabled = false, requestID = 0
    private var offer: (String, FileSource)?
    private var timer: DispatchSourceTimer?
    private var revision = 0 // Main-thread pasteboard revision, suppresses received-file echo.
    init(key: Data, passwordSalt: Data? = nil, control: Wire, allowed: @escaping () -> Bool, status: @escaping (String) -> Void) throws {
        self.key = key; self.passwordSalt = passwordSalt; self.control = control; self.allowed = allowed; self.status = status
        let fd = Darwin.socket(Darwin.AF_INET, Darwin.SOCK_STREAM, 0)
        guard fd >= 0 else { throw POSIXError(.EIO) }
        var one: Int32 = 1
        Darwin.setsockopt(fd, Darwin.SOL_SOCKET, Darwin.SO_REUSEADDR, &one, Darwin.socklen_t(MemoryLayout<Int32>.size))
        var address = Darwin.sockaddr_in(); address.sin_len = UInt8(MemoryLayout<Darwin.sockaddr_in>.size)
        address.sin_family = Darwin.sa_family_t(Darwin.AF_INET); address.sin_port = UInt16(47981).bigEndian
        let bound = withUnsafePointer(to: &address) { $0.withMemoryRebound(to: Darwin.sockaddr.self, capacity: 1) { Darwin.bind(fd, $0, Darwin.socklen_t(MemoryLayout<Darwin.sockaddr_in>.size)) } }
        guard bound == 0, Darwin.listen(fd, 2) == 0 else { Darwin.close(fd); throw POSIXError(POSIXErrorCode(rawValue: errno) ?? .EIO) }
        listener = fd
        DispatchQueue.global(qos: .utility).async { [self] in acceptLoop(fd) }
        let source = DispatchSource.makeTimerSource(queue: output)
        source.schedule(deadline: .now() + .milliseconds(500), repeating: .milliseconds(500))
        source.setEventHandler { [weak self] in self?.poll() }
        timer = source; source.resume()
    }
    private func valid(_ id: Int) -> Bool {
        let permission = allowed()
        lock.lock(); defer { lock.unlock() }
        return !stopped && enabled && requested && permission && requestID == id
    }
    // Called only on output. Reset the baseline on every consent generation;
    // old clipboard data is never sent merely because a session was enabled.
    private func refresh(force: Bool = false) throws {
        let permission = allowed()
        lock.lock()
        let next = !stopped && requested && permission, changed = next != enabled
        enabled = next; let id = requestID
        let active = changed || force ? current : nil
        if changed || force { offer = nil }
        lock.unlock(); active?.close()
        if changed || force {
            DispatchQueue.main.sync { revision = NSPasteboard.general.changeCount }
            try control.json(["op":"file_sync","requestId":id,"enabled":next])
            status(next ? "文件同步：已启用，复制文件后自动传输" : "文件同步：关闭或等待 Windows 允许")
        }
    }
    func configure(_ message: [String: Any]) throws {
        guard let value = message["enabled"] as? Bool, let id = message["requestId"] as? Int, id > 0 else { return }
        try output.sync {
            lock.lock()
            guard !stopped, id > requestID else { lock.unlock(); return }
            requested = value; requestID = id; lock.unlock()
            try refresh(force: true)
        }
    }
    private func poll() {
        do {
            try refresh()
            lock.lock(); let id = requestID, active = !stopped && enabled; lock.unlock()
            guard active else { return }
            let paths: [String]? = DispatchQueue.main.sync {
                let board = NSPasteboard.general
                guard board.changeCount != revision else { return nil }
                revision = board.changeCount
                guard let objects = board.readObjects(forClasses: [NSURL.self], options: [.urlReadingFileURLsOnly:true]) as? [NSURL], !objects.isEmpty else { return nil }
                return Array(objects.prefix(FileSource.maxFiles + 1)).map { $0.path ?? "" }
            }
            guard let paths else { return }
            let source = try FileSource(paths: paths), name = UUID().uuidString.replacingOccurrences(of: "-", with: "").lowercased()
            guard valid(id) else { return }
            lock.lock(); offer = (name, source); lock.unlock()
            try control.json(["op":"file_offer","requestId":id,"offer":name,"items":FileTransport.metadata(source.items)])
            status("文件已复制，正在等待 Windows 接收…")
        } catch { status("文件同步失败：\(error.localizedDescription)；重新复制可重试") }
    }
    private func acceptLoop(_ fd: Int32) {
        while true {
            lock.lock(); let running = !stopped && listener == fd; lock.unlock()
            guard running else { return }
            var peer = Darwin.sockaddr_in(), length = Darwin.socklen_t(MemoryLayout<Darwin.sockaddr_in>.size)
            let socket = withUnsafeMutablePointer(to: &peer) { $0.withMemoryRebound(to: Darwin.sockaddr.self, capacity: 1) { Darwin.accept(fd, $0, &length) } }
            guard socket >= 0 else { return }
            let ip = UInt32(bigEndian: peer.sin_addr.s_addr)
            let local = ip >> 24 == 10 || ip >> 20 == 0xac1 || ip >> 16 == 0xc0a8 || ip >> 16 == 0xa9fe || ip >> 24 == 127
            lock.lock()
            guard !stopped, enabled, current == nil, local else { lock.unlock(); Darwin.close(socket); continue }
            let wire = Wire(socket); current = wire; lock.unlock()
            DispatchQueue.global(qos: .utility).async { [self] in transfer(wire) }
        }
    }
    private func transfer(_ wire: Wire) {
        let deadline = DispatchSource.makeTimerSource(queue: .global(qos: .utility))
        deadline.schedule(deadline: .now() + .seconds(3600))
        deadline.setEventHandler { [weak wire] in wire?.close() }; deadline.resume()
        defer { deadline.cancel() }
        var lastProgress: Double = -.infinity
        let started = ProcessInfo.processInfo.systemUptime
        var direction = "传输"
        func progress(_ done: Int64, _ total: Int64) {
            let now = ProcessInfo.processInfo.systemUptime; guard now - lastProgress >= 0.25 || done == total else { return }
            lastProgress = now
            status(FileTransport.progressText(direction, done: done, total: total, elapsed: now - started))
        }
        defer { wire.close(); lock.lock(); if current === wire { current = nil }; lock.unlock() }
        var authenticated = false
        do {
            try wire.authenticate(key: key, passwordSalt: passwordSalt)
            authenticated = true
            let message = try wire.receive()
            guard let supplied = message["session"] as? String, supplied.utf8.count == session.utf8.count,
                  zip(supplied.utf8,session.utf8).reduce(UInt8(0), { $0 | ($1.0 ^ $1.1) }) == 0,
                  let id = message["requestId"] as? Int, valid(id) else { throw FileFailure.denied }
            if message["op"] as? String == "file_push" {
                direction = "接收"
                let items = try FileTransport.items(message["items"])
                let baseline = DispatchQueue.main.sync { NSPasteboard.general.changeCount }
                try wire.json(["op":"file_ready"])
                let paths = try FileTransport.receive(items, root: Self.receivedDirectory, wire: wire, progress: progress, canCommit: { self.valid(id) })
                let ready = DispatchQueue.main.sync { () -> Bool in
                    let board = NSPasteboard.general
                    guard valid(id), board.changeCount == baseline else { return false }
                    board.clearContents()
                    let written = board.writeObjects(paths.map { NSURL(fileURLWithPath: $0) })
                    revision = board.changeCount; return written
                }
                try wire.json(["op":"file_done","clipboardReady":ready])
                status(ready ? "文件已接收，可在 Mac 粘贴" : "文件已接收；剪贴板已变化，可打开接收目录")
            } else if message["op"] as? String == "file_pull" {
                direction = "发送"
                lock.lock(); let candidate = offer; lock.unlock()
                guard let candidate, message["offer"] as? String == candidate.0 else { throw FileFailure.changed }
                try wire.json(["op":"file_ready","items":FileTransport.metadata(candidate.1.items)])
                try FileTransport.send(candidate.1, wire: wire, progress: progress)
                let completed = try wire.receive(); try FileTransport.expect(completed, "file_done")
                status(FileTransport.completionText(completed))
            } else { throw FileFailure.invalid }
        } catch {
            if authenticated { try? wire.json(["op":"file_error","code":(error as? FileFailure)?.rawValue ?? "io"]) }
            status("文件传输结束或失败：\(error.localizedDescription)；重新复制可重试")
        }
    }
    func stop() {
        lock.lock(); stopped = true; enabled = false; offer = nil
        let source = timer; timer = nil
        let socket = listener; listener = -1; let active = current; lock.unlock()
        source?.cancel()
        active?.close()
        if socket >= 0 { Darwin.shutdown(socket, Darwin.SHUT_RDWR); Darwin.close(socket) }
    }
    func cancelTransfer() { lock.lock(); let active = current; offer = nil; lock.unlock(); active?.close(); status("文件传输已取消；重新复制可重试") }
    deinit { stop() }
}
