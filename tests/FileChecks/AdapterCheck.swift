import Foundation
import AppKit
import Dispatch
import Darwin
func require(_ value: Bool, _ message: String) { if !value { fatalError(message) } }
@main struct AdapterCheck {
    static func main() {
        DispatchQueue.global().async {
            do {
                let path = FileManager.default.temporaryDirectory.appendingPathComponent("localremote-file-adapter-" + UUID().uuidString + ".txt")
                try Data("synthetic file".utf8).write(to:path); defer { try? FileManager.default.removeItem(at:path) }
                let root = FileManager.default.temporaryDirectory.appendingPathComponent("lrd-file-lifecycle-" + UUID().uuidString)
                defer { try? FileManager.default.removeItem(at:root) }
                func until(_ condition: () -> Bool, _ message: String) {
                    let deadline = Date().addingTimeInterval(3)
                    while !condition() { require(Date() < deadline, message); Thread.sleep(forTimeInterval:0.01) }
                }
                func text() { DispatchQueue.main.sync { let board = NSPasteboard.general; board.clearContents(); _ = board.setString("new local text", forType:.string) } }
                func copy() { DispatchQueue.main.sync { let board = NSPasteboard.general; board.clearContents(); _ = board.writeObjects([NSURL(fileURLWithPath:path.path)]) } }
                copy()
                let control = Wire(), permissionLock = NSLock(), reportLock = NSLock(); var permission = true, reports = [String]()
                let files = try MacFiles(key: Data(repeating:1,count:32), control: control, allowed: { permissionLock.lock(); defer { permissionLock.unlock() }; return permission }, status:{ text in reportLock.lock(); reports.append(text); reportLock.unlock() }, root:root)
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
                // A text copy invalidates the offer immediately, even before the next poll.
                text()
                let stale = try Wire.peer(201, messages:[["op":"file_pull","session":files.session,"requestId":1,"offer":offers[0]["offer"]!]])
                SocketFixture.enqueue(201); until({ stale.closed }, "stale pull did not finish")
                require(!stale.messages.contains { $0["op"] as? String == "file_ready" }, "text copy retained readable old offer")
                Thread.sleep(forTimeInterval:0.65)
                // Drive a real production receive worker into a waiting partial batch.
                let pending = try Wire.peer(202, messages:[["op":"file_push","session":files.session,"requestId":1,"items":[["name":"partial.txt","size":9]]], ["op":"file_start","index":0]], stall:true)
                SocketFixture.enqueue(202)
                until({ ((try? FileManager.default.contentsOfDirectory(atPath:root.path)) ?? []).contains { $0.hasPrefix(".incoming-") } }, "partial worker did not create staging")
                reportLock.lock(); reports.removeAll(); reportLock.unlock()
                text(); until({ pending.closed }, "new copy did not cancel old socket")
                until({ ((try? FileManager.default.contentsOfDirectory(atPath:root.path)) ?? []).isEmpty }, "copy replacement retained staging")
                reportLock.lock(); let obsoleteError = reports.contains { $0.contains("失败") }; reportLock.unlock()
                require(!obsoleteError,"canceled worker replaced new-copy status with an error")
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
                let stoppedPeer = try Wire.peer(203, messages:[["op":"file_push","session":files.session,"requestId":3,"items":[["name":"stopped.txt","size":9]]], ["op":"file_start","index":0]], stall:true)
                try files.configure(["enabled":true,"requestId":3]); SocketFixture.enqueue(203)
                until({ ((try? FileManager.default.contentsOfDirectory(atPath:root.path)) ?? []).contains { $0.hasPrefix(".incoming-") } }, "stop fixture did not start")
                files.stop(); until({ stoppedPeer.closed }, "Stop left peer waiting")
                until({ ((try? FileManager.default.contentsOfDirectory(atPath:root.path)) ?? []).isEmpty }, "Stop retained staging")
                let count = control.messages.count; copy(); Thread.sleep(forTimeInterval:0.65)
                require(control.messages.count == count,"stopped timer offered files")
                print("PASS production Mac file adapter with mocked AppKit/Darwin/Wire: consent, metadata, no initial copy/duplicates, revocation/recovery, stale IDs, text replacement rejects old offer, replaced/stopped partial workers close and staging clears")
                exit(0)
            } catch { fatalError("file adapter fixture: \(error)") }
        }
        dispatchMain()
    }
}
