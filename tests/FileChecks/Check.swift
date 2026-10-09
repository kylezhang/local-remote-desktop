import Foundation
func require(_ value: Bool, _ message: String) { if !value { fatalError(message) } }
func reject(_ action: () throws -> Void) { do { try action(); fatalError("invalid file accepted") } catch { } }
@main struct Check {
    static func main() throws {
        let manager = FileManager.default, root = manager.temporaryDirectory.appendingPathComponent("localremote-swift-files-" + UUID().uuidString)
        try manager.createDirectory(at: root, withIntermediateDirectories: false)
        defer { try? manager.removeItem(at: root) }
        for name in ["../bad","a/b","a\\b","NUL.txt","COM1","COM¹.txt","CON .txt","CONIN$","LPT9.txt","a.","a ","","..","e\u{0301}.txt"] { require(!FileSource.safeName(name),"unsafe name accepted") }
        reject { try FileSource.validate([FileItem(name:"a",size:0),FileItem(name:"A",size:0)]) }
        reject { try FileSource.validate([FileItem(name:"big",size:FileSource.maxFile+1)]) }
        reject { try FileSource.validate((0..<33).map { FileItem(name:"\($0)",size:0) }) }
        reject { _ = try FileSource(paths: [root.path]) }
        let first = root.appendingPathComponent("报告 📝.txt"), second = root.appendingPathComponent("empty.txt")
        let bytes = Data((0..<100003).map { UInt8($0 % 251) })
        try bytes.write(to: first); try Data().write(to: second)
        let source = try FileSource(paths: [first.path,second.path]), receive = root.appendingPathComponent("received")
        let wire = Wire(); try FileTransport.send(source,wire:wire)
        let paths = try FileTransport.receive(source.items,root:receive,wire:wire)
        require(try Data(contentsOf:URL(fileURLWithPath:paths[0])) == bytes,"Swift transport bytes differ")
        require(try Data(contentsOf:URL(fileURLWithPath:paths[1])).isEmpty,"empty file corrupted")
        let count = try manager.contentsOfDirectory(atPath:receive.path).count
        let wrong = Wire(); wrong.records = wire.records
        let index = wrong.records.firstIndex { $0.0 == 3 }!
        wrong.records[index].1[0] ^= 1
        reject { _ = try FileTransport.receive(source.items,root:receive,wire:wrong) }
        let truncated = Wire(); truncated.records = Array(wire.records.prefix(3))
        reject { _ = try FileTransport.receive(source.items,root:receive,wire:truncated) }
        let revoked = Wire(); revoked.records = wire.records
        reject { _ = try FileTransport.receive(source.items,root:receive,wire:revoked,canCommit:{false}) }
        require(try manager.contentsOfDirectory(atPath:receive.path).count == count,"partial files leaked")
        print("PASS production Swift file models/transport: Unicode/empty/chunks, SHA256 verification, unsafe metadata, corruption/truncation/revocation and cleanup")
    }
}
