using System.Drawing.Drawing2D;

namespace WindowOpacityTuner.UI.Controls;

/// <summary>
/// The rounded surface each section sits on. Painting the parent background first
/// keeps the corners genuinely round instead of showing grey rectangles behind them.
/// </summary>
public sealed class CardPanel : Panel
{
    private Color _borderColor = Color.Transparent;
    private int _cornerRadius = 10;

    public CardPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint
            | ControlStyles.SupportsTransparentBackColor,
            true);

        Padding = new Padding(18, 16, 18, 18);
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

    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = Math.Max(0, value);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        GraphicsUtil.EnableQuality(g);

        // Fill with the parent's colour so the area outside the rounded corners blends in.
        using (var parentBrush = new SolidBrush(Parent?.BackColor ?? BackColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using GraphicsPath path = GraphicsUtil.RoundedRect(bounds, _cornerRadius);

        using (var fill = new SolidBrush(BackColor))
        {
            g.FillPath(fill, path);
        }

        if (_borderColor != Color.Transparent)
        {
            using var pen = new Pen(_borderColor, 1f);
            g.DrawPath(pen, path);
        }

        base.OnPaint(e);
    }
}
