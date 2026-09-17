using System.Diagnostics;
using WindowOpacityTuner.Interop;

namespace WindowOpacityTuner.Core;

/// <summary>
/// Applies and tracks per-window opacity.
///
/// The value set by SetLayeredWindowAttributes lives on the target window itself,
/// not in this process, so a window stays translucent after this app exits. That is
/// intentional, but it also means we are the only thing that remembers how to undo
/// it — hence the tracked list.
/// </summary>
public sealed class OpacityService
{

    /// <summary>
    /// Percent to the 0-255 alpha the Win32 call takes. Clamped to at least 1: alpha 0
    /// is a fully invisible window that also stops receiving clicks.
    /// </summary>
    public static int AlphaForPercent(int percent) =>
        Math.Clamp((int)Math.Round(percent * 255.0 / 100.0), 1, 255);

    private readonly Dictionary<IntPtr, WindowEntry> _tracked = new();

    /// <summary>Raised whenever an entry is added, changed or removed.</summary>
    public event EventHandler TrackedChanged;

    public IReadOnlyCollection<WindowEntry> Tracked
    {
        get
        {
            PruneDeadWindows();
            return _tracked.Values.ToList();
        }
    }

    /// <summary>
    /// How many windows are currently modified. Unlike <see cref="Tracked"/> this does
    /// not prune, so it is safe to call from a <see cref="TrackedChanged"/> handler.
    /// </summary>
    public int TrackedCount => _tracked.Count;

    public bool IsTracked(IntPtr handle) => _tracked.ContainsKey(handle);

    public WindowEntry Get(IntPtr handle) => _tracked.TryGetValue(handle, out WindowEntry entry) ? entry : null;

    /// <summary>
    /// Reads the alpha a window currently has. Non-layered windows are fully opaque.
    /// </summary>
    public static byte ReadAlpha(IntPtr handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return 255;
        }

        long exStyle = NativeMethods.GetWindowLongEx(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        if ((exStyle & NativeMethods.WS_EX_LAYERED) == 0)
        {
            return 255;
        }

        // A layered window created with UpdateLayeredWindow has no alpha key to read;
        // the call fails and we treat it as opaque.
        return NativeMethods.GetLayeredWindowAttributes(handle, out _, out byte alpha, out uint flags)
               && (flags & NativeMethods.LWA_ALPHA) != 0
            ? alpha
            : (byte)255;
    }

    public static bool HasLayeredStyle(IntPtr handle)
    {
        long exStyle = NativeMethods.GetWindowLongEx(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        return (exStyle & NativeMethods.WS_EX_LAYERED) != 0;
    }

    /// <summary>
    /// Starts tracking a window (capturing its original state) without changing it yet.
    /// </summary>
    public WindowEntry Track(IntPtr handle)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            return null;
        }

        if (_tracked.TryGetValue(handle, out WindowEntry existing))
        {
            existing.Title = NativeMethods.GetWindowTitle(handle);
            return existing;
        }

        var entry = new WindowEntry(
            handle,
            NativeMethods.GetWindowTitle(handle),
            GetProcessName(handle),
            HasLayeredStyle(handle),
            ReadAlpha(handle));

        _tracked[handle] = entry;
        OnTrackedChanged();
        return entry;
    }

    /// <summary>Applies an alpha value (0-255) to a window and remembers it.</summary>
    public bool Apply(IntPtr handle, byte alpha)
    {
        if (!NativeMethods.IsWindow(handle))
        {
            Forget(handle);
            return false;
        }

        WindowEntry entry = Track(handle);
        if (entry == null)
        {
            return false;
        }

        // Dragging back to where the window started is the same as restoring it:
        // put the original style back too rather than leaving it needlessly layered.
        if (alpha == entry.OriginalAlpha)
        {
            return Restore(handle);
        }

        if (!EnsureLayered(handle))
        {
            return false;
        }

        if (!NativeMethods.SetLayeredWindowAttributes(handle, 0, alpha, NativeMethods.LWA_ALPHA))
        {
            return false;
        }

        entry.CurrentAlpha = alpha;
        OnTrackedChanged();
        return true;
    }

    /// <summary>Puts a single window back the way we found it and stops tracking it.</summary>
    public bool Restore(IntPtr handle)
    {
        if (!_tracked.TryGetValue(handle, out WindowEntry entry))
        {
            // Not tracked: the most useful interpretation of "reset" is full opacity.
            return NativeMethods.IsWindow(handle)
                   && EnsureLayered(handle)
                   && NativeMethods.SetLayeredWindowAttributes(handle, 0, 255, NativeMethods.LWA_ALPHA);
        }

        bool ok = true;
        if (NativeMethods.IsWindow(handle))
        {
            ok = NativeMethods.SetLayeredWindowAttributes(handle, 0, entry.OriginalAlpha, NativeMethods.LWA_ALPHA);

            // If the window was not layered before we touched it, take the style back off
            // so it renders exactly as it did originally.
            if (!entry.HadLayeredStyle)
            {
                RemoveLayered(handle);
            }
        }

        _tracked.Remove(handle);
        OnTrackedChanged();
        return ok;
    }

    /// <summary>Restores every window we have modified. Used by the "restore on exit" option.</summary>
    public int RestoreAll()
    {
        int count = 0;
        foreach (IntPtr handle in _tracked.Keys.ToList())
        {
            if (Restore(handle))
            {
                count++;
            }
        }

        return count;
    }

    public void Forget(IntPtr handle)
    {
        if (_tracked.Remove(handle))
        {
            OnTrackedChanged();
        }
    }

    /// <summary>Drops entries whose windows have since been closed.</summary>
    public void PruneDeadWindows()
    {
        List<IntPtr> dead = _tracked.Keys.Where(h => !NativeMethods.IsWindow(h)).ToList();
        if (dead.Count == 0)
        {
            return;
        }

        foreach (IntPtr handle in dead)
        {
            _tracked.Remove(handle);
        }

        OnTrackedChanged();
    }

    /// <summary>Refreshes cached titles; windows get renamed as the user navigates.</summary>
    public void RefreshTitles()
    {
        foreach (WindowEntry entry in _tracked.Values)
        {
            if (NativeMethods.IsWindow(entry.Handle))
            {
                entry.Title = NativeMethods.GetWindowTitle(entry.Handle);
            }
        }
    }

    private static bool EnsureLayered(IntPtr handle)
    {
        long exStyle = NativeMethods.GetWindowLongEx(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        if ((exStyle & NativeMethods.WS_EX_LAYERED) != 0)
        {
            return true;
        }

        NativeMethods.SetWindowLongEx(
            handle,
            NativeMethods.GWL_EXSTYLE,
            new IntPtr(exStyle | NativeMethods.WS_EX_LAYERED));

        long updated = NativeMethods.GetWindowLongEx(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        return (updated & NativeMethods.WS_EX_LAYERED) != 0;
    }

    private static void RemoveLayered(IntPtr handle)
    {
        long exStyle = NativeMethods.GetWindowLongEx(handle, NativeMethods.GWL_EXSTYLE).ToInt64();
        NativeMethods.SetWindowLongEx(
            handle,
            NativeMethods.GWL_EXSTYLE,
            new IntPtr(exStyle & ~NativeMethods.WS_EX_LAYERED));
    }

    internal static string GetProcessName(IntPtr handle)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(handle, out uint pid);
            if (pid == 0)
            {
                return string.Empty;
            }

            using Process process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void OnTrackedChanged() => TrackedChanged?.Invoke(this, EventArgs.Empty);
}
