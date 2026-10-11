import Foundation
protocol NetServiceBrowserDelegate: AnyObject {}
protocol NetServiceDelegate: AnyObject {}
final class NetServiceBrowser: NSObject {
    static var instances = [NetServiceBrowser]()
    weak var delegate: NetServiceBrowserDelegate?
    var type = "", domain = "", stopped = false
    override init() { super.init(); Self.instances.append(self) }
    func searchForServices(ofType: String, inDomain: String) { type = ofType; domain = inDomain }
    func stop() { stopped = true }
}
final class NetService: NSObject {
    weak var delegate: NetServiceDelegate?
    var name: String, port = 47980, addresses: [Data]?, txt: Data?
    var resolving = false, stopped = false
    init(_ name: String) { self.name = name }
    func resolve(withTimeout: TimeInterval) { precondition(withTimeout == 3); resolving = true }
    func stop() { stopped = true }
    func txtRecordData() -> Data? { txt }
    static func dictionary(fromTXTRecord data: Data) -> [String: Data] {
        let json = (try? JSONSerialization.jsonObject(with: data)) as? [String: String] ?? [:]
        return json.mapValues { Data($0.utf8) }
    }
}
@main struct BrowserCheck {
    static func endpoint(_ bytes: [UInt8]) -> Data { Data([16,2,0,0]+bytes+Array(repeating: 0, count: 8)) }
    static func main() throws {
        var jobs = [DispatchWorkItem]()
        let scan = ControllerDiscovery(schedule: { jobs.append($0) })
        var found = [NearbyDevice](), searching = false
        scan.changed = { found = $0; searching = $1 }
        scan.start(); let first = NetServiceBrowser.instances.last!
        precondition(searching && first.type == "_localremote._tcp." && first.domain == "local.")
        func service(_ name: String, _ ip: [UInt8], platform: String = "windows") -> NetService {
            let item = NetService(name); item.addresses = [endpoint(ip)]
            item.txt = try! JSONSerialization.data(withJSONObject: ["protocol":"1", "platform":platform, "deviceName":"办公室 PC"])
            return item
        }
        let peer = service("internal-label", [192,168,1,20])
        scan.netServiceBrowser(first, didFind: peer, moreComing: false); scan.netServiceDidResolveAddress(peer)
        precondition(peer.resolving && found.count == 1 && found[0].name == "办公室 PC" && found[0].systemLabel == "Windows")
        let duplicate = service("duplicate", [192,168,1,20]); scan.netServiceBrowser(first, didFind: duplicate, moreComing: false); scan.netServiceDidResolveAddress(duplicate)
        precondition(found.count == 1)
        let invalid = service("public", [8,8,8,8]); scan.netServiceBrowser(first, didFind: invalid, moreComing: false); scan.netServiceDidResolveAddress(invalid)
        let unsupported = service("Mac", [192,168,1,21], platform: "macos");scan.netServiceBrowser(first, didFind: unsupported, moreComing: false);scan.netServiceDidResolveAddress(unsupported)
        precondition(found.count == 1)
        for data in [Data(),Data([16,2]),Data([255,2]+Array(repeating:0,count:14)),endpoint([127,0,0,1])] {
            peer.addresses = [data];scan.netServiceDidResolveAddress(peer);precondition(found.count == 1)
        }
        scan.netServiceBrowser(first, didRemove: duplicate, moreComing: false);precondition(found.isEmpty && duplicate.stopped && duplicate.delegate == nil)
        scan.start();let second = NetServiceBrowser.instances.last!
        precondition(first.stopped && first.delegate == nil && peer.stopped && peer.delegate == nil)
        jobs.first!.perform(); precondition(searching && found.isEmpty && !second.stopped)
        scan.netServiceDidResolveAddress(peer);scan.netServiceBrowser(first, didFind: peer, moreComing: false);precondition(found.isEmpty)
        var offered = [NetService]()
        for i in 1...40 { let item=service("PC \(i)",[192,168,2,UInt8(i)]);offered.append(item);scan.netServiceBrowser(second,didFind:item,moreComing:false);scan.netServiceDidResolveAddress(item) }
        precondition(found.count == 32 && offered.filter { $0.resolving }.count == 32)
        scan.netServiceBrowser(second,didNotSearch:[:])
        let stoppedCount = offered.prefix(32).filter { $0.stopped }.count
        let detachedCount = offered.prefix(32).filter { $0.delegate == nil }.count
        precondition(!searching && second.stopped && stoppedCount == 32 && detachedCount == 32)
        scan.stop();scan.start();let timed=NetServiceBrowser.instances.last!
        jobs.last!.perform()
        precondition(!searching && timed.stopped && timed.delegate == nil)
        let suite = "lrd-discovery-memory-"+UUID().uuidString
        let prefs=UserDefaults(suiteName:suite)!
        defer { prefs.removePersistentDomain(forName:suite) }
        let records = [NearbyDevice(name:"Recent PC",address:"192.168.1.20",remembered:true),NearbyDevice(name:"Untrusted",address:"8.8.8.8")]
        prefs.set(try JSONEncoder().encode(records),forKey:"controllerPeers")
        let remembered = NearbyDevice.load(prefs)
        precondition(remembered.count == 1 && remembered[0].remembered && !remembered[0].online)
        prefs.removeObject(forKey:"controllerPeers")
        print("PASS production controller discovery with browser/service doubles: Windows capability/port/private IPv4, malformed sockaddr, dedup/removal, stale generations, 32-service cap, scheduled scan stop and untrusted recent history")
        print("SKIP native Bonjour browsing and actual multicast/local-network permissions")
    }
}
