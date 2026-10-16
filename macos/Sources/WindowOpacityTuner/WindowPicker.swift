import AppKit

/// Click-to-pick: covers every screen with a nearly invisible window that eats
/// the mouse, outlines the window under the cursor in green, and reports the one
/// clicked. Esc cancels.
@MainActor
final class WindowPicker {
    enum Outcome {
        case picked(WindowSnapshot)
        case ownWindow
        case cancelled
    }

    private var shields: [NSWindow] = []
    private var highlight: NSWindow?
    private var monitor: Any?
    private var resignObserver: NSObjectProtocol?
    private var ignoring: Set<CGWindowID> = []
    private var completion: ((Outcome) -> Void)?
    private var current: WindowSnapshot?

    var isActive: Bool { completion != nil }

    func begin(ignoring: Set<CGWindowID>, completion: @escaping (Outcome) -> Void) {
        cancel()
        self.ignoring = ignoring
        self.completion = completion

        for screen in NSScreen.screens {
            let shield = KeyableWindow(contentRect: screen.frame, styleMask: [.borderless], backing: .buffered, defer: false)
            // Fully clear windows let clicks through; this alpha is invisible but still hit-tests.
            shield.backgroundColor = NSColor(white: 0, alpha: 0.004)
            shield.isOpaque = false
            shield.level = .popUpMenu
            shield.hasShadow = false
            shield.acceptsMouseMovedEvents = true
            shield.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
            shield.isReleasedWhenClosed = false
            shield.orderFrontRegardless()
            shields.append(shield)
        }
        shields.forEach { self.ignoring.insert(CGWindowID($0.windowNumber)) }

        let hl = NSWindow(contentRect: .zero, styleMask: [.borderless], backing: .buffered, defer: false)
        hl.isOpaque = false
        hl.backgroundColor = .clear
        hl.hasShadow = false
        hl.ignoresMouseEvents = true
        hl.level = .popUpMenu + 1
        hl.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
        hl.isReleasedWhenClosed = false
        hl.contentView = OutlineView()
        highlight = hl
        self.ignoring.insert(CGWindowID(hl.windowNumber))

        NSApp.activate(ignoringOtherApps: true)
        shields.first?.makeKey()

        monitor = NSEvent.addLocalMonitorForEvents(matching: [.mouseMoved, .leftMouseDown, .leftMouseDragged, .keyDown, .rightMouseDown]) { [weak self] event in
            guard let self else { return event }
            switch event.type {
            case .keyDown where event.keyCode == 53: // Esc
                self.finish(.cancelled)
            case .rightMouseDown:
                self.finish(.cancelled)
            case .leftMouseDown:
                self.update()
                let hit = self.hitTest()
                if hit.own {
                    self.finish(.ownWindow)
                } else if let w = hit.window {
                    self.finish(.picked(w))
                }
            default:
                self.update()
            }
            return nil
        }
        // The shields swallow every click, so never leave them up once the
        // user has switched to another app (Esc would no longer reach us).
        resignObserver = NotificationCenter.default.addObserver(
            forName: NSApplication.didResignActiveNotification, object: nil, queue: .main
        ) { [weak self] _ in
            MainActor.assumeIsolated { self?.cancel() }
        }
        update()
    }

    func cancel() {
        if completion != nil {
            finish(.cancelled)
        }
    }

    private func hitTest() -> (window: WindowSnapshot?, own: Bool) {
        let point = ScreenGeometry.cgPoint(fromAppKit: NSEvent.mouseLocation)
        let (w, own) = WindowList.window(at: point, ignoring: ignoring)
        return (w, own)
    }

    private func update() {
        let hit = hitTest()
        guard let w = hit.window else {
            current = nil
            highlight?.orderOut(nil)
            return
        }
        if w != current {
            current = w
            highlight?.setFrame(ScreenGeometry.appKitRect(fromCG: w.bounds).insetBy(dx: -3, dy: -3), display: true)
            highlight?.orderFrontRegardless()
        }
    }

    private func finish(_ outcome: Outcome) {
        if let monitor {
            NSEvent.removeMonitor(monitor)
        }
        monitor = nil
        if let resignObserver {
            NotificationCenter.default.removeObserver(resignObserver)
        }
        resignObserver = nil
        shields.forEach { $0.orderOut(nil) }
        shields.removeAll()
        highlight?.orderOut(nil)
        highlight = nil
        current = nil
        let done = completion
        completion = nil
        done?(outcome)
    }
}

private final class KeyableWindow: NSWindow {
    override var canBecomeKey: Bool { true }
}

private final class OutlineView: NSView {
    override func draw(_ dirtyRect: NSRect) {
        let path = NSBezierPath(roundedRect: bounds.insetBy(dx: 2, dy: 2), xRadius: 12, yRadius: 12)
        path.lineWidth = 4
        NSColor(red: 0x2E / 255, green: 0xE6 / 255, blue: 0x5C / 255, alpha: 1).setStroke()
        path.stroke()
    }
}
