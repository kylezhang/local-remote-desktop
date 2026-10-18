import Foundation
#if canImport(Combine)
import Combine
#endif

// Page selection does not own the transport. Leaving the canvas releases input;
// returning reuses the same controller, decoder and authenticated session.
final class DesktopNavigation: ObservableObject {
    @Published var page = "home"
    func show(_ destination: String, controller: ControllerModel) {
        guard !controller.busy || !["connect", "share"].contains(destination) else { return }
        if page == "session" && destination != "session" { controller.setScreenVisible(false) }
        page = destination
        if destination == "session" { controller.setScreenVisible(true) }
    }
    func sessionChanged(_ controller: ControllerModel) {
        if controller.busy && !controller.manualOpen && ["home", "connect"].contains(page) { show("session", controller: controller) }
        else if !controller.busy && page == "session" { page = "connect" }
    }
}
