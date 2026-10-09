import Foundation
import AppKit
import Dispatch
func require(_ value: Bool, _ message: String) { if !value { fatalError(message) } }
@main struct AdapterCheck {
    static func main() {
        DispatchQueue.global().async {
            do {
                let path = FileManager.default.temporaryDirectory.appendingPathComponent("localremote-file-adapter-" + UUID().uuidString + ".txt")
                try Data("synthetic file".utf8).write(to:path); defer { try? FileManager.default.removeItem(at:path) }
                func copy() { DispatchQueue.main.sync { let board = NSPasteboard.general; board.clearContents(); _ = board.writeObjects([NSURL(fileURLWithPath:path.path)]) } }
                copy()
                let control = Wire(), permissionLock = NSLock(); var permission = true
                let files = try MacFiles(key: Data(repeating:1,count:32), control: control, allowed: { permissionLock.lock(); defer { permissionLock.unlock() }; return permission }, status:{_ in})
                defer { files.stop() }
                try files.configure(["enabled":true,"requestId":1])
                Thread.sleep(forTimeInterval:0.65)
                require(!control.messages.contains { $0["op"] as? String == "file_offer" },"enable sent existing file")
                copy(); Thread.sleep(forTimeInterval:0.65)
                let offers = control.messages.filter { $0["op"] as? String == "file_offer" }
                require(offers.count == 1,"new copied file not offered")
                require(try FileTransport.items(offers[0]["items"])[0].name == path.lastPathComponent,"offer metadata differs")
                Thread.sleep(forTimeInterval:0.65)
                require(control.messages.filter { $0["op"] as? String == "file_offer" }.count == 1,"unchanged clipboard offered twice")
                permissionLock.lock(); permission = false; permissionLock.unlock()
                Thread.sleep(forTimeInterval:0.65)
                require(control.messages.last?["enabled"] as? Bool == false,"permission revocation not acknowledged")
                copy(); Thread.sleep(forTimeInterval:0.65)
                require(control.messages.filter { $0["op"] as? String == "file_offer" }.count == 1,"revoked clipboard offered")
                permissionLock.lock(); permission = true; permissionLock.unlock()
                Thread.sleep(forTimeInterval:0.65)
                require(control.messages.last?["enabled"] as? Bool == true,"permission recovery not acknowledged")
                require(control.messages.filter { $0["op"] as? String == "file_offer" }.count == 1,"recovery sent disabled copy")
                try files.configure(["enabled":false,"requestId":2]); try files.configure(["enabled":true,"requestId":1])
                require(control.messages.last?["enabled"] as? Bool == false,"stale consent enabled files")
                files.stop(); let count = control.messages.count; copy(); Thread.sleep(forTimeInterval:0.65)
                require(control.messages.count == count,"stopped timer offered files")
                print("PASS production Mac file adapter with mocked AppKit/Darwin/Wire: consent, metadata, no initial copy/duplicates, revocation/recovery, stale IDs and stop")
                exit(0)
            } catch { fatalError("file adapter fixture: \(error)") }
        }
        dispatchMain()
    }
}
