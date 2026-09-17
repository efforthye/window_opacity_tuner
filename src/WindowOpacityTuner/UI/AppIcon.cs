namespace WindowOpacityTuner.UI;

/// <summary>
/// The window icon, read once from the app.ico embedded in the exe. Loading the
/// multi-size .ico rather than Icon.ExtractAssociatedIcon keeps the taskbar and
/// alt-tab renditions crisp instead of upscaling a 32px frame.
/// </summary>
internal static class AppIcon
{
    private static readonly Lazy<Icon> Instance = new(Load);

    /// <summary>The shared icon, or null when the resource cannot be read (the form then keeps the stock icon).</summary>
    public static Icon Shared => Instance.Value;

    private static Icon Load()
    {
        try
        {
            using Stream stream = typeof(AppIcon).Assembly.GetManifestResourceStream("WindowOpacityTuner.app.ico");
            return stream is null ? null : new Icon(stream);
        }
        catch
        {
            return null;
        }
    }
}
