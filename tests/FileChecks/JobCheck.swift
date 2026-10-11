import Foundation
enum MacFiles { static var receivedDirectory: URL { URL(fileURLWithPath: CommandLine.arguments[2]).appendingPathComponent("received") } }
@main struct JobCheck {
    static func main() throws {
        let mode = CommandLine.arguments[1], root = URL(fileURLWithPath:CommandLine.arguments[2])
        let path = root.appendingPathComponent("报告.txt"); try Data("Swift file → Windows 🎉".utf8).write(to:path)
        let job = MacControllerFileJob()
        let request = ControllerFileRequest(address:"127.0.0.1", password:"Fixture1", session:String(repeating:"a",count:64), requestID:1,
            paths:mode == "push" ? [path.path] : nil, offer:mode == "push" ? nil : String(repeating:"b",count:32), items:[FileItem(name:"报告.txt",size:Int64(Data("Python file → Mac 🎉".utf8).count))])
        let timeout = DispatchSource.makeTimerSource(queue:.global()); timeout.schedule(deadline:.now()+12);timeout.setEventHandler { fatalError("File job timeout") };timeout.resume()
        DispatchQueue.main.async {
            if mode == "cancel" { DispatchQueue.main.asyncAfter(deadline:.now()+0.35) {job.cancel()} }
            DispatchQueue.global().async { job.run(request,publish:{ paths in
                dispatchPrecondition(condition:.onQueue(.main)); precondition(paths.count == 1)
                precondition((try? Data(contentsOf:URL(fileURLWithPath:paths[0]))) == Data("Python file → Mac 🎉".utf8))
                return true
            },progress:{ _ in dispatchPrecondition(condition:.onQueue(.main)) },completion:{ result in
                dispatchPrecondition(condition:.onQueue(.main))
                switch result {
                case .success(let ready): precondition(["push","pull"].contains(mode) && ready)
                case .failure: precondition(["checksum","cancel"].contains(mode))
                    let entries = (try? FileManager.default.contentsOfDirectory(atPath:MacFiles.receivedDirectory.path)) ?? []
                    precondition(entries.isEmpty,"Failed/canceled transfer retained staging files")
                }
                timeout.cancel(); print("PASS production Mac file worker " + mode + ": real encrypted socket, recorded publish callback; portable platform adapters");exit(0)
            }) }
        }
        dispatchMain()
    }
}
