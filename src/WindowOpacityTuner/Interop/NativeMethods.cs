using System.Runtime.InteropServices;
using System.Text;

namespace WindowOpacityTuner.Interop;

/// <summary>
/// Thin P/Invoke layer. Everything this app does to other people's windows
/// goes through here, so the rest of the code can stay in plain C#.
/// </summary>
internal static class NativeMethods
{
    // ---- Window styles -------------------------------------------------
    public const int GWL_EXSTYLE = -20;

    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public const int WS_EX_APPWINDOW = 0x00040000;

    public const uint LWA_ALPHA = 0x00000002;

    public const uint GA_ROOT = 2;
    public const uint GA_ROOTOWNER = 3;

    // ---- SetWindowPos --------------------------------------------------
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);

    // ---- ShowWindow ----------------------------------------------------
    public const int SW_RESTORE = 9;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    // ---- DWM -----------------------------------------------------------
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    public const int DWMWA_CLOAKED = 14;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;
        public readonly int Height => Bottom - Top;
        public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    // ---- Layered window opacity ---------------------------------------

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetLayeredWindowAttributes(IntPtr hwnd, out uint pcrKey, out byte pbAlpha, out uint pdwFlags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    /// <summary>Pointer-width safe GetWindowLong.</summary>
    public static IntPtr GetWindowLongEx(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));

    /// <summary>Pointer-width safe SetWindowLong.</summary>
    public static IntPtr SetWindowLongEx(IntPtr hWnd, int nIndex, IntPtr dwNewLong) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));

    // ---- Window discovery ----------------------------------------------

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    public static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLengthW(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // ---- DWM -----------------------------------------------------------

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out RECT value, int attrSize);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attr, out int value, int attrSize);

    // ---- Convenience helpers -------------------------------------------

    public static string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLengthW(hWnd);
        if (length <= 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(length + 1);
        GetWindowTextW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static string GetWindowClass(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        GetClassNameW(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    /// <summary>True when the window is a hidden UWP shadow window that should not be listed.</summary>
    public static bool IsCloaked(IntPtr hWnd)
    {
        if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, sizeof(int)) != 0)
        {
            return false;
        }

        return cloaked != 0;
    }

    /// <summary>
    /// Visible bounds of a window. Uses the DWM frame bounds when available so the
    /// picker highlight hugs the window instead of the invisible resize border.
    /// </summary>
    public static Rectangle GetVisibleBounds(IntPtr hWnd)
    {
        if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT frame, Marshal.SizeOf<RECT>()) == 0
            && frame.Width > 0 && frame.Height > 0)
        {
            return frame.ToRectangle();
        }

        return GetWindowRect(hWnd, out RECT raw) ? raw.ToRectangle() : Rectangle.Empty;
    }

    /// <summary>
    /// Pulls one of our own windows back to the front, un-minimizing it first.
    /// SetForegroundWindow alone is not enough: it is refused outright when the
    /// foreground belongs to another app, and it does nothing about z-order when we
    /// already hold the foreground but sit under a window from the topmost band. So
    /// the raise always happens too, and <paramref name="keepTopmost"/> says which
    /// band to leave the window in afterwards.
    /// </summary>
    public static void ForceForeground(IntPtr hWnd, bool keepTopmost)
    {
        if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
        {
            return;
        }

        if (IsIconic(hWnd))
        {
            ShowWindow(hWnd, SW_RESTORE);
        }

        SetForegroundWindow(hWnd);

        const uint flags = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE;
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, flags);

        if (!keepTopmost)
        {
            // A trip through the topmost band raises the window above everything,
            // including other apps' always-on-top windows; dropping straight back out
            // of the band keeps it there until something else is activated.
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, flags);
        }
    }

    /// <summary>Paints the title bar of our own form dark on Windows 10 1809+.</summary>
    public static void SetImmersiveDarkMode(IntPtr hWnd, bool enabled)
    {
        int value = enabled ? 1 : 0;
        if (DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref value, sizeof(int));
        }
    }
}
