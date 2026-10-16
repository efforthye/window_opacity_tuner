import AppKit
import ScreenCaptureKit

/// Owns every overlay and keeps them tracking their windows.
@MainActor
final class OpacityService: ObservableObject {
    struct Adjusted: Identifiable, Equatable {
        let id: CGWindowID
        let name: String
        let opacity: Double
    }

    @Published private(set) var adjusted: [Adjusted] = []

    /// Raised when a tracked window closes or its capture fails.
    var onWindowGone: ((CGWindowID) -> Void)?
    var onCaptureFailed: ((CGWindowID, Error) -> Void)?

    private var overlays: [CGWindowID: WindowOverlay] = [:]
    private var order: [CGWindowID] = []
    private var timer: Timer?

    var overlayWindowIDs: Set<CGWindowID> {
        Set(overlays.values.map(\.windowNumber))
    }

    func opacity(for id: CGWindowID) -> Double {
        overlays[id]?.opacity ?? 1
    }

    /// Sets a window's apparent opacity, 0...1. Fully opaque removes the overlay.
    func setOpacity(_ value: Double, for target: WindowSnapshot) {
        if value >= 0.999 {
            restore(target.id)
            return
        }
        if let overlay = overlays[target.id] {
            overlay.setOpacity(value)
            publish()
            return
        }

        let overlay = WindowOverlay(target: target, opacity: value)
        overlay.onStreamError = { [weak self] error in
            self?.restore(target.id)
            self?.onCaptureFailed?(target.id, error)
        }
        overlays[target.id] = overlay
        order.append(target.id)
        publish()
        startTimer()

        Task {
            do {
                try await overlay.start()
            } catch {
                self.restore(target.id)
                if case OverlayError.windowGone = error {
                    self.onWindowGone?(target.id)
                } else {
                    self.onCaptureFailed?(target.id, error)
                }
            }
        }
    }

    func setOpacity(_ value: Double, forTracked id: CGWindowID) {
        guard let overlay = overlays[id] else { return }
        if value >= 0.999 {
            restore(id)
        } else {
            overlay.setOpacity(value)
            publish()
        }
    }

    func restore(_ id: CGWindowID) {
        guard let overlay = overlays.removeValue(forKey: id) else { return }
        overlay.stop()
        order.removeAll { $0 == id }
        publish()
        if overlays.isEmpty {
            timer?.invalidate()
            timer = nil
        }
    }

    func restoreAll() {
        for id in order {
            restore(id)
        }
    }

    private func startTimer() {
        guard timer == nil else { return }
        // 60 Hz keeps the overlay close behind a window being dragged.
        let t = Timer(timeInterval: 1.0 / 60.0, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated { self?.tick() }
        }
        RunLoop.main.add(t, forMode: .common)
        timer = t
    }

    private func tick() {
        var changed = false
        for id in order {
            guard let overlay = overlays[id] else { continue }
            let before = overlay.title
            if overlay.track() == .targetGone {
                restore(id)
                onWindowGone?(id)
                continue
            }
            changed = changed || overlay.title != before
        }
        if changed {
            publish()
        }
    }

    private func publish() {
        let rows = order.compactMap { id in
            overlays[id].map { Adjusted(id: id, name: $0.displayName, opacity: $0.opacity) }
        }
        if rows != adjusted {
            adjusted = rows
        }
    }
}
