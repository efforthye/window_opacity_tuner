import AppKit
import CoreMedia
import ScreenCaptureKit

/// Makes one window of another app look translucent.
///
/// macOS has no public API for changing another process's window alpha, so this
/// does the opposite: it captures the screen *without* the target window (what
/// would be visible if the target were not there) and draws that on top of the
/// target at `1 - opacity`. The result on screen is the same blend a real
/// translucent window would produce. The overlay ignores the mouse, so clicks
/// and scrolling still go to the real window underneath.
///
/// Nothing is changed on the target itself: removing the overlay — or quitting
/// the app — puts the window straight back to normal.
final class WindowOverlay: NSObject, SCStreamOutput, SCStreamDelegate {
    let targetID: CGWindowID
    let appName: String
    private(set) var title: String
    private(set) var opacity: Double

    /// Called on the main queue when capture stops on its own.
    var onStreamError: ((Error) -> Void)?

    private let window: NSWindow
    private let imageLayer = CALayer()
    private let frameQueue = DispatchQueue(label: "WindowOverlay.frames")

    private var stream: SCStream?
    private var displayID: CGDirectDisplayID?
    private var displayBounds: CGRect = .zero
    private var hasFrame = false
    private var shown = false
    private var restarting = false

    init(target: WindowSnapshot, opacity: Double) {
        targetID = target.id
        appName = target.appName
        title = target.title
        self.opacity = opacity

        window = NSWindow(contentRect: .zero, styleMask: [.borderless], backing: .buffered, defer: false)
        window.isOpaque = false
        window.backgroundColor = .clear
        window.hasShadow = false
        window.ignoresMouseEvents = true
        window.isReleasedWhenClosed = false
        window.animationBehavior = .none
        window.level = .normal
        // Visibility is driven by whether the target is on screen, so the
        // overlay can follow it to any Space, including a full-screen one.
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .ignoresCycle]

        imageLayer.contentsGravity = .resize
        imageLayer.actions = ["contents": NSNull(), "contentsRect": NSNull(), "bounds": NSNull(), "position": NSNull()]
        let view = NSView()
        view.layer = imageLayer
        view.wantsLayer = true
        window.contentView = view

