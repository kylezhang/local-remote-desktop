import Foundation
import ApplicationServices
import CoreGraphics

struct InputAccessState {
    let accessibility: Bool
    let eventPosting: Bool
    // We send CGEvents, rather than scripting other apps through AX APIs.
    // Ask the permission API for that operation; keep AX as separate evidence.
    var canControl: Bool { eventPosting }
}

enum MacInputAccess {
    static var canPost: Bool { CGPreflightPostEventAccess() }
    static func snapshot() -> InputAccessState {
        InputAccessState(accessibility: AXIsProcessTrusted(), eventPosting: canPost)
    }
    static func request() {
        if !CGPreflightPostEventAccess() { _ = CGRequestPostEventAccess() }
    }
}
