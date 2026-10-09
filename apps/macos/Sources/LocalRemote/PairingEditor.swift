import Foundation

// Drafts are separate from the persisted credential and sharing state.
struct PairingEditor {
    var draft = "" { didSet { error = "" } }
    private(set) var saving = false
    private(set) var error = ""

    func validation(current: String) -> String? {
        if draft.isEmpty { return nil }
        do { try PairingCredential.validate(draft) }
        catch { return error.localizedDescription }
        return draft == current ? "与当前密码相同，无需保存。" : nil
    }
    func canSave(current: String) -> Bool {
        !saving && !draft.isEmpty && validation(current: current) == nil
    }
    mutating func generate(_ password: String) {
        guard !saving else { return }
        draft = password; error = ""
    }
    mutating func beginSave(current: String) -> String? {
        guard canSave(current: current) else { return nil }
        saving = true; error = ""; return draft
    }
    mutating func failed(_ message: String) { saving = false; error = message }
    mutating func reset() { self = Self() }

    static func displayed(_ password: String) -> String {
        // Break even an unspaced legacy key into bounded lines. Copying uses the
        // original credential, never this presentation string.
        let chars = Array(password)
        return stride(from: 0, to: chars.count, by: 24)
            .map { String(chars[$0..<min($0 + 24, chars.count)]) }.joined(separator: "\n")
    }
}
