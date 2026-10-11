import Foundation
func require(_ value: @autoclosure () -> Bool, _ message: String) {
    if !value() { fatalError(message) }
}
@MainActor func until(_ condition: () -> Bool) async {
    let deadline = Date().addingTimeInterval(10)
    while Date() < deadline {
        if condition() { return }
        // A fixed yield count can finish before the Darwin main actor or a KDF
        // worker is scheduled. Suspend against a real deadline on both platforms.
        try? await Task.sleep(nanoseconds: 1_000_000)
    }
    fatalError("model operation did not settle")
}
@main enum Check {
    @MainActor static func main() async throws {
        let suite = "LocalRemoteDesktop.NameModelCheck." + UUID().uuidString
        let preferences = UserDefaults(suiteName: suite)!
        defer { preferences.removePersistentDomain(forName: suite) }
        let model = HostModel(namePreferences: preferences)
        model.controlling = true; model.start(requestPermission: true)
        require(!model.active && !model.starting && Pairing.readAttempts == 0, "sharing started while controlling another desktop")
        model.controlling = false
        model.start()
        require(Pairing.readAttempts == 0 && !model.active, "development launch read credentials")
        model.openPasswordEditor(); model.generatePassword()
        require(model.editingPassword && model.passwordEditor.draft.count == 8, "generation did not fill draft")
        model.start(requestPermission: true)
        require(!model.sharingActionAvailable && !model.starting && Host.instances.isEmpty && Pairing.readAttempts == 0, "unsaved password draft started sharing")
        model.cancelPasswordEditor()
        require(!model.editingPassword && model.passwordEditor.draft.isEmpty && Pairing.saveAttempts == 0, "cancel saved a draft")
        model.openNameEditor(); model.nameDraft = "Unsaved fixture"
        model.start(requestPermission: true)
        require(!model.sharingActionAvailable && !model.starting && Host.instances.isEmpty, "unsaved name started sharing")
        model.cancelNameEditor(); require(model.sharingActionAvailable, "cancel did not restore start action")
        let idleStatus = model.status
        model.togglePasswordVisibility(); await until { !model.readingPassword }
        require(model.revealSecret && model.legacyCredential && !model.active && model.status == idleStatus, "view password changed sharing")
        model.togglePasswordVisibility(); model.togglePasswordVisibility()
        require(Pairing.readAttempts == 1, "cached password accessed store again")
        let display = PairingEditor.displayed(model.secret)
        require(display.split(separator: "\n").allSatisfy { $0.count <= 24 }, "legacy key can widen window")
        model.copyPassword()
        require(NSPasteboard.general.text == model.secret && !NSPasteboard.general.text.contains("\n"), "copy used wrapped presentation")
        print("PASS draft cancellation, deliberate/cached reveal, legacy bounded display and exact copying; sharing state unchanged")

        model.openPasswordEditor(); model.passwordEditor.draft = "short"
        require(!model.passwordEditor.canSave(current: model.secret), "invalid password can save")
        model.savePassword(); require(Pairing.saveAttempts == 0, "invalid editor called store")
        model.passwordEditor.draft = model.secret
        require(!model.passwordEditor.canSave(current: model.secret), "same password rotates session")
        model.cancelPasswordEditor()
        model.start(requestPermission: true); await until { !model.starting }
        require(model.active && Pairing.readAttempts == 1, "sharing did not reuse credential")
        let original = Host.instances.last!, oldSecret = model.secret, sharingStatus = model.status
        let nameBefore = model.deviceName, discoveryStarts = HostDiscovery.starts
        model.openNameEditor(); model.nameDraft = "Discarded name"; model.cancelNameEditor()
        require(model.deviceName == nameBefore && preferences.string(forKey: DeviceName.preference) == nil, "name cancel persisted a draft")
        model.openNameEditor(); model.nameDraft = "Office fixture"; model.saveName()
        require(model.active && !original.stopped && model.secret == oldSecret && Pairing.saveAttempts == 0, "rename changed sharing or pairing")
        require(original.deviceName == "Office fixture" && HostDiscovery.lastName == "Office fixture" && HostDiscovery.starts == discoveryStarts + 1, "rename did not update active metadata and advertisement")
        require(DeviceName.current(preferences) == "Office fixture" && !model.editingName, "rename not persisted or editor left open")
        print("PASS isolated name editor cancellation/persistence and active rename preserves listener/password while updating metadata/publication")
        model.openPasswordEditor(); model.generatePassword(); let draft = model.passwordEditor.draft
        Pairing.rejectSave = true; model.savePassword(); await until { !model.passwordEditor.saving }
        require(model.active && !original.stopped && model.secret == oldSecret && model.status == sharingStatus, "failed save stopped sharing or replaced credential")
        require(model.editingPassword && model.passwordEditor.draft == draft && !model.passwordEditor.error.isEmpty, "failed save lost editor")
        model.passwordEditor.draft = "Office#2"
        require(model.passwordEditor.error.isEmpty, "editing retained stale error")
        print("PASS inline validation, same-password no-op and failed-save draft/session preservation")

        model.fileStatus = "接收文件：fixture"
        Pairing.rejectSave = false; model.savePassword(); await until { !model.passwordEditor.saving && !model.starting }
        require(!model.active && original.stopped && Host.instances.count == 1 && model.sharingActionAvailable, "successful save auto-started sharing or kept old host")
        require(model.fileStatus == "文件同步：等待连接", "password revocation retains stale file progress")
        model.start(requestPermission: true); await until { !model.starting }
        require(model.active && Host.instances.count == 2, "explicit start did not adopt saved password")
        require(model.secret == "Office#2" && !model.legacyCredential && !model.editingPassword && !model.revealSecret, "saved credential not adopted/hidden")
        require(Pairing.readAttempts == 1 && Host.instances.last!.passwordSalt?.count == 16, "saved host reread credentials or lost salt")
        let updatedStatus = model.status, updatedFileStatus = model.fileStatus
        original.status("stale old connection"); original.fileStatus("stale old files")
        for _ in 0..<100 { await Task.yield() }
        require(model.status == updatedStatus && model.fileStatus == updatedFileStatus, "stale host callback overwrote new session")
        print("PASS successful save closes old host, waits for explicit restart with new cached credential and filters old callbacks")

        model.openPasswordEditor(); model.generatePassword(); Pairing.pauseSave = true
        model.savePassword(); await until { Pairing.pendingSave != nil }
        require(model.sharingActionAvailable, "save made active Stop action unavailable")
        model.cancelPasswordEditor()
        require(model.editingPassword && model.passwordEditor.saving, "saving editor dismissed")
        model.stop(); Pairing.pendingSave?.resume(); Pairing.pendingSave = nil
        await until { !model.passwordEditor.saving }
        require(!model.active && !model.starting && Host.instances.count == 2 && model.status == "已停止", "stop during save restarted sharing")
        print("PASS busy dismissal guard and stop/quit during persistence never restarts sharing")
        require(HostDiscovery.starts == 3 && HostDiscovery.stops >= 2, "discovery not tied to sharing/password lifecycle")
        print("PASS discovery starts with successful sharing, stops on password replacement and stop")
        print("SKIP native SwiftUI/Apple SDK/Keychain: production model compiled with explicit service doubles.")
    }
}
