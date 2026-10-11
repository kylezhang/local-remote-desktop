import Foundation

// A bounded main-runloop Bonjour scan. Refresh replaces the previous generation;
// removed services and callbacks from an old browser cannot resurrect a device.
final class ControllerDiscovery: NSObject, ControllerBrowsing, NetServiceBrowserDelegate, NetServiceDelegate {
    var changed: (([NearbyDevice], Bool) -> Void)?
    private var browser: NetServiceBrowser?
    private var services = [ObjectIdentifier: NetService]()
    private var results = [ObjectIdentifier: [NearbyDevice]]()
    private var deadline: DispatchWorkItem?
    private let schedule: (DispatchWorkItem) -> Void
    init(schedule: @escaping (DispatchWorkItem) -> Void = { DispatchQueue.main.asyncAfter(deadline: .now()+5, execute: $0) }) {
        self.schedule = schedule; super.init()
    }
    func start() {
        stop(); results.removeAll()
        let next = NetServiceBrowser(); next.delegate = self; browser = next
        changed?([], true); next.searchForServices(ofType: "_localremote._tcp.", inDomain: "local.")
        let job = DispatchWorkItem { [weak self, weak next] in
            guard let self, let next, self.browser === next else { return }
            self.finish()
        }
        deadline = job; schedule(job)
    }
    private func snapshot(_ searching: Bool) {
        var seen = Set<String>()
        let devices = results.values.flatMap { $0 }.filter { seen.insert($0.address).inserted }
            .sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        changed?(Array(devices.prefix(32)), searching)
    }
    private func finish() {
        deadline?.cancel(); deadline = nil
        browser?.stop(); browser?.delegate = nil; browser = nil
        for service in services.values { service.stop(); service.delegate = nil }
        services.removeAll(); snapshot(false)
    }
    func stop() {
        deadline?.cancel(); deadline = nil
        browser?.stop(); browser?.delegate = nil; browser = nil
        for service in services.values { service.stop(); service.delegate = nil }
        services.removeAll(); results.removeAll()
    }
    func netServiceBrowser(_ sender: NetServiceBrowser, didFind service: NetService, moreComing: Bool) {
        guard browser === sender, services.count < 32 else { return }
        let id = ObjectIdentifier(service)
        guard services[id] == nil else { return }
        services[id] = service; service.delegate = self; service.resolve(withTimeout: 3)
    }
    func netServiceBrowser(_ sender: NetServiceBrowser, didRemove service: NetService, moreComing: Bool) {
        guard browser === sender else { return }
        let id = ObjectIdentifier(service)
        services.removeValue(forKey: id)?.stop(); service.delegate = nil
        results.removeValue(forKey: id); snapshot(true)
    }
    func netServiceBrowser(_ sender: NetServiceBrowser, didNotSearch error: [String: NSNumber]) {
        guard browser === sender else { return }; finish()
    }
    func netServiceDidResolveAddress(_ sender: NetService) {
        let id = ObjectIdentifier(sender)
        guard services[id] === sender, browser != nil else { return }
        results[id] = NearbyDevice.resolved(name: sender.name, port: sender.port,
            txt: sender.txtRecordData().map(NetService.dictionary(fromTXTRecord:)) ?? [:], addresses: sender.addresses ?? [])
        snapshot(true)
    }
    func netService(_ sender: NetService, didNotResolve error: [String: NSNumber]) {
        let id = ObjectIdentifier(sender)
        guard services[id] === sender else { return }; results.removeValue(forKey: id); snapshot(true)
    }
    deinit { stop() }
}
