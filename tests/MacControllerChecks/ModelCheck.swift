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
        model.address = "192.168.1.10"; model.connect()
        let first = ControllerSession.instances.last!
        precondition(model.busy && host.controlling && first.started && !model.connected)
        model.send(["op":"key_event"]); precondition(first.messages.isEmpty)
        first.status(["op":"capabilities","inputEnabled":true,"deviceName":"Office Windows"]); first.status(["op":"video_header","width":1280,"height":720])
        first.status(["op":"connected"]); await until { model.connected }
        precondition(model.targetName == "Office Windows" && model.targetSystem == "Windows")
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
        print("PASS exclusive roles, authenticated input gate, latest-only presentation, disconnect and stale callbacks; only address persisted")
        model.autoReconnect = false; model.connect()
        let second = ControllerSession.instances.last!
        second.status(["op":"connected"]); await until { model.connected }
        second.finish(WireError.closed); await until { !model.busy }
        precondition(!host.controlling)
        model.autoReconnect = true; model.connect()
        let third = ControllerSession.instances.last!; third.finish(WireError.authentication)
        await until { !model.busy }; precondition(!host.controlling && model.status.contains("身份验证失败"))
        model.connect(); let fourth = ControllerSession.instances.last!
        fourth.finish(WireError.closed); await until { !model.busy }; precondition(!host.controlling)
        model.connect(); let fifth = ControllerSession.instances.last!
        fifth.status(["op":"connected"]); await until { model.connected }
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
        model.autoReconnect = true; model.connect()
        let seventh = ControllerSession.instances.last!
        seventh.status(["op":"connected"]); await until { model.connected }
        seventh.finish(WireError.closed); await until { !model.connected }
        model.autoReconnect = false; precondition(!model.busy && !host.controlling)
        let finalCount = ControllerSession.instances.count
        try await Task.sleep(nanoseconds:3_100_000_000)
        precondition(ControllerSession.instances.count == finalCount)
        print("PASS failure/incorrect-password states, reconnect cancellation, quality restart and held-input release flow (session doubles)")
    }
}
