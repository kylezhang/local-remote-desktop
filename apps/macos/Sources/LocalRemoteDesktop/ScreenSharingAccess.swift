import Foundation
import AppKit
import ScreenCaptureKit
import CoreGraphics

enum ScreenSharingError: Error, Equatable { case chooseMainDisplay, selectionUnavailable }

struct ScreenCaptureSource {
    let filter: SCContentFilter
    let displayID: CGDirectDisplayID
    let width: Int, height: Int
    let bounds: CGRect
    var current: Bool { displayID == CGMainDisplayID() && bounds == CGDisplayBounds(displayID) }

    static func legacyPrimary() async throws -> Self {
        let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)
        guard let display = content.displays.first(where: { $0.displayID == CGMainDisplayID() }) else { throw CaptureError.noDisplay }
        return Self(filter: SCContentFilter(display: display, excludingWindows: []), displayID: display.displayID,
                    width: display.width, height: display.height, bounds: CGDisplayBounds(display.displayID))
    }
    @available(macOS 14.0, *)
    static func selected(_ filter: SCContentFilter) throws -> Self {
        let id = CGMainDisplayID(), bounds = CGDisplayBounds(id)
        if #available(macOS 15.2, *) {
            guard filter.includedDisplays.count == 1, let display = filter.includedDisplays.first,
                  display.displayID == id else { throw ScreenSharingError.chooseMainDisplay }
            return Self(filter: filter, displayID: id, width: display.width, height: display.height, bounds: bounds)
        }
        // Older pickers do not expose the selected display ID. Only accept an
        // unambiguous single-display desktop; never inject input into another screen.
        var count: UInt32 = 0
        guard CGGetActiveDisplayList(0, nil, &count) == .success, count == 1,
              abs(filter.contentRect.width - bounds.width) < 1,
              abs(filter.contentRect.height - bounds.height) < 1 else { throw ScreenSharingError.chooseMainDisplay }
        let scale = Double(filter.pointPixelScale)
        guard scale.isFinite, scale > 0, scale <= 4 else { throw ScreenSharingError.selectionUnavailable }
        return Self(filter: filter, displayID: id, width: Int(Double(bounds.width)*scale),
                    height: Int(Double(bounds.height)*scale), bounds: bounds)
    }
}

// Keep the system-selected filter for this application run. It is the permission
// token; querying SCShareableContent again would re-enter the stale global TCC gate.
final class ScreenSharingAccess {
    private var source: ScreenCaptureSource?
    private var request: AnyObject?
    private(set) var mode = "not checked"
    @MainActor func select(interactive: Bool) async throws -> ScreenCaptureSource {
        if let source, source.current { return source }
        source = nil
        if #available(macOS 14.0, *), interactive {
            mode = "system display picker"
            let current = DisplaySharingPicker(); request = current
            defer { if request === current { request = nil } }
            let selected = try await current.select()
            try Task.checkCancellation()
            source = selected; return selected
        }
        mode = "legacy ScreenCaptureKit permission"
        let selected = try await ScreenCaptureSource.legacyPrimary()
        try Task.checkCancellation()
        source = selected; return selected
    }
    func cancelPending() {
        if #available(macOS 14.0, *), let current = request as? DisplaySharingPicker { current.cancel() }
    }
    func invalidate() { source = nil; cancelPending() }
}

@available(macOS 14.0, *)
private final class DisplaySharingPicker: NSObject, SCContentSharingPickerObserver {
    private var continuation: CheckedContinuation<ScreenCaptureSource, Error>?
    private var timeout: DispatchWorkItem?
    private let picker = SCContentSharingPicker.shared
    @MainActor func select() async throws -> ScreenCaptureSource {
        try Task.checkCancellation()
        return try await withTaskCancellationHandler(operation: {
            try await withCheckedThrowingContinuation { continuation in
                self.continuation = continuation
                let timeout = DispatchWorkItem { [weak self] in self?.finish(.failure(ScreenSharingError.selectionUnavailable)) }
                self.timeout = timeout; DispatchQueue.main.asyncAfter(deadline: .now()+120, execute: timeout)
                let configuration = SCContentSharingPickerConfiguration()
                configuration.allowedPickerModes = [.singleDisplay]
                configuration.allowsChangingSelectedContent = false
                picker.defaultConfiguration = configuration
                picker.add(self); picker.isActive = true
                picker.present(using: .display)
            }
        }, onCancel: { [weak self] in
            DispatchQueue.main.async { self?.cancel() }
        })
    }
    func cancel() { finish(.failure(CancellationError())) }
    private func finish(_ result: Result<ScreenCaptureSource, Error>) {
        guard let continuation else { return }
        self.continuation = nil; picker.remove(self)
        timeout?.cancel(); timeout = nil
        if case .failure = result { picker.isActive = false }
        continuation.resume(with: result)
    }
    func contentSharingPicker(_ picker: SCContentSharingPicker, didCancelFor stream: SCStream?) {
        DispatchQueue.main.async { self.cancel() }
    }
    func contentSharingPicker(_ picker: SCContentSharingPicker, didUpdateWith filter: SCContentFilter, for stream: SCStream?) {
        guard stream == nil else { return }
        DispatchQueue.main.async { self.finish(Result { try ScreenCaptureSource.selected(filter) }) }
    }
    func contentSharingPickerStartDidFailWithError(_ error: Error) {
        DispatchQueue.main.async { self.finish(.failure(error)) }
    }
}
