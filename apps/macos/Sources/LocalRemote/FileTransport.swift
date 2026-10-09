import Foundation
import CryptoKit
final class SHAFileDigest: FileDigest {
    private var hash = SHA256()
    func update(_ data: Data) { hash.update(data: data) }
    func finish() -> String { hash.finalize().map { String(format: "%02x", $0) }.joined() }
}
enum FileTransport {
    static func expect(_ message: [String: Any], _ op: String) throws { guard message["op"] as? String == op else { throw FileFailure.invalid } }
    static func items(_ value: Any?) throws -> [FileItem] {
        guard let value else { throw FileFailure.invalid }
        return try FileSource.validate(JSONDecoder().decode([FileItem].self, from: JSONSerialization.data(withJSONObject: value)))
    }
    static func metadata(_ items: [FileItem]) throws -> Any { try JSONSerialization.jsonObject(with: JSONEncoder().encode(items)) }
    static func send(_ source: FileSource, wire: Wire, progress: (Int64, Int64) -> Void = { _, _ in }) throws {
        let total = source.items.reduce(Int64(0), { $0 + $1.size }); var done: Int64 = 0
        for (index, item) in source.items.enumerated() {
            guard try FileSource(paths: [source.paths[index]]).items.first == item else { throw FileFailure.changed }
            try wire.json(["op":"file_start","index":index])
            let input = try FileHandle(forReadingFrom: URL(fileURLWithPath: source.paths[index])); defer { try? input.close() }
            let hash = SHAFileDigest(); var count: Int64 = 0
            while let data = try input.read(upToCount: FileSource.chunk), !data.isEmpty {
                count += Int64(data.count); guard count <= item.size else { throw FileFailure.changed }
                hash.update(data); try wire.send(type: 3, payload: data)
                done += Int64(data.count); progress(done,total)
                Thread.sleep(forTimeInterval: 0.008)
            }
            guard count == item.size else { throw FileFailure.changed }
            try wire.json(["op":"file_end","index":index,"sha256":hash.finish()])
        }
        try wire.json(["op":"file_complete"])
    }
    static func receive(_ items: [FileItem], root: URL, wire: Wire, progress: (Int64, Int64) -> Void = { _, _ in }, canCommit: () -> Bool = { true }) throws -> [String] {
        let sink = try FileSink(root: root, items: items, digest: { SHAFileDigest() }); defer { sink.cancel() }
        let total = items.reduce(Int64(0), { $0 + $1.size }); var done: Int64 = 0
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
