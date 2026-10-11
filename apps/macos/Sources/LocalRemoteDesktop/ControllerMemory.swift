import Foundation

protocol ControllerCredentials {
    func load(address: String) async throws -> String?
    func save(address: String, password: String) async throws
    func forget(address: String) async throws
}
struct NearbyDevice: Identifiable, Codable, Equatable {
    var id: String { address }
    let name: String, address: String
    var platform = "windows"
    var online = true
    var remembered = false
    var systemLabel: String { platform == "windows" ? "Windows" : "macOS" }
    static func resolved(name: String, port: Int, txt: [String: Data], addresses: [Data]) -> [NearbyDevice] {
        guard port == 47980, txt["protocol"] == Data("1".utf8), txt["platform"] == Data("windows".utf8) else { return [] }
        let display = DeviceName.valid(txt["deviceName"].flatMap { $0.count <= 128 ? String(data: $0, encoding: .utf8) : nil }) ?? DeviceName.valid(name)
        return addresses.prefix(16).compactMap { data in
            // Darwin sockaddr_in: length, AF_INET, port, four network-order bytes.
            guard data.count >= 16, data.count <= 256 else { return nil }
            let bytes = Array(data)
            guard bytes[0] >= 16, Int(bytes[0]) <= bytes.count, bytes[1] == 2 else { return nil }
            let address = bytes[4...7].map(String.init).joined(separator: ".")
            guard (try? ControllerRules.address(address)) == address, !address.hasPrefix("127.") else { return nil }
            return NearbyDevice(name: display ?? address, address: address)
        }
    }
    static func load(_ preferences: UserDefaults) -> [NearbyDevice] {
        guard let data = preferences.data(forKey: "controllerPeers"), data.count <= 32768,
              let values = try? JSONDecoder().decode([NearbyDevice].self, from: data) else { return [] }
        var seen = Set<String>()
        return values.prefix(32).compactMap { item in
            guard item.platform == "windows", (try? ControllerRules.address(item.address)) == item.address,
                  let name = DeviceName.valid(item.name), seen.insert(item.address).inserted else { return nil }
            return NearbyDevice(name: name, address: item.address, online: false, remembered: item.remembered)
        }
    }
}

protocol ControllerBrowsing: AnyObject {
    var changed: (([NearbyDevice], Bool) -> Void)? { get set }
    func start()
    func stop()
}
