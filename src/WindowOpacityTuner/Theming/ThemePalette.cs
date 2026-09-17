using WindowOpacityTuner.Core;

namespace WindowOpacityTuner.Theming;

/// <summary>
/// Flat colour set for one theme. The accent is a neutral graphite rather than a
/// brand blue — it is the only place to change if a different button colour is wanted.
/// Two instances exist (light and dark) and every
/// control reads its colours from the active one, so switching theme is a repaint
/// rather than a rebuild.
/// </summary>
public sealed class ThemePalette
{
    public static readonly ThemePalette Light = new()
    {
        Mode = ThemeMode.Light,
        FormBackground = Color.FromArgb(0xEF, 0xF0, 0xF2),
        CardBackground = Color.FromArgb(0xFA, 0xFA, 0xFB),
        CardBorder = Color.FromArgb(0xE2, 0xE4, 0xE8),
        TextPrimary = Color.FromArgb(0x1B, 0x1D, 0x21),
        TextSecondary = Color.FromArgb(0x6B, 0x70, 0x7A),
        TextOnAccent = Color.White,
        Accent = Color.FromArgb(0x3C, 0x41, 0x4A),
        AccentHover = Color.FromArgb(0x4C, 0x52, 0x5C),
        AccentPressed = Color.FromArgb(0x2C, 0x30, 0x37),
        AccentDisabled = Color.FromArgb(0xC2, 0xC6, 0xCC),
        SliderTrack = Color.FromArgb(0xC9, 0xCD, 0xD4),
        SliderThumb = Color.FromArgb(0x3C, 0x41, 0x4A),
        SubtleButton = Color.FromArgb(0xE6, 0xE8, 0xEC),
        SubtleButtonHover = Color.FromArgb(0xDA, 0xDD, 0xE3),
        Danger = Color.FromArgb(0xC0, 0x39, 0x3B),
        Highlight = Color.FromArgb(0x2E, 0xE6, 0x5C),
    };

    public static readonly ThemePalette Dark = new()
    {
        Mode = ThemeMode.Dark,
        FormBackground = Color.FromArgb(0x17, 0x19, 0x1C),
        CardBackground = Color.FromArgb(0x22, 0x25, 0x2A),
        CardBorder = Color.FromArgb(0x33, 0x37, 0x3E),
        TextPrimary = Color.FromArgb(0xEC, 0xEE, 0xF1),
        TextSecondary = Color.FromArgb(0x9A, 0xA1, 0xAC),
        TextOnAccent = Color.White,
        Accent = Color.FromArgb(0x50, 0x57, 0x62),
        AccentHover = Color.FromArgb(0x61, 0x69, 0x75),
        AccentPressed = Color.FromArgb(0x41, 0x47, 0x51),
        AccentDisabled = Color.FromArgb(0x32, 0x36, 0x3C),
        SliderTrack = Color.FromArgb(0x3C, 0x41, 0x49),
        SliderThumb = Color.FromArgb(0x8A, 0x93, 0xA1),
        SubtleButton = Color.FromArgb(0x2E, 0x32, 0x39),
        SubtleButtonHover = Color.FromArgb(0x3A, 0x3F, 0x47),
        Danger = Color.FromArgb(0xE0, 0x5C, 0x5C),
        Highlight = Color.FromArgb(0x2E, 0xE6, 0x5C),
    };

    public ThemeMode Mode { get; private init; }

    public Color FormBackground { get; private init; }

    public Color CardBackground { get; private init; }

    public Color CardBorder { get; private init; }

    public Color TextPrimary { get; private init; }

    public Color TextSecondary { get; private init; }

    public Color TextOnAccent { get; private init; }

    public Color Accent { get; private init; }

    public Color AccentHover { get; private init; }

    public Color AccentPressed { get; private init; }

    public Color AccentDisabled { get; private init; }

    public Color SliderTrack { get; private init; }

    public Color SliderThumb { get; private init; }

    public Color SubtleButton { get; private init; }

    public Color SubtleButtonHover { get; private init; }

    public Color Danger { get; private init; }

    /// <summary>Colour of the picker outline drawn over the window under the cursor.</summary>
    public Color Highlight { get; private init; }

    public bool IsDark => Mode == ThemeMode.Dark;

    public static ThemePalette For(ThemeMode mode) => mode == ThemeMode.Dark ? Dark : Light;
}
