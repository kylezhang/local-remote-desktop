import Foundation

// Main-runloop Bonjour publication. Advertise only when the listener is active.
final class HostDiscovery: NSObject, NetServiceDelegate {
    private var service: NetService?
    private(set) var state = "stopped"
    static func serviceName(_ raw: String) -> String {
        var value = ""
        for scalar in raw.unicodeScalars where !CharacterSet.controlCharacters.contains(scalar) {
            let next = value + String(scalar)
            if next.utf8.count > 63 { break }; value = next
        }
        return value.isEmpty ? "Mac" : value
    }
    func start(name: String = DeviceName.current()) {
        stop()
        let raw = DeviceName.valid(name) ?? DeviceName.system
        let next = NetService(domain: "local.", type: "_localremote._tcp.", name: Self.serviceName(raw), port: 47980)
        next.delegate = self
        next.setTXTRecord(NetService.data(fromTXTRecord: ["protocol": Data("1".utf8), "platform": Data("macos".utf8), "deviceName": Data(raw.utf8)]))
        service = next; state = "publishing"; next.publish()
    }
    func netServiceDidPublish(_ sender: NetService) { if service === sender { state = "published" } }
    func netService(_ sender: NetService, didNotPublish errorDict: [String: NSNumber]) {
        if service === sender { state = "unavailable (manual IP still works)" }
    }
    func stop() { service?.stop(); service?.delegate = nil; service = nil; state = "stopped" }
}
