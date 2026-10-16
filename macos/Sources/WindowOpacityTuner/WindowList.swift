import AppKit
import CoreGraphics

/// One row of CGWindowListCopyWindowInfo. Bounds are in CoreGraphics global
/// coordinates: origin at the top-left of the primary display, y growing down.
struct WindowSnapshot: Equatable {
    let id: CGWindowID
    let pid: pid_t
    let appName: String
    let title: String
    let bounds: CGRect
    let layer: Int
    let isOnScreen: Bool

    init?(_ dict: [String: Any]) {
        guard let id = dict[kCGWindowNumber as String] as? CGWindowID,
              let pid = dict[kCGWindowOwnerPID as String] as? pid_t,
              let boundsDict = dict[kCGWindowBounds as String] as? NSDictionary,
              let bounds = CGRect(dictionaryRepresentation: boundsDict)
        else { return nil }

        self.id = id
        self.pid = pid
        self.appName = dict[kCGWindowOwnerName as String] as? String ?? ""
        self.title = dict[kCGWindowName as String] as? String ?? ""
        self.bounds = bounds
        self.layer = dict[kCGWindowLayer as String] as? Int ?? 0
        self.isOnScreen = dict[kCGWindowIsOnscreen as String] as? Bool ?? false
    }

    /// "Title — App", or just the app when the window has no title (or the
    /// title is hidden because Screen Recording has not been granted).
    var displayName: String {
        title.isEmpty ? appName : "\(title) — \(appName)"
    }
}

enum WindowList {
    static let ownPID = getpid()

    static func info(_ id: CGWindowID) -> WindowSnapshot? {
        list([.optionIncludingWindow], id).first { $0.id == id }
    }

    /// The window stacked directly above `id`, or nil if it is the frontmost.
    static func idDirectlyAbove(_ id: CGWindowID) -> CGWindowID? {
        // Front-to-back order, so the nearest one is last.
        list([.optionOnScreenAboveWindow], id).last?.id
    }

    /// The frontmost normal (layer 0) window under a point in CG global
    /// coordinates, or `hitOwnWindow` when the tuner itself is in the way.
    /// Menu bar, Dock and other system layers are looked through: the Dock
    /// keeps an invisible window over the whole screen, so stopping at the
    /// first hit would never reach an app window.
    static func window(at point: CGPoint, ignoring: Set<CGWindowID>) -> (WindowSnapshot?, hitOwnWindow: Bool) {
        for w in list([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) {
            if ignoring.contains(w.id) || !w.bounds.contains(point) {
                continue
            }
            if w.pid == ownPID {
                return (nil, true)
            }
            if w.layer == 0 {
                return (w, false)
            }
        }
        return (nil, false)
    }

    private static func list(_ options: CGWindowListOption, _ relativeTo: CGWindowID) -> [WindowSnapshot] {
        guard let raw = CGWindowListCopyWindowInfo(options, relativeTo) as? [[String: Any]] else {
            return []
        }
        return raw.compactMap(WindowSnapshot.init)
    }
}

/// Conversions between CoreGraphics global coordinates (top-left origin) and
/// AppKit screen coordinates (bottom-left origin of the primary display).
enum ScreenGeometry {
    static var primaryHeight: CGFloat {
        NSScreen.screens.first?.frame.height ?? 0
    }

    static func appKitRect(fromCG r: CGRect) -> NSRect {
        NSRect(x: r.minX, y: primaryHeight - r.maxY, width: r.width, height: r.height)
    }

    static func cgPoint(fromAppKit p: NSPoint) -> CGPoint {
        CGPoint(x: p.x, y: primaryHeight - p.y)
    }

    static func displayID(of screen: NSScreen) -> CGDirectDisplayID? {
        screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? CGDirectDisplayID
    }

    /// The display holding the largest part of a CG-space rect.
    static func bestDisplay(for r: CGRect) -> CGDirectDisplayID? {
        var best: (CGDirectDisplayID, CGFloat)?
        for screen in NSScreen.screens {
            guard let id = displayID(of: screen) else { continue }
            let i = CGDisplayBounds(id).intersection(r)
            let area = i.isNull ? 0 : i.width * i.height
            if area > (best?.1 ?? 0) {
                best = (id, area)
            }
        }
        return best?.0
    }

    static func backingScale(of display: CGDirectDisplayID) -> CGFloat {
        NSScreen.screens.first { displayID(of: $0) == display }?.backingScaleFactor ?? 2
    }
}
