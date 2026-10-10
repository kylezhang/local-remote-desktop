import Foundation
struct FileItem: Codable, Equatable { let name: String; let size: Int64 }
enum FileFailure: String, LocalizedError {
    case invalid, changed, incomplete, checksum, busy, denied, diskFull
    var errorDescription: String? {
        switch self {
        case .invalid: return "文件名、类型、数量或大小不支持"
        case .changed: return "复制后来源文件已变化，请重新复制"
        case .incomplete: return "文件接收不完整，请重新复制"
        case .checksum: return "文件完整性校验失败，请重新复制"
        case .busy: return "文件通道忙，请稍后重试"
        case .denied: return "文件共享未允许或本次连接已失效"
        case .diskFull: return "接收设备磁盘空间不足"
        }
    }
}
protocol FileDigest { func update(_ data: Data); func finish() -> String }
final class FileSource {
    static let chunk = 32768, maxFiles = 32
    static let maxFile: Int64 = 4 * 1024 * 1024 * 1024, maxBatch: Int64 = 8 * 1024 * 1024 * 1024
    let paths: [String], items: [FileItem]
    static func safeName(_ name: String) -> Bool {
        guard !name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty, name.utf8.count <= 240, name.utf8.elementsEqual(name.precomposedStringWithCanonicalMapping.utf8),
              !name.hasSuffix("."), !name.hasSuffix(" "), name != ".", name != "..",
              !name.unicodeScalars.contains(where: { $0.value < 32 || "<>:\"/\\|?*".unicodeScalars.contains($0) }) else { return false }
        let stem = String(name.split(separator: ".", omittingEmptySubsequences: false).first ?? "").trimmingCharacters(in: .whitespaces).uppercased()
        if ["CON","PRN","AUX","NUL","CONIN$","CONOUT$","CLOCK$"].contains(stem) { return false }
        if stem.count == 4, stem.hasPrefix("COM") || stem.hasPrefix("LPT"), let last = stem.last, "123456789¹²³".contains(last) { return false }
        return true
    }
    @discardableResult static func validate(_ items: [FileItem]) throws -> [FileItem] {
        guard !items.isEmpty, items.count <= maxFiles else { throw FileFailure.invalid }
        var names = Set<String>(), total: Int64 = 0
        for item in items {
            guard safeName(item.name), names.insert(item.name.lowercased()).inserted, item.size >= 0, item.size <= maxFile else { throw FileFailure.invalid }
            total += item.size
        }
        guard total <= maxBatch else { throw FileFailure.invalid }; return items
    }
    init(paths: [String]) throws {
        guard !paths.isEmpty, paths.count <= Self.maxFiles else { throw FileFailure.invalid }
        self.paths = paths
        self.items = try paths.map { path in
            let info = try FileManager.default.attributesOfItem(atPath: path)
            guard info[.type] as? FileAttributeType == .typeRegular, let size = info[.size] as? NSNumber else { throw FileFailure.invalid }
            return FileItem(name: URL(fileURLWithPath: path).lastPathComponent.precomposedStringWithCanonicalMapping, size: size.int64Value)
        }
        try Self.validate(items)
    }
}
final class FileSink {
    private let items: [FileItem], destination: URL
    private let factory: () -> FileDigest
    private var stage: URL?, output: FileHandle?, digest: FileDigest?
    private var next = 0, count: Int64 = 0
    init(root: URL, items: [FileItem], digest: @escaping () -> FileDigest) throws {
        self.items = try FileSource.validate(items); factory = digest
        let manager = FileManager.default
        try manager.createDirectory(at: root, withIntermediateDirectories: true, attributes: [.posixPermissions: 0o700])
        if let free = try manager.attributesOfFileSystem(forPath: root.path)[.systemFreeSize] as? NSNumber,
           free.int64Value < items.reduce(Int64(16 * 1024 * 1024), { $0 + $1.size }) { throw FileFailure.diskFull }
        let id = UUID().uuidString.replacingOccurrences(of: "-", with: "").lowercased()
        let temp = root.appendingPathComponent(".incoming-" + id, isDirectory: true)
        destination = root.appendingPathComponent(id, isDirectory: true)
        try manager.createDirectory(at: temp, withIntermediateDirectories: false, attributes: [.posixPermissions: 0o700]); stage = temp
    }
    func start(_ index: Int) throws {
        guard let stage, output == nil, index == next, index < items.count else { throw FileFailure.invalid }
        let path = stage.appendingPathComponent(items[index].name)
        guard !FileManager.default.fileExists(atPath: path.path), FileManager.default.createFile(atPath: path.path, contents: nil, attributes: [.posixPermissions: 0o600]) else { throw FileFailure.invalid }
        output = try FileHandle(forWritingTo: path); digest = factory(); count = 0
    }
    func write(_ data: Data) throws {
        guard let output, !data.isEmpty, data.count <= FileSource.chunk, count + Int64(data.count) <= items[next].size else { throw FileFailure.invalid }
        try output.write(contentsOf: data); digest?.update(data); count += Int64(data.count)
    }
    func end(_ index: Int, hash: String) throws {
        guard let output, index == next, count == items[next].size else { throw FileFailure.incomplete }
        guard hash.count == 64, hash == digest?.finish() else { throw FileFailure.checksum }
        try output.close(); self.output = nil; digest = nil; next += 1
    }
    func commit() throws -> [String] {
        guard let stage, output == nil, next == items.count else { throw FileFailure.incomplete }
        try FileManager.default.moveItem(at: stage, to: destination); self.stage = nil
        return items.map { destination.appendingPathComponent($0.name).path }
    }
    func cancel() { try? output?.close(); output = nil; digest = nil; if let stage { try? FileManager.default.removeItem(at: stage); self.stage = nil } }
    deinit { cancel() }
}
