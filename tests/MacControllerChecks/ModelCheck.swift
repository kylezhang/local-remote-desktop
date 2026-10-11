import Foundation
@main struct ModelCheck {
    @MainActor static func until(_ action: () -> Bool) async {
        for _ in 0..<10000 { if action() { return }; await Task.yield() }
        fatalError("Controller state did not settle")
    }
    @MainActor static func main() async throws {
        let suite = "localremote-controller-check-"+UUID().uuidString
        let preferences = UserDefaults(suiteName:suite)!
        defer { preferences.removePersistentDomain(forName:suite) }
        let host = HostModel(), model = ControllerModel(host:host,preferences:preferences)
        model.address = "192.168.1.10"; model.password = "Office-LAN#42"
        host.active = true; model.connect(); precondition(!model.busy && ControllerSession.instances.isEmpty)
        host.active = false; host.starting = true; model.connect(); precondition(!model.busy)
        host.starting = false; model.address = "8.8.8.8"; model.connect(); precondition(!model.busy)
        model.address = "192.168.1.10"; model.password = "Office-LAN#42"; model.connect()
        let first = ControllerSession.instances.last!
        precondition(model.busy && host.controlling && first.started && !model.connected)
        model.send(["op":"key_event"]); precondition(first.messages.isEmpty)
        first.status(["op":"capabilities","inputEnabled":true,"deviceName":"Office Windows"]); first.status(["op":"video_header","width":1280,"height":720])
        first.status(["op":"connected"]); await until { model.connected && !model.credentialBusy }
        precondition(model.targetName == "Office Windows" && model.targetSystem == "Windows")
        let currentQuality = model.quality
        model.defaultQuality = 2; model.defaultShortcuts = false; model.defaultAutoReconnect = false
        precondition(model.quality == currentQuality && model.shortcuts && model.autoReconnect && !first.stopped)
        print("PASS editing future defaults preserves active quality/shortcuts/reconnect and transport")
        first.status(["op":"heartbeat","deviceName":"Renamed Windows"]); await until { model.targetName == "Renamed Windows" }
        model.send(["op":"key_event","vk":67,"down":true]); precondition(first.messages.count == 1)
        model.release(); precondition(first.messages.last?["op"] as? String == "release_all")
        for i in 0..<10_000 { first.frame(CGImage(i)) }
        try await Task.sleep(nanoseconds:60_000_000)
        precondition(model.image?.id == 9999)
        model.disconnect(); precondition(first.stopped && !model.busy && !host.controlling && model.image == nil)
        first.frame(CGImage(1)); first.status(["op":"connected"])
        try await Task.sleep(nanoseconds:30_000_000); precondition(!model.connected && model.image == nil)
        let fresh = ControllerModel(host:host,preferences:preferences)
        precondition(fresh.address == model.address && fresh.password.isEmpty)
        precondition(fresh.defaultQuality == 2 && !fresh.defaultShortcuts && !fresh.defaultAutoReconnect)
        print("PASS exclusive roles, authenticated input gate, latest-only presentation, disconnect and stale callbacks; only nonsecret peer metadata persisted; no automatic Keychain access")
        model.defaultAutoReconnect = false; model.connect()
        let second = ControllerSession.instances.last!
        second.status(["op":"connected"]); await until { model.connected && !model.credentialBusy }
        second.finish(WireError.closed); await until { !model.busy }
        precondition(!host.controlling)
        model.defaultAutoReconnect = true; model.connect()
        let third = ControllerSession.instances.last!; third.finish(WireError.authentication)
        await until { !model.busy }; precondition(!host.controlling && model.status.contains("身份验证失败"))
        model.connect(); let fourth = ControllerSession.instances.last!
        fourth.finish(WireError.closed); await until { !model.busy }; precondition(!host.controlling)
        model.connect(); let fifth = ControllerSession.instances.last!
        fifth.status(["op":"connected"]); await until { model.connected && !model.credentialBusy }
        fifth.finish(WireError.closed); await until { !model.connected }
        precondition(model.busy && host.controlling)
        model.disconnect(); let count = ControllerSession.instances.count
        try await Task.sleep(nanoseconds:3_100_000_000)
        precondition(ControllerSession.instances.count == count && !model.busy && !host.controlling)
        model.connect(); let sixth = ControllerSession.instances.last!; model.restart()
        precondition(sixth.stopped && model.busy && ControllerSession.instances.count == count+1)
        model.autoReconnect = false; model.quality = 2; model.restart()
        precondition(model.busy && host.controlling)
        try await Task.sleep(nanoseconds:3_100_000_000)
        precondition(ControllerSession.instances.count == count+2 && model.busy)
        model.disconnect()
        model.defaultAutoReconnect = true; model.connect()
        let seventh = ControllerSession.instances.last!
        seventh.status(["op":"connected"]); await until { model.connected && !model.credentialBusy }
        seventh.finish(WireError.closed); await until { !model.connected }
        model.autoReconnect = false; precondition(!model.busy && !host.controlling)
        let finalCount = ControllerSession.instances.count
        try await Task.sleep(nanoseconds:3_100_000_000)
        precondition(ControllerSession.instances.count == finalCount)
        print("PASS failure/incorrect-password states, reconnect cancellation, quality restart and held-input release flow (session doubles)")
        try await checkPairing()
    }
    @MainActor static func checkPairing() async throws {
        let suite = "lrd-peer-memory-check-"+UUID().uuidString
        let prefs = UserDefaults(suiteName: suite)!
        defer { prefs.removePersistentDomain(forName: suite) }
        let store = ControllerKeychain(), browser = ControllerDiscovery(), host = HostModel()
        let model = ControllerModel(host: host, preferences: prefs, credentials: store, browser: browser)
        precondition(store.loads.isEmpty && store.saves.isEmpty)
        model.refreshDevices()
        let peer = NearbyDevice(name: "Fixture PC", address: "192.168.1.20")
        browser.changed?([peer], false); model.select(peer)
        precondition(model.selectedDevice == peer && model.password.isEmpty && store.loads.isEmpty)
        model.openManual(); model.manualAddress = "8.8.8.8"; model.manualPassword = "Fixture1"
        host.active = true; model.stopSharingToConnect = true; model.connectManual()
        precondition(host.active && host.stops == 0 && !model.busy && !model.pairingNotice.isEmpty)
        model.cancelManual(); precondition(!model.manualOpen && host.active)
        model.openManual(); model.manualAddress = peer.address; model.manualPassword = "WrongPW1"; model.stopSharingToConnect = true; model.connectManual()
        let rejected = ControllerSession.instances.last!
        precondition(host.stops == 1 && model.busy && rejected.address == peer.address)
        rejected.finish(WireError.authentication); await until { !model.busy }
        precondition(model.manualOpen && model.manualPassword == "WrongPW1" && store.saves.isEmpty && prefs.data(forKey: "controllerPeers") == nil)
        model.manualPassword = "Fixture1"; model.connectManual()
        let accepted = ControllerSession.instances.last!
        accepted.status(["op":"capabilities", "deviceName":"Authenticated PC", "inputEnabled":true])
        accepted.status(["op":"connected"]); await until { model.connected && !model.credentialBusy }
        precondition(!model.manualOpen && store.saves == [peer.address] && model.hasRememberedPassword)
        let data = prefs.data(forKey: "controllerPeers")!
        precondition(!String(data: data, encoding: .utf8)!.contains("Fixture1"))
        precondition(NearbyDevice.load(prefs).first?.name == "Authenticated PC")
        model.disconnect(); model.connect(); let repeated = ControllerSession.instances.last!
        repeated.status(["op":"connected"]); await until { model.connected && !model.credentialBusy }
        precondition(store.loads.isEmpty && store.saves.count == 1)
        model.disconnect()
        let freshBrowser = ControllerDiscovery()
        let fresh = ControllerModel(host: host, preferences: prefs, credentials: store, browser: freshBrowser)
        fresh.refreshDevices(); fresh.select(fresh.devices.first!)
        precondition(store.loads.isEmpty && fresh.password.isEmpty && fresh.hasRememberedPassword && fresh.selectedDevice?.online == false)
        fresh.openManual(); fresh.connectManual()
        await until { fresh.busy && ControllerSession.instances.last !== repeated }
        precondition(store.loads == [peer.address] && ControllerSession.instances.last!.password == "Fixture1")
        ControllerSession.instances.last!.status(["op":"connected"]); await until { fresh.connected && !fresh.credentialBusy }
        precondition(store.saves.count == 1)
        fresh.disconnect(); fresh.forgetPassword(); await until { !fresh.credentialBusy }
        precondition(store.forgotten == [peer.address] && fresh.address == peer.address && !fresh.hasRememberedPassword && fresh.password.isEmpty)
        fresh.openManual(); fresh.manualPassword = "Fixture2"; fresh.manualRemember = false; fresh.connectManual()
        ControllerSession.instances.last!.status(["op":"connected"]); await until { fresh.connected && !fresh.credentialBusy }
        precondition(store.saves.count == 1 && !fresh.hasRememberedPassword)
        fresh.disconnect()
        store.failSave = true; fresh.rememberPassword = true; fresh.connect()
        ControllerSession.instances.last!.status(["op":"connected"]); await until { fresh.connected && !fresh.credentialBusy }
        precondition(fresh.pairingNotice.contains("无法记住") && !fresh.hasRememberedPassword && store.values[peer.address] == nil)
        fresh.disconnect()
        store.failSave = false; fresh.connect(); ControllerSession.instances.last!.status(["op":"connected"])
        await until { fresh.connected && !fresh.credentialBusy }; fresh.disconnect()
        let loading = ControllerModel(host: host, preferences: prefs, credentials: store, browser: ControllerDiscovery())
        store.failLoad = true; loading.connect(); await until { !loading.credentialBusy }
        precondition(!loading.busy && !host.controlling && loading.pairingNotice.contains("无法读取"))
        store.failLoad = false; store.holdLoad = true
        let count = ControllerSession.instances.count
        loading.openManual(); loading.connectManual(); await until { store.pending != nil }
        loading.cancelManual(); store.pending!.resume(returning: "Fixture2"); store.pending = nil
        try await Task.sleep(nanoseconds:30_000_000)
        precondition(!loading.busy && !loading.credentialBusy && !host.controlling && ControllerSession.instances.count == count)
        print("PASS real controller model: no automatic Keychain access; deliberate saved-password load/cancel; successful-only save, opt-out, failed-save recovery, exact-target forget, retained failed draft and bounded nonsecret history")
    }

}
