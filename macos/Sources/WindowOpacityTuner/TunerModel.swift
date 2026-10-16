import AppKit
import CoreGraphics

/// State behind the main window: the picked window, its slider, status line.
@MainActor
final class TunerModel: ObservableObject {
    @Published private(set) var selected: WindowSnapshot?
    @Published var opacityPercent: Double = 100
    @Published private(set) var status: String?
    @Published private(set) var picking = false
    @Published private(set) var hasPermission = CGPreflightScreenCaptureAccess()
    @Published var focusSliderToken = 0

    let settings: AppSettings
    let service: OpacityService
    private let picker = WindowPicker()

    init(settings: AppSettings, service: OpacityService) {
        self.settings = settings
        self.service = service

        service.onWindowGone = { [weak self] id in
            guard let self, self.selected?.id == id else { return }
            self.selected = nil
            self.opacityPercent = 100
            self.status = self.settings.t("WindowGone")
        }
        service.onCaptureFailed = { [weak self] id, _ in
            guard let self else { return }
            if self.selected?.id == id {
                self.opacityPercent = 100
            }
            self.refreshPermission()
            self.status = self.settings.t("CaptureFailed")
        }
    }

    var minPercent: Double {
        Double(settings.minimumOpacityPercent)
    }

    func refreshPermission() {
        hasPermission = CGPreflightScreenCaptureAccess()
    }

    func requestPermission() {
        if !CGRequestScreenCaptureAccess() {
            openPrivacySettings()
        }
        refreshPermission()
    }

    func openPrivacySettings() {
        if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture") {
            NSWorkspace.shared.open(url)
        }
    }

    func pick() {
        refreshPermission()
        if !hasPermission {
            // The picker would cover the system prompt, so ask first and stop here.
            requestPermission()
            return
        }
        status = settings.t("PickingHint")
        picking = true
        picker.begin(ignoring: service.overlayWindowIDs) { [weak self] outcome in
            guard let self else { return }
            self.picking = false
            self.status = nil
            switch outcome {
            case .picked(let w):
                self.selected = w
                self.opacityPercent = (self.service.opacity(for: w.id) * 100).rounded()
                // Bring the tuner back with the slider focused, as on Windows.
                NSApp.activate(ignoringOtherApps: true)
                self.focusSliderToken += 1
            case .ownWindow:
                self.status = self.settings.t("OwnWindowBlocked")
            case .cancelled:
                break
            }
        }
    }

    func cancelPick() {
        picker.cancel()
    }

    func applyOpacity() {
        guard let selected else { return }
        let percent = max(minPercent, min(100, opacityPercent.rounded()))
        // Re-read the window so a moved or renamed window is tracked from where it is now.
        guard let fresh = WindowList.info(selected.id) else {
            self.selected = nil
            opacityPercent = 100
            status = settings.t("WindowGone")
            return
        }
        self.selected = fresh
        service.setOpacity(percent / 100, for: fresh)
    }

    func reset() {
        opacityPercent = 100
        applyOpacity()
    }

    /// Keeps the main slider in step when a window is changed from Settings.
    func syncFromService() {
        if let selected {
            opacityPercent = (service.opacity(for: selected.id) * 100).rounded()
        }
    }
}
