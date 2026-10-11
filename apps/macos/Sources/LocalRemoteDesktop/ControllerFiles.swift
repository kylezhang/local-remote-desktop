import Foundation

protocol ControllerFileClipboard {
    var revision: Int { get }
    func read() -> (Bool, [String]?)
    func write(_ paths: [String]) -> Bool
}
protocol ControllerFileTask { func cancel() }
struct ControllerFileRequest {
    let address: String, password: String, session: String, requestID: Int
    let paths: [String]?, offer: String?, items: [FileItem]
}
protocol ControllerFileTransport {
    func start(_ request: ControllerFileRequest, publish: @escaping ([String]) -> Bool,
               progress: @escaping (String) -> Void, completion: @escaping (Result<Bool, Error>) -> Void) -> ControllerFileTask
}

// Main-thread role state; transport workers marshal callbacks to this thread.
// The clipboard baseline is renewed on consent, reconnect and received files.
final class ControllerFiles {
    private let board: ControllerFileClipboard, transport: ControllerFileTransport
    private let allowed: () -> Bool, send: ([String: Any]) -> Void, status: (String, Bool) -> Void
    private var address = "", password = "", session = ""
    private var active = false, requested = false, enabled = false, requestID = 0, revision = 0
    private var generation = UUID(), job: ControllerFileTask?, remoteReady = true
    var busy: Bool { job != nil }
    var pasteReady: Bool { !busy && remoteReady }
    init(board: ControllerFileClipboard, transport: ControllerFileTransport, allowed: @escaping () -> Bool,
         send: @escaping ([String: Any]) -> Void, status: @escaping (String, Bool) -> Void) {
        self.board = board; self.transport = transport; self.allowed = allowed; self.send = send; self.status = status
    }
    func start(capabilities: [String: Any], address: String, password: String) {
        stop()
        guard capabilities["fileClipboard"] as? Bool == true, capabilities["filePort"] as? Int == 47981,
              let token = capabilities["fileSession"] as? String, token.count == 64,
              token.utf8.allSatisfy({ (48...57).contains($0) || (97...102).contains($0) }) else { return }
        self.address = address; self.password = password; session = token; active = true
        requested = !allowed(); refresh()
    }
    private func refresh() {
        guard active, requested != allowed() else { return }
        requested = allowed(); enabled = false; cancel(); remoteReady = true
        revision = board.revision; requestID += 1
        send(["op":"file_sync", "enabled":requested, "requestId":requestID])
    }
    func receive(_ message: [String: Any]) {
        refresh()
        guard active, let id = message["requestId"] as? Int, id == requestID else { return }
        if message["op"] as? String == "file_sync", let value = message["enabled"] as? Bool {
            let next = value && requested && allowed()
            if next != enabled { cancel(); remoteReady = true; revision = board.revision }
            enabled = next
            status(next ? "文件同步：已启用，复制文件后自动传输" : "文件同步：关闭", false)
        } else if message["op"] as? String == "file_offer", enabled, allowed(),
                  let offer = message["offer"] as? String, offer.count == 32,
                  offer.utf8.allSatisfy({ (48...57).contains($0) || (97...102).contains($0) }) {
            do {
                guard let value = message["items"] else { throw FileFailure.invalid }
                let items = try FileSource.validate(JSONDecoder().decode([FileItem].self, from: JSONSerialization.data(withJSONObject: value)))
                begin(paths: nil, offer: offer, items: items)
            } catch { status("文件同步失败，请检查文件后重新复制。", busy) }
        }
    }
    func poll() {
        refresh()
        guard active, enabled, allowed(), board.revision != revision else { return }
        let (readable, paths) = board.read(); guard readable else { return }
        revision = board.revision; cancel(); remoteReady = true
        if let paths, !paths.isEmpty { begin(paths: paths, offer: nil, items: []) }
    }
    private func begin(paths: [String]?, offer: String?, items: [FileItem]) {
        cancel(); remoteReady = false
        let current = generation, baseline = board.revision
        let request = ControllerFileRequest(address: address, password: password, session: session, requestID: requestID, paths: paths, offer: offer, items: items)
        status("文件传输：正在校验并准备剪贴板…", true)
        job = transport.start(request, publish: { [weak self] paths in
            guard let self, self.generation == current, self.active, self.enabled, self.allowed(), self.board.revision == baseline else { return false }
            guard self.board.write(paths) else { return false }
            self.revision = self.board.revision; return true
        }, progress: { [weak self] text in
            guard let self, self.generation == current else { return }; self.status(text, true)
        }, completion: { [weak self] result in
            guard let self, self.generation == current else { return }; self.job = nil
            switch result {
            case .success(let ready):
                self.remoteReady = paths == nil || ready
                self.status(paths == nil ? (ready ? "文件已接收，可在 Mac 粘贴" : "文件已接收；剪贴板已变化，可打开接收目录") : (ready ? "文件已发送，可在 Windows 粘贴" : "文件已发送；Windows 未确认剪贴板就绪，请查看其提示或接收目录"), false)
            case .failure: self.remoteReady = paths == nil; self.status("文件传输结束或失败；重新复制可重试", false)
            }
        })
    }
    func cancel() { generation = UUID(); job?.cancel(); job = nil }
    func stop() { cancel(); active = false; enabled = false; address = ""; password = ""; session = ""; remoteReady = true; revision = board.revision }
    deinit { job?.cancel() }
}
