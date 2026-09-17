using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowOpacityTuner.Core;

public enum ThemeMode
{
    Light,
    Dark,
}

/// <summary>
/// User preferences, stored as JSON under %APPDATA%\WindowOpacityTuner\settings.json.
/// Saving never throws: a broken or read-only profile directory degrades to defaults
/// rather than taking the app down.
/// </summary>
public sealed class AppSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>UI language tag. English is the default for a first run.</summary>
    public string Language { get; set; } = "en";

    public ThemeMode Theme { get; set; } = ThemeMode.Light;

    /// <summary>Opacity of this app's own window, 5-100 percent.</summary>
    public int SelfOpacityPercent { get; set; } = 100;

    /// <summary>
    /// Keeps the tuner above other windows. On by default: the window you just picked
    /// would otherwise cover the slider you are trying to drag.
    /// </summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>When true, every window we dimmed is put back before the app exits.</summary>
    public bool RestoreAllOnExit { get; set; }

    /// <summary>Lowest opacity the slider will allow, so a window cannot be lost entirely.</summary>
    public int MinimumOpacityPercent { get; set; } = OpacityFloorPercent;

    /// <summary>The hard floor the "lowest opacity allowed" setting itself can be dragged to.</summary>
    public const int OpacityFloorPercent = 3;

    /// <summary>The hard floor for this app's own window.</summary>
    public const int SelfOpacityFloorPercent = 5;

    public bool RememberWindowPosition { get; set; } = true;

    public int WindowLeft { get; set; } = -1;

    public int WindowTop { get; set; } = -1;

    [JsonIgnore]
    public static string SettingsDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowOpacityTuner");

    [JsonIgnore]
    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            AppSettings loaded = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
            return loaded is null ? new AppSettings() : loaded.Normalized();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch
        {
            // Preferences are a convenience; failing to persist them is not worth an error dialog.
        }
    }

    private AppSettings Normalized()
    {
        SelfOpacityPercent = Math.Clamp(SelfOpacityPercent, SelfOpacityFloorPercent, 100);
        MinimumOpacityPercent = Math.Clamp(MinimumOpacityPercent, OpacityFloorPercent, 90);

        if (string.IsNullOrWhiteSpace(Language))
        {
            Language = "en";
        }

        return this;
    }
}
