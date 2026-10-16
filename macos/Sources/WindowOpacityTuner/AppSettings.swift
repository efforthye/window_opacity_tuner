import Foundation

enum ThemeMode: String {
    case light
    case dark
}

/// User preferences, kept in UserDefaults (~/Library/Preferences/<bundle id>.plist).
/// Same fields and defaults as the Windows build, minus restore-on-exit: overlays
/// cannot outlive the app, so quitting always restores every window.
@MainActor
final class AppSettings: ObservableObject {
    static let opacityFloorPercent = 3
    static let selfOpacityFloorPercent = 5

    private let defaults = UserDefaults.standard

    @Published var language: String { didSet { defaults.set(language, forKey: "Language") } }
    @Published var theme: ThemeMode { didSet { defaults.set(theme.rawValue, forKey: "Theme") } }
    @Published var selfOpacityPercent: Int { didSet { defaults.set(selfOpacityPercent, forKey: "SelfOpacityPercent") } }
    @Published var alwaysOnTop: Bool { didSet { defaults.set(alwaysOnTop, forKey: "AlwaysOnTop") } }
    @Published var minimumOpacityPercent: Int { didSet { defaults.set(minimumOpacityPercent, forKey: "MinimumOpacityPercent") } }

    init() {
        defaults.register(defaults: [
            "Language": "en",
            "Theme": ThemeMode.light.rawValue,
            "SelfOpacityPercent": 100,
            "AlwaysOnTop": true,
            "MinimumOpacityPercent": Self.opacityFloorPercent,
        ])
        let lang = defaults.string(forKey: "Language") ?? "en"
        language = Loc.languages.contains { $0.tag == lang } ? lang : "en"
        theme = ThemeMode(rawValue: defaults.string(forKey: "Theme") ?? "") ?? .light
        selfOpacityPercent = min(100, max(Self.selfOpacityFloorPercent, defaults.integer(forKey: "SelfOpacityPercent")))
        alwaysOnTop = defaults.bool(forKey: "AlwaysOnTop")
        minimumOpacityPercent = min(90, max(Self.opacityFloorPercent, defaults.integer(forKey: "MinimumOpacityPercent")))
    }

    func t(_ key: String, _ args: CustomStringConvertible...) -> String {
        Loc.format(key, language, args)
    }
}
