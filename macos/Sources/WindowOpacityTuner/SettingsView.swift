import SwiftUI

struct SettingsView: View {
    @ObservedObject var settings: AppSettings
    @ObservedObject var service: OpacityService
    let close: () -> Void

    private func t(_ key: String, _ args: CustomStringConvertible...) -> String {
        Loc.format(key, settings.language, args)
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            Text(t("Settings")).font(.title2.bold())

            Card {
                Text(t("Appearance")).font(.headline)
                Picker(t("Theme"), selection: $settings.theme) {
                    Text(t("Light")).tag(ThemeMode.light)
                    Text(t("Dark")).tag(ThemeMode.dark)
                }
                .pickerStyle(.segmented)
                Picker(t("Language"), selection: $settings.language) {
                    ForEach(Loc.languages) { Text($0.nativeName).tag($0.tag) }
                }
            }

            Card {
                Text(t("Behavior")).font(.headline)
                Toggle(t("AlwaysOnTop"), isOn: $settings.alwaysOnTop)
                Text(t("AlwaysOnTopHint")).font(.caption).foregroundStyle(.secondary)
                Text(t("MinOpacity", settings.minimumOpacityPercent)).padding(.top, 6)
                Slider(
                    value: Binding(
                        get: { Double(settings.minimumOpacityPercent) },
                        set: { settings.minimumOpacityPercent = Int($0.rounded()) }
                    ),
                    in: Double(AppSettings.opacityFloorPercent)...90,
                    step: 1
                ) { EmptyView() }
                Text(t("MinOpacityHint")).font(.caption).foregroundStyle(.secondary)
            }

            Card {
                HStack {
                    Text(t("Managed")).font(.headline)
                    Spacer()
                    Button(t("RestoreAll")) { service.restoreAll() }
                        .disabled(service.adjusted.isEmpty)
                }
                Text(t("ManagedHint")).font(.caption).foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
                if service.adjusted.isEmpty {
                    Text(t("NoManaged")).foregroundStyle(.secondary).padding(.vertical, 4)
                } else {
                    ScrollView {
                        VStack(spacing: 10) {
                            ForEach(service.adjusted) { row in
                                AdjustedRow(row: row, settings: settings, service: service, restoreTitle: t("RestoreOne"))
                            }
                        }
                    }
                    .frame(maxHeight: 220)
                }
            }

            HStack {
                Spacer()
                Button(t("Close"), action: close).keyboardShortcut(.cancelAction)
            }
        }
        .padding(18)
        .frame(width: 440)
        .environment(\.layoutDirection, Loc.isRightToLeft(settings.language) ? .rightToLeft : .leftToRight)
    }
}

private struct AdjustedRow: View {
    let row: OpacityService.Adjusted
    @ObservedObject var settings: AppSettings
    @ObservedObject var service: OpacityService
    let restoreTitle: String

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(row.name).lineLimit(1).truncationMode(.middle)
            HStack {
                Slider(
                    value: Binding(
                        get: { row.opacity * 100 },
                        set: { service.setOpacity($0.rounded() / 100, forTracked: row.id) }
                    ),
                    in: Double(settings.minimumOpacityPercent)...100,
                    step: 1
                ) { EmptyView() }
                Text("\(Int((row.opacity * 100).rounded()))%")
                    .monospacedDigit()
                    .frame(width: 40)
                Button(restoreTitle) { service.restore(row.id) }
            }
        }
    }
}
