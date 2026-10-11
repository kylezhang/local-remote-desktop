import Foundation
final class FileBoardFixture: ControllerFileClipboard {
    var revision = 0, paths: [String]?, writes = 0, writable = true
    func read() -> (Bool, [String]?) { (true, paths) }
    func write(_ paths: [String]) -> Bool { guard writable else { return false }; self.paths = paths; revision += 1; writes += 1; return true }
    func copy(_ value: [String]?) { paths = value; revision += 1 }
}
final class FileJobFixture: ControllerFileTask { var canceled = false; func cancel() { canceled = true } }
final class FileTransportFixture: ControllerFileTransport {
    var requests = [ControllerFileRequest](), jobs = [FileJobFixture]()
    var publish: (([String]) -> Bool)?, completion: ((Result<Bool, Error>) -> Void)?
    func start(_ request: ControllerFileRequest, publish: @escaping ([String]) -> Bool, progress: @escaping (String) -> Void,
               completion: @escaping (Result<Bool, Error>) -> Void) -> ControllerFileTask {
        self.publish = publish; self.completion = completion; requests.append(request)
        let job = FileJobFixture(); jobs.append(job); return job
    }
}
@main struct ControllerCheck {
    static func main() throws {
        let board = FileBoardFixture(), transport = FileTransportFixture(); var allowed = true, messages = [[String: Any]]()
        let files = ControllerFiles(board: board, transport: transport, allowed: { allowed }, send: { messages.append($0) }, status: { _, _ in })
        let caps: [String: Any] = ["fileClipboard":true,"filePort":47981,"fileSession":String(repeating:"a",count:64)]
        board.copy(["old private file"]); files.start(capabilities:caps,address:"127.0.0.1",password:"Fixture1")
        let id = messages.last!["requestId"] as! Int
        files.receive(["op":"file_sync","requestId":id,"enabled":true]); files.poll()
        precondition(transport.requests.isEmpty,"Enable transmitted preexisting copy")
        board.copy(["new file"]); files.poll(); precondition(files.busy && !files.pasteReady && transport.requests.last!.paths == ["new file"])
        let oldCompletion = transport.completion!; files.cancel(); precondition(transport.jobs.last!.canceled)
        oldCompletion(.success(true)); precondition(!files.pasteReady,"Canceled send became paste-ready")
        board.copy(nil); files.poll(); precondition(files.pasteReady)
        let offer: [String: Any] = ["op":"file_offer","requestId":id,"offer":String(repeating:"b",count:32),"items":[["name":"报告.txt","size":0]]]
        files.receive(offer); precondition(files.busy && transport.requests.last!.items == [FileItem(name:"报告.txt",size:0)])
        precondition(transport.publish!(["received file"])); transport.completion!(.success(true)); precondition(files.pasteReady && board.writes == 1)
        let count = transport.requests.count; files.poll(); precondition(transport.requests.count == count,"Received file echoed")
        files.receive(offer); let stalePublish = transport.publish!, staleFinish = transport.completion!
        board.copy(["newer local copy"]); precondition(!stalePublish(["must not override"]))
        files.poll(); precondition(transport.jobs[count].canceled)
        allowed = false; files.poll(); let disabledID = messages.last!["requestId"] as! Int
        precondition(messages.last!["enabled"] as? Bool == false && !files.busy)
        staleFinish(.success(true)); precondition(board.paths == ["newer local copy"])
        files.receive(["op":"file_sync","requestId":id,"enabled":true]); files.receive(offer)
        precondition(transport.requests.count == count+2,"Stale acknowledgment restored disabled files")
        allowed = true; files.poll(); precondition((messages.last!["requestId"] as! Int) > disabledID)
        let newID = messages.last!["requestId"] as! Int
        files.receive(["op":"file_sync","requestId":newID,"enabled":true]); files.poll()
        precondition(transport.requests.count == count+2,"Consent restore transmitted old data")
        let bad: [String: Any] = ["op":"file_offer","requestId":newID,"offer":String(repeating:"b",count:32),"items":[["name":"../escape","size":0]]]
        files.receive(bad); precondition(!files.busy,"Unsafe file offer scheduled")
        files.stop(); files.receive(offer); files.poll(); precondition(!files.busy && files.pasteReady)
        print("PASS production Mac file controller state: consent/ack, baseline, push/pull scheduling, no echo, paste readiness, copy replacement, cancel, stale callbacks, path rejection and stop; transport/clipboard recordings")
    }
}
