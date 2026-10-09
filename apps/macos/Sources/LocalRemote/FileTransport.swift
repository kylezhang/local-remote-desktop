import Foundation
import CryptoKit
final class SHAFileDigest: FileDigest {
    private var hash = SHA256()
    func update(_ data: Data) { hash.update(data: data) }
    func finish() -> String { hash.finalize().map { String(format: "%02x", $0) }.joined() }
}
enum FilePacing {
    static let rate: Double = 8 * 1048576, burst: Double = 512 * 1024
    static func delay(_ bytes: Int64, elapsed: Double) -> Double { max(0, (Double(bytes) - burst) / rate - max(0, elapsed)) }
}
enum FileTransport {
    static func completionText(_ message: [String: Any]) -> String {
        message["clipboardReady"] as? Bool == true ? "文件已发送，可在 Windows 粘贴" : "文件已发送；Windows 未确认剪贴板就绪，请查看其提示或接收目录"
    }
    static func progressText(_ direction: String, done: Int64, total: Int64, elapsed: Double) -> String {
        if done >= total { return "文件传输：正在校验并准备剪贴板…" }
        let percent = Int(min(99, max(0, Double(done) * 100 / Double(max(1, total)))))
        let speed = elapsed >= 0.2 && done > 0 ? String(format: " · %.1f MiB/s", Double(done) / 1048576 / elapsed) : ""
        return String(format: "%@文件：%d%% · %.1f / %.1f MiB", direction, percent, Double(done) / 1048576, Double(total) / 1048576) + speed
    }
    static func expect(_ message: [String: Any], _ op: String) throws { guard message["op"] as? String == op else { throw FileFailure.invalid } }
    static func items(_ value: Any?) throws -> [FileItem] {
        guard let value else { throw FileFailure.invalid }
        return try FileSource.validate(JSONDecoder().decode([FileItem].self, from: JSONSerialization.data(withJSONObject: value)))
    }
    static func metadata(_ items: [FileItem]) throws -> Any { try JSONSerialization.jsonObject(with: JSONEncoder().encode(items)) }
    static func send(_ source: FileSource, wire: Wire, progress: (Int64, Int64) -> Void = { _, _ in }) throws {
        let total = source.items.reduce(Int64(0), { $0 + $1.size }); var done: Int64 = 0
        let started = ProcessInfo.processInfo.systemUptime
        progress(0,total)
        for (index, item) in source.items.enumerated() {
            guard try FileSource(paths: [source.paths[index]]).items.first == item else { throw FileFailure.changed }
            try wire.json(["op":"file_start","index":index])
            let input = try FileHandle(forReadingFrom: URL(fileURLWithPath: source.paths[index])); defer { try? input.close() }
            let hash = SHAFileDigest(); var count: Int64 = 0
            while let data = try input.read(upToCount: FileSource.chunk), !data.isEmpty {
                count += Int64(data.count); guard count <= item.size else { throw FileFailure.changed }
                hash.update(data)
                let delay = FilePacing.delay(done + Int64(data.count), elapsed: ProcessInfo.processInfo.systemUptime - started)
                if delay >= 0.001 { Thread.sleep(forTimeInterval: delay) }
                try wire.send(type: 3, payload: data)
                done += Int64(data.count); progress(done,total)
            }
            guard count == item.size else { throw FileFailure.changed }
            try wire.json(["op":"file_end","index":index,"sha256":hash.finish()])
        }
        try wire.json(["op":"file_complete"])
    }
    static func receive(_ items: [FileItem], root: URL, wire: Wire, progress: (Int64, Int64) -> Void = { _, _ in }, canCommit: () -> Bool = { true }) throws -> [String] {
        let sink = try FileSink(root: root, items: items, digest: { SHAFileDigest() }); defer { sink.cancel() }
        let total = items.reduce(Int64(0), { $0 + $1.size }); var done: Int64 = 0
        progress(0,total)
        for index in items.indices {
            let start = try wire.receive(); try expect(start,"file_start")
            guard start["index"] as? Int == index else { throw FileFailure.invalid }; try sink.start(index)
            while true {
                let (type, data) = try wire.receivePacket()
                if type == 3 { try sink.write(data); done += Int64(data.count); progress(done,total) }
                else {
                    guard type == 1, let end = try JSONSerialization.jsonObject(with: data) as? [String: Any],
                          end["index"] as? Int == index, let hash = end["sha256"] as? String else { throw FileFailure.invalid }
                    try expect(end,"file_end"); try sink.end(index,hash:hash); break
                }
            }
        }
        try expect(wire.receive(),"file_complete")
        guard canCommit() else { throw FileFailure.denied }
        return try sink.commit()
    }
}
