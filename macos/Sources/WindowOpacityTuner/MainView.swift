import SwiftUI

struct MainView: View {
    @ObservedObject var model: TunerModel
    @ObservedObject var settings: AppSettings
    @ObservedObject var service: OpacityService
    @State private var showSettings = false
    @FocusState private var sliderFocused: Bool

    private func t(_ key: String, _ args: CustomStringConvertible...) -> String {
        Loc.format(key, settings.language, args)
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            header

            if !model.hasPermission {
                permissionCard
            }

            Card {
                Text(t("SectionSelect")).font(.headline)
                Text(t("SelectedWindow", model.selected?.displayName ?? t("None")))
                    .foregroundStyle(.secondary)
                    .lineLimit(2)
                    .truncationMode(.middle)
                Button {
                    model.pick()
                } label: {
                    Label(t(model.selected == nil ? "PickWindow" : "PickAnother"), systemImage: "scope")
                        .frame(minWidth: 200)
                }
                .controlSize(.large)
                .buttonStyle(.borderedProminent)
                .tint(Color(nsColor: .darkGray))
                .disabled(model.picking)
            }

            Card {
                Text(t("SectionOpacity")).font(.headline)
                let p = Int(model.opacityPercent.rounded())
                Text(t(p >= 100 ? "OpacityPercentOpaque" : "OpacityPercent", p))
                    .monospacedDigit()
                Slider(value: $model.opacityPercent, in: model.minPercent...100, step: 1) {
                    EmptyView()
                }
                .focused($sliderFocused)
                .disabled(model.selected == nil)
                .onChange(of: model.opacityPercent) { _ in model.applyOpacity() }
            }

            HStack {
                Button(t("Reset")) { model.reset() }
                    .disabled(model.selected == nil)
                Spacer()
                Link("@efforthye", destination: URL(string: "https://github.com/efforthye")!)
                    .font(.footnote)
                    .foregroundStyle(.secondary)
            }

            if let status = model.status {
                Text(status)
                    .font(.callout)
                    .foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
            }
        }
        .padding(18)
        .frame(width: 440)
        .environment(\.layoutDirection, Loc.isRightToLeft(settings.language) ? .rightToLeft : .leftToRight)
        .onChange(of: model.focusSliderToken) { _ in sliderFocused = true }
        .onChange(of: service.adjusted) { _ in model.syncFromService() }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didBecomeActiveNotification)) { _ in
            model.refreshPermission()
        }
        .sheet(isPresented: $showSettings) {
            SettingsView(settings: settings, service: service) { showSettings = false }
        }
    }

    private var header: some View {
        HStack(spacing: 10) {
            Text(t("AppTitle")).font(.title2.bold())
            Spacer()
            Text("\(settings.selfOpacityPercent)%")
                .font(.caption)
                .monospacedDigit()
                .foregroundStyle(.secondary)
            Slider(
                value: Binding(
                    get: { Double(settings.selfOpacityPercent) },
                    set: { settings.selfOpacityPercent = Int($0.rounded()) }
                ),
                in: Double(AppSettings.selfOpacityFloorPercent)...100
            )
            .frame(width: 80)
            .help(t("SelfOpacity"))
            Button {
                settings.theme = settings.theme == .dark ? .light : .dark
            } label: {
                Image(systemName: settings.theme == .dark ? "sun.max" : "moon")
            }
            .buttonStyle(.borderless)
            .help(t("ToggleTheme"))
            Button {
                showSettings = true
            } label: {
                Image(systemName: "gearshape")
            }
            .buttonStyle(.borderless)
            .help(t("Settings"))
        }
    }

    private var permissionCard: some View {
        Card {
            Label(t("PermissionTitle"), systemImage: "exclamationmark.triangle")
                .font(.headline)
            Text(t("PermissionBody"))
                .font(.callout)
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
            Button(t("OpenPrivacySettings")) { model.requestPermission() }
        }
    }
}

struct Card<Content: View>: View {
    @ViewBuilder var content: Content

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            content
        }
        .frame(maxWidth: .infinity, alignment: .leading)
        .padding(14)
        .background(
            RoundedRectangle(cornerRadius: 10)
                .fill(Color(nsColor: .controlBackgroundColor))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 10)
                .strokeBorder(Color(nsColor: .separatorColor))
        )
    }
}
