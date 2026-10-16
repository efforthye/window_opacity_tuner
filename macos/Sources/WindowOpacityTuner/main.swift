import AppKit
import Combine
import SwiftUI

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate {
    private let settings = AppSettings()
    private let service = OpacityService()
    private lazy var model = TunerModel(settings: settings, service: service)
    private var window: NSWindow!
    private var bag = Set<AnyCancellable>()

    func applicationDidFinishLaunching(_ notification: Notification) {
        let root = MainView(model: model, settings: settings, service: service)
        let hosting = NSHostingView(rootView: root)

        window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 440, height: 360),
            styleMask: [.titled, .closable, .miniaturizable],
            backing: .buffered,
            defer: false
        )
        window.contentView = hosting
        window.delegate = self
        window.isReleasedWhenClosed = false
        window.setFrameAutosaveName("MainWindow")
        if !window.setFrameUsingName("MainWindow") {
            window.center()
        }
        window.makeKeyAndOrderFront(nil)
        MainWindowRegistry.mainWindowID = CGWindowID(window.windowNumber)

        settings.$language.sink { [weak self] lang in
            self?.window.title = Loc.t("AppTitle", lang)
            self?.buildMenu(lang)
        }.store(in: &bag)
        settings.$theme.sink { theme in
            NSApp.appearance = NSAppearance(named: theme == .dark ? .darkAqua : .aqua)
        }.store(in: &bag)
        settings.$selfOpacityPercent.sink { [weak self] p in
            self?.window.alphaValue = CGFloat(p) / 100
        }.store(in: &bag)
        settings.$alwaysOnTop.sink { [weak self] onTop in
            self?.window.level = onTop ? .floating : .normal
        }.store(in: &bag)

        NSApp.activate(ignoringOtherApps: true)
    }

    func applicationWillTerminate(_ notification: Notification) {
        service.restoreAll()
    }

    func windowWillClose(_ notification: Notification) {
        NSApp.terminate(nil)
    }

    private func buildMenu(_ lang: String) {
        let main = NSMenu()
        let appItem = NSMenuItem()
        let appMenu = NSMenu()
        appMenu.addItem(withTitle: "\(Loc.t("Quit", lang)) \(Loc.t("AppTitle", lang))",
                        action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        appItem.submenu = appMenu
        main.addItem(appItem)

        // Standard Edit menu so copy/paste shortcuts work in any text field.
        let editItem = NSMenuItem()
        let edit = NSMenu(title: "Edit")
        edit.addItem(withTitle: "Cut", action: #selector(NSText.cut(_:)), keyEquivalent: "x")
        edit.addItem(withTitle: "Copy", action: #selector(NSText.copy(_:)), keyEquivalent: "c")
        edit.addItem(withTitle: "Paste", action: #selector(NSText.paste(_:)), keyEquivalent: "v")
        edit.addItem(withTitle: "Select All", action: #selector(NSText.selectAll(_:)), keyEquivalent: "a")
        editItem.submenu = edit
        main.addItem(editItem)

        NSApp.mainMenu = main
    }
}

MainActor.assumeIsolated {
    let app = NSApplication.shared
    let delegate = AppDelegate()
    app.delegate = delegate
    app.setActivationPolicy(.regular)
    app.run()
}