        super.init()
        window.alphaValue = 1 - opacity
    }

    var windowNumber: CGWindowID {
        CGWindowID(window.windowNumber)
    }

    var displayName: String {
        title.isEmpty ? appName : "\(title) — \(appName)"
    }

    func setOpacity(_ value: Double) {
        opacity = value
        window.alphaValue = 1 - value
    }

    // MARK: Capture

    func start() async throws {
        guard let target = WindowList.info(targetID) else {
            throw OverlayError.windowGone
        }
        guard let display = ScreenGeometry.bestDisplay(for: target.bounds) else {
            throw OverlayError.noDisplay
        }
        try await startStream(on: display)
    }

    func stop() {
        stream?.stopCapture { _ in }
        stream = nil
        window.orderOut(nil)
        shown = false
    }

    private func startStream(on display: CGDirectDisplayID) async throws {
        let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: false)
        guard let scDisplay = content.displays.first(where: { $0.displayID == display }) else {
            throw OverlayError.noDisplay
        }

        // Leave out the target and our own overlay for it; everything else is
        // what really sits behind the target. The tuner's own window is left out
        // too, since it normally floats above and would otherwise ghost into the
        // background.
        let ownOverlay = windowNumber
        let excluded = content.windows.filter {
            $0.windowID == targetID
                || $0.windowID == ownOverlay
                || ($0.owningApplication?.processID == WindowList.ownPID && $0.windowLayer != 0)
                || $0.windowID == MainWindowRegistry.mainWindowID
        }
        let filter = SCContentFilter(display: scDisplay, excludingWindows: excluded)

        let bounds = CGDisplayBounds(display)
        let scale = ScreenGeometry.backingScale(of: display)
        let config = SCStreamConfiguration()
        config.width = Int(bounds.width * scale)
        config.height = Int(bounds.height * scale)
        config.showsCursor = false
        config.pixelFormat = kCVPixelFormatType_32BGRA
        config.minimumFrameInterval = CMTime(value: 1, timescale: 60)
        config.queueDepth = 5

        let newStream = SCStream(filter: filter, configuration: config, delegate: self)
        try newStream.addStreamOutput(self, type: .screen, sampleHandlerQueue: frameQueue)
        try await newStream.startCapture()

        await MainActor.run {
            self.stream = newStream
            self.displayID = display
            self.displayBounds = bounds
        }
    }

    func stream(_ stream: SCStream, didOutputSampleBuffer sampleBuffer: CMSampleBuffer, of type: SCStreamOutputType) {
        guard type == .screen,
              let attachments = CMSampleBufferGetSampleAttachmentsArray(sampleBuffer, createIfNecessary: false)
                  as? [[SCStreamFrameInfo: Any]],
              let rawStatus = attachments.first?[.status] as? Int,
              SCFrameStatus(rawValue: rawStatus) == .complete,
              let pixelBuffer = CMSampleBufferGetImageBuffer(sampleBuffer),
              let surface = CVPixelBufferGetIOSurface(pixelBuffer)?.takeUnretainedValue()
        else { return }

        DispatchQueue.main.async {
            CATransaction.begin()
            CATransaction.setDisableActions(true)
            self.imageLayer.contents = surface
            CATransaction.commit()
            if !self.hasFrame {
                self.hasFrame = true
                self.track()
            }
        }
    }

    func stream(_ stream: SCStream, didStopWithError error: Error) {
        DispatchQueue.main.async {
            guard stream === self.stream else { return }
            self.stream = nil
            self.window.orderOut(nil)
            self.shown = false
            self.onStreamError?(error)
        }
    }

    // MARK: Tracking

    enum TrackResult {
        case ok
        case targetGone
    }

    /// Keeps the overlay glued to the target: same frame, stacked directly
    /// above it, hidden while it is minimized, hidden or on another Space.
    @discardableResult
    func track() -> TrackResult {
        guard let target = WindowList.info(targetID) else {
            return .targetGone
        }
        if !target.title.isEmpty {
            title = target.title
        }

        guard target.isOnScreen, hasFrame, let displayID, stream != nil else {
            hide()
            return .ok
        }

        if let best = ScreenGeometry.bestDisplay(for: target.bounds), best != displayID, !restarting {
            moveStream(to: best)
            hide()
            return .ok
        }

        let visible = target.bounds.intersection(displayBounds)
        guard !visible.isNull, visible.width > 1, visible.height > 1 else {
            hide()
            return .ok
        }

        let frame = ScreenGeometry.appKitRect(fromCG: visible)
        if window.frame != frame {
            window.setFrame(frame, display: false)
        }

        // contentsRect is in unit coordinates of the captured display image,
        // measured from the bottom like AppKit, while CG rects run top-down.
        let rect = CGRect(
            x: (visible.minX - displayBounds.minX) / displayBounds.width,
            y: (displayBounds.maxY - visible.maxY) / displayBounds.height,
            width: visible.width / displayBounds.width,
            height: visible.height / displayBounds.height
        )
        if imageLayer.contentsRect != rect {
            CATransaction.begin()
            CATransaction.setDisableActions(true)
            imageLayer.contentsRect = rect
            CATransaction.commit()
        }

        if !shown || WindowList.idDirectlyAbove(targetID) != windowNumber {
            window.order(.above, relativeTo: Int(targetID))
            shown = true
        }
        return .ok
    }

    private func hide() {
        if shown {
            window.orderOut(nil)
            shown = false
        }
    }

    private func moveStream(to display: CGDirectDisplayID) {
        restarting = true
        hasFrame = false
        stream?.stopCapture { _ in }
        stream = nil
        Task {
            do {
                try await startStream(on: display)
            } catch {
                await MainActor.run { self.onStreamError?(error) }
            }
            await MainActor.run { self.restarting = false }
        }
    }
}

enum OverlayError: Error {
    case windowGone
    case noDisplay
}

/// The tuner's main window id, so overlays can keep it out of their captures.
enum MainWindowRegistry {
    static var mainWindowID: CGWindowID = 0
}
