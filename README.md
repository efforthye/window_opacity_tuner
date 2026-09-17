# Window Opacity Tuner

Adjust the opacity of any window with a slider. Point at a window, pick it, drag.

A small Windows utility built on WinForms (.NET 8). Light and dark themes, five UI
languages (English, Korean, Japanese, Chinese, Arabic with right-to-left layout), and
its own window can be made translucent from the control in the top-right corner.

## How it works

Picking a window highlights it with a green outline that follows the cursor. Releasing
over a window selects it and brings the tuner back to the front with the opacity slider
focused, so the arrow keys and the wheel work straight away. The tuner also stays above
other windows by default — *Keep this window in front of other windows* under
**Behavior** turns that off.

Opacity is
applied through `SetLayeredWindowAttributes`, which stores the value **on the target
window itself** — so a window stays translucent after this app exits.

That is deliberate, but it means the tuner is the only thing that remembers how to undo
it. Every window it has changed is listed under **Settings → Adjusted windows**, where
each one has its own slider and a Restore button. `Restore` puts a window back exactly as
it was found, including removing the `WS_EX_LAYERED` style if the window did not have it
to begin with. Settings also has a *Restore every window when this app closes* option,
off by default.

## Building

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
`dotnet publish -r win-x64` cross-compiles, so building from WSL works — no Windows host
needed.

```bash
./build.sh                # framework-dependent: ~1 MB exe, needs .NET 8 Desktop Runtime
./build.sh --standalone   # self-contained:     ~70 MB exe, no prerequisites
```

```powershell
.\build.ps1
.\build.ps1 -Standalone
```

The exe lands in `dist/`. Windows Forms cannot be trimmed, so a self-contained build
carries the whole runtime; if you are distributing to machines you control, the
framework-dependent build plus a one-time
[.NET Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) install is much
smaller.

## Installing and pinning to the taskbar

Windows will not pin a program that lives on a network path, and the WSL share
(`\\wsl.localhost\...`) counts as one — run the exe straight from `dist/` over that
share and the taskbar right-click menu offers nothing but **Close window**.

Copy it to a local folder first — from WSL:

```bash
./install.sh
```

or from PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

That puts the exe in `%LOCALAPPDATA%\Programs\WindowOpacityTuner` and adds a Start menu
shortcut; **Pin to taskbar** works from there. Copying the exe anywhere on `C:` by hand
does the same thing.

## Settings

Stored as JSON at `%APPDATA%\WindowOpacityTuner\settings.json`: language, theme, the
app's own opacity (down to 5%), window position, the lowest opacity the slider will allow
(3% by default, so a window can never be dimmed until you cannot find it), the
always-on-top switch (on by default), and the restore-on-exit switch. A settings file written by an older build keeps its own floor —
lower it under **Behavior** or delete the file to pick up the new default.

## Notes

- A window running with higher privileges than the tuner cannot be changed. Run the app
  as administrator to adjust those.
- The desktop and the taskbar are skipped by the picker on purpose.
- Some apps that draw their own layered windows (certain overlays and splash screens)
  manage alpha themselves and will reset whatever you set.

## Project layout

```
src/WindowOpacityTuner/
  Interop/NativeMethods.cs     win32 P/Invoke: layered windows, hit testing, DWM
  Core/OpacityService.cs       applies opacity, tracks what was changed, restores it
  Core/AppSettings.cs          JSON preferences
  Localization/Loc.cs          the five string tables
  Theming/                     light and dark palettes, per-language font selection
  UI/MainForm.cs               the main window
  UI/SettingsForm.cs           theme, language, behavior, adjusted-window list
  UI/HighlightForm.cs          the click-through picker outline
  UI/Controls/                 flat button, label, slider and card drawn for theme support
  app.ico                      window and exe icon
assets/make_icon.py            regenerates app.ico (needs Pillow)
install.sh, install.ps1        copy the exe somewhere Windows will let you pin it
```

## License

See [LICENSE](LICENSE).
