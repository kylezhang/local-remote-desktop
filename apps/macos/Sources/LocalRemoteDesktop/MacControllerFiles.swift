import Foundation
import AppKit
import Darwin

enum MacControllerFiles {
    static func openReceivedDirectory() {
        let root = MacFiles.receivedDirectory
        do { try FileManager.default.createDirectory(at: root, withIntermediateDirectories: true); NSWorkspace.shared.open(root) } catch { }
    }
}

final class MacFileClipboard: ControllerFileClipboard {
    var revision: Int { NSPasteboard.general.changeCount }
    func read() -> (Bool, [String]?) {
        let files = NSPasteboard.general.readObjects(forClasses: [NSURL.self], options: [.urlReadingFileURLsOnly:true]) as? [NSURL]
        return (true, files.map { Array($0.prefix(FileSource.maxFiles + 1)).map { $0.path ?? "" } })
    }
    func write(_ paths: [String]) -> Bool {
        let board = NSPasteboard.general; board.clearContents()
        return board.writeObjects(paths.map { NSURL(fileURLWithPath: $0) })
    }
}

struct MacControllerFileTransport: ControllerFileTransport {
    func start(_ request: ControllerFileRequest, publish: @escaping ([String]) -> Bool,
               progress: @escaping (String) -> Void, completion: @escaping (Result<Bool, Error>) -> Void) -> ControllerFileTask {
        let job = MacControllerFileJob()
        DispatchQueue.global(qos: .utility).async { job.run(request, publish: publish, progress: progress, completion: completion) }
        return job
    }
}
final class MacControllerFileJob: ControllerFileTask {
    private let lock = NSLock()
    private var current: Wire?, canceled = false
    func cancel() { lock.lock(); canceled = true; let wire = current; lock.unlock(); wire?.close() }
    private var stopped: Bool { lock.lock(); defer { lock.unlock() }; return canceled }
    func run(_ request: ControllerFileRequest, publish: @escaping ([String]) -> Bool,
             progress: @escaping (String) -> Void, completion: @escaping (Result<Bool, Error>) -> Void) {
        var wire: Wire?
        let deadline = DispatchSource.makeTimerSource(queue: .global(qos: .utility))
        deadline.schedule(deadline: .now() + .seconds(3600)); deadline.setEventHandler { [weak self] in self?.cancel() }; deadline.resume()
        defer { deadline.cancel(); wire?.close(); lock.lock(); current = nil; lock.unlock() }
        do {
            let source = try request.paths.map { try FileSource(paths: $0) }
            // Replaced copies may still be closing the target's old worker.
            for attempt in 0..<4 {
                guard !stopped else { throw FileFailure.denied }
                let fd = Darwin.socket(Darwin.AF_INET, Darwin.SOCK_STREAM, 0); guard fd >= 0 else { throw WireError.closed }
                let next = Wire(fd, controller: true, fileChannel: true)
                lock.lock(); current = next; let stop = canceled; lock.unlock()
                if stop { next.close(); throw FileFailure.denied }
                do { try next.connect(address: request.address, port: 47981); try next.authenticateController(secret: request.password); wire = next; break }
                catch { next.close(); if attempt == 3 { throw error }; Thread.sleep(forTimeInterval: Double(attempt+1)*0.1) }
            }
            guard let wire else { throw WireError.closed }
            let started = ProcessInfo.processInfo.systemUptime; var last: Double = -.infinity
            func report(_ done: Int64, _ total: Int64) {
                let now = ProcessInfo.processInfo.systemUptime; guard now-last >= 0.25 || done == total else { return }; last = now
                let text = FileTransport.progressText(source == nil ? "接收" : "发送", done: done, total: total, elapsed: now-started)
                DispatchQueue.main.async { progress(text) }
            }
            var ready: Bool
            if let source {
                try wire.json(["op":"file_push","session":request.session,"requestId":request.requestID,"items":FileTransport.metadata(source.items)])
                try FileTransport.expect(wire.receive(), "file_ready")
                try FileTransport.send(source, wire: wire, progress: report)
                let done = try wire.receive(); try FileTransport.expect(done, "file_done")
                ready = done["clipboardReady"] as? Bool == true
            } else {
                try wire.json(["op":"file_pull","session":request.session,"requestId":request.requestID,"offer":request.offer ?? ""])
                let message = try wire.receive(); try FileTransport.expect(message, "file_ready")
                let items = try FileTransport.items(message["items"]); guard items == request.items else { throw FileFailure.changed }
                let paths = try FileTransport.receive(items, root: MacFiles.receivedDirectory, wire: wire, progress: report, canCommit: { !self.stopped })
                ready = false
                for _ in 0..<20 {
                    guard !stopped else { throw FileFailure.denied }
                    ready = DispatchQueue.main.sync { publish(paths) }; if ready { break }; Thread.sleep(forTimeInterval: 0.1)
                }
                try wire.json(["op":"file_done","clipboardReady":ready])
            }
            guard !stopped else { throw FileFailure.denied }
            DispatchQueue.main.async { completion(.success(ready)) }
        } catch { DispatchQueue.main.async { completion(.failure(error)) } }
    }
    deinit { cancel() }
}
