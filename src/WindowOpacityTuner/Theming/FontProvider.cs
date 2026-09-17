using System.Drawing.Text;
using WindowOpacityTuner.Localization;

namespace WindowOpacityTuner.Theming;

/// <summary>
/// Hands out fonts in a family that can actually render the current language.
/// Segoe UI has no CJK glyphs, so Korean/Japanese/Chinese get their own family
/// and anything missing falls back to the system default.
///
/// Fonts are cached and never disposed. Two forms share this provider and a form
/// can outlive the language switch that replaced its fonts, so disposing on switch
/// would hand a live control a dead font handle. The cache is keyed by family,
/// size and style, which bounds it at a few dozen entries for the app's lifetime.
/// </summary>
public static class FontProvider
{
    private static readonly HashSet<string> Installed = LoadInstalledFamilies();

    private static readonly Dictionary<string, Font> Cache = new();

    public static Font Get(float size, FontStyle style = FontStyle.Regular)
    {
        string family = Resolve(Loc.CurrentInfo.FontFamily);
        string key = $"{family}|{size:0.##}|{(int)style}";

        if (Cache.TryGetValue(key, out Font cached))
        {
            return cached;
        }

        Font font;
        try
        {
            font = new Font(family, size, style, GraphicsUnit.Point);
        }
        catch
        {
            font = new Font(SystemFonts.MessageBoxFont.FontFamily, size, style, GraphicsUnit.Point);
        }

        Cache[key] = font;
        return font;
    }

    private static string Resolve(string preferred)
    {
        if (Installed.Contains(preferred))
        {
            return preferred;
        }

        foreach (string candidate in new[] { "Segoe UI", "Tahoma", "Arial" })
        {
            if (Installed.Contains(candidate))
            {
                return candidate;
            }
        }

        return SystemFonts.MessageBoxFont.FontFamily.Name;
    }

    private static HashSet<string> LoadInstalledFamilies()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var collection = new InstalledFontCollection();
            foreach (FontFamily family in collection.Families)
            {
                set.Add(family.Name);
            }
        }
        catch
        {
            // Enumeration can fail in odd session types; Resolve() then falls through to the system font.
        }

        return set;
    }
}
