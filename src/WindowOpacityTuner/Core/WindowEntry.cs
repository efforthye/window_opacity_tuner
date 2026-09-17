namespace WindowOpacityTuner.Core;

/// <summary>
/// One window this app has touched. We remember what the window looked like
/// before we changed it so "restore" puts it back exactly as it was, rather
/// than just forcing alpha to 255.
/// </summary>
public sealed class WindowEntry
{
    public WindowEntry(IntPtr handle, string title, string processName, bool hadLayeredStyle, byte originalAlpha)
    {
        Handle = handle;
        Title = title;
        ProcessName = processName;
        HadLayeredStyle = hadLayeredStyle;
        OriginalAlpha = originalAlpha;
        CurrentAlpha = originalAlpha;
    }

    public IntPtr Handle { get; }

    public string Title { get; set; }

    public string ProcessName { get; }

    /// <summary>True when the window was already layered before we got to it.</summary>
    public bool HadLayeredStyle { get; }

    public byte OriginalAlpha { get; }

    public byte CurrentAlpha { get; set; }

    public int Percent => (int)Math.Round(CurrentAlpha / 255.0 * 100.0);

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Title)
            ? (string.IsNullOrWhiteSpace(ProcessName) ? "(untitled)" : ProcessName)
            : Title;
}
