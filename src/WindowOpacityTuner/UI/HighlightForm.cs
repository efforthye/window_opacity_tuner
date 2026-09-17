using WindowOpacityTuner.Interop;

namespace WindowOpacityTuner.UI;

/// <summary>
/// The green outline drawn around the window under the cursor while picking.
///
/// It is click-through (WS_EX_TRANSPARENT) so it never steals the click and never
/// shows up as the result of WindowFromPoint, and it never activates, so the window
/// the user is pointing at keeps its own focus appearance.
/// </summary>
public sealed class HighlightForm : Form
{
    private const int BorderThickness = 4;

    private Color _borderColor = Color.FromArgb(0x2E, 0xE6, 0x5C);

    public HighlightForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Enabled = false;

        // Black is punched out, leaving only the coloured border painted below.
        BackColor = Color.Black;
        TransparencyKey = Color.Black;

        DoubleBuffered = true;
        Bounds = new Rectangle(-10000, -10000, 1, 1);
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                          | NativeMethods.WS_EX_TRANSPARENT
                          | NativeMethods.WS_EX_TOOLWINDOW
                          | NativeMethods.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    /// <summary>Draws the outline over a screen rectangle, or hides it when empty.</summary>
    public void SurroundScreenRect(Rectangle screenRect)
    {
        if (screenRect.Width <= 0 || screenRect.Height <= 0)
        {
            HideOutline();
            return;
        }

        // Inside the window rather than around it. A maximized window already fills its
        // monitor, so an outline drawn outside its edges lands off-screen and all the
        // user sees is whatever slice happens to overlap the desktop.
        Rectangle target = Rectangle.Intersect(screenRect, SystemInformation.VirtualScreen);

        if (target.Width <= 0 || target.Height <= 0)
        {
            HideOutline();
            return;
        }

        if (!Visible)
        {
            Show();
        }

        if (Bounds != target)
        {
            Bounds = target;
        }

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

        Invalidate();
    }

    public void HideOutline()
    {
        if (Visible)
        {
            Hide();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var pen = new Pen(_borderColor, BorderThickness)
        {
            Alignment = System.Drawing.Drawing2D.PenAlignment.Inset,
        };

        e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
    }
}
