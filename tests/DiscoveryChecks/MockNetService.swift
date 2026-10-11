import Foundation
protocol NetServiceDelegate: AnyObject {}
final class NetService: NSObject {
    static var instances: [NetService] = []
    weak var delegate: NetServiceDelegate?
    let domain: String, type: String, name: String, port: Int32
    var published = false, stopped = false
    var txt: Data?
    init(domain: String, type: String, name: String, port: Int32) {
        self.domain = domain; self.type = type; self.name = name; self.port = port
        super.init(); Self.instances.append(self)
    }
    static func data(fromTXTRecord record: [String: Data]) -> Data {
        try! JSONSerialization.data(withJSONObject: record.mapValues { String(data: $0, encoding: .utf8)! }, options: [.sortedKeys])
    }
    @discardableResult func setTXTRecord(_ value: Data) -> Bool { txt = value; return true }
    func publish() { published = true }
    func stop() { stopped = true }
}
@main struct Check {
    static func main() {
        let discovery = HostDiscovery(); discovery.start(name: "Office Mac")
        let first = NetService.instances.last!
        precondition(first.published && first.domain == "local." && first.type == "_localremote._tcp." && first.port == 47980)
        let txt = try! JSONSerialization.jsonObject(with: first.txt!) as! [String: String]
        precondition(txt == ["protocol": "1", "platform": "macos", "deviceName": "Office Mac"])
        discovery.netServiceDidPublish(first); precondition(discovery.state == "published")
        discovery.start(name: "改名后的 Mac"); let second = NetService.instances.last!
        precondition(second.name == "改名后的 Mac")
        precondition(first.stopped && first.delegate == nil && second !== first)
        discovery.netServiceDidPublish(first); precondition(discovery.state == "publishing")
        discovery.netService(second, didNotPublish: [:]); precondition(discovery.state.contains("manual IP"))
        discovery.stop(); precondition(second.stopped && discovery.state == "stopped")
        precondition(HostDiscovery.serviceName("\n\r") == "Mac")
        let unicode = HostDiscovery.serviceName(String(repeating: "设备", count: 100))
        precondition(unicode.utf8.count <= 63 && !unicode.isEmpty)
        print("PASS production Bonjour lifecycle, stale callbacks, TXT has no credentials and bounded UTF-8 names with NetService double")
        print("SKIP native Foundation Bonjour publication and macOS local-network permission")
    }
}
