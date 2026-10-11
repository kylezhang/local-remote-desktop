import Foundation
@main struct ControllerCheck {
    static func main() {
        var revision = 0, text = "offline", consent = true, sent = [[String: Any]]()
        let sync = TextClipboardSync(revision: { revision }, read: { (true,text) }, write: { text=$0; revision+=1; return true })
        let controller = ControllerTextClipboard(sync:sync,allowed:{ consent },send:{ sent.append($0) })
        controller.start(supported:true)
        let id = sent.last!["requestId"] as! Int
        precondition(sent.last!["enabled"] as? Bool == true)
        controller.receive(["op":"clipboard_sync","requestId":id+1,"enabled":true])
        text="before ack"; revision+=1; controller.poll(); precondition(sent.count==1)
        controller.receive(["op":"clipboard_sync","requestId":id,"enabled":true]); controller.poll(); precondition(sent.count==1)
        text="Mac → Windows 中文"; revision+=1; controller.poll()
        precondition(TextClipboardSync.decode(sent.last!["text"] as! String)==text)
        controller.receive(["op":"clipboard_text","requestId":id,"encoding":"utf8-base64url","text":TextClipboardSync.encode("Windows → Mac")!])
        precondition(text=="Windows → Mac"); let count=sent.count
        for _ in 0..<200 { controller.poll() }; precondition(sent.count==count)
        consent=false; controller.poll(); let disabled=sent.last!["requestId"] as! Int
        precondition(disabled>id && sent.last!["enabled"] as? Bool == false)
        controller.receive(["op":"clipboard_sync","requestId":id,"enabled":true])
        controller.receive(["op":"clipboard_text","requestId":id,"encoding":"utf8-base64url","text":TextClipboardSync.encode("blocked")!])
        precondition(text=="Windows → Mac")
        consent=true; controller.poll(); let next=sent.last!["requestId"] as! Int
        controller.receive(["op":"clipboard_sync","requestId":next,"enabled":true])
        controller.stop(); controller.receive(["op":"clipboard_text","requestId":next,"encoding":"utf8-base64url","text":TextClipboardSync.encode("stale")!]); precondition(text=="Windows → Mac")
        let stopped=sent.count; revision+=1; controller.poll(); precondition(sent.count==stopped)
        controller.start(supported:false); revision+=1; controller.poll(); precondition(sent.count==stopped)
        print("PASS production Mac controller clipboard state: ack gate, Unicode, no echo, local revocation, stale ID/stop and unsupported peer")
    }
}
