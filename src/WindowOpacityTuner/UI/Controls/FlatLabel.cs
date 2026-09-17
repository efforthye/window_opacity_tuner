namespace WindowOpacityTuner.UI.Controls;

/// <summary>
/// A double-buffered label. The stock Label erases its background before redrawing
/// the text, which makes the live percentage readouts strobe while a slider is
/// dragged; buffering the paint removes the flash.
///
/// <see cref="Label.AutoEllipsis"/> picks the behaviour: on, the text stays on one
/// line and is cut with an ellipsis; off, it wraps and the wrapped block is placed
/// according to <see cref="Label.TextAlign"/> (DrawText ignores vertical centring
/// once wrapping is on, so the rectangle is positioned by hand).
/// </summary>
public sealed class FlatLabel : Label
{
    public FlatLabel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint,
            true);
    }

    /// <summary>Assigns text only when it really changed, so a repaint costs nothing when it would not.</summary>
    public void SetText(string text)
    {
        if (Text != text)
        {
            Text = text;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(BackColor);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        TextFormatFlags flags = TextFormatFlags.NoPrefix | HorizontalFlag(TextAlign);

        if (RightToLeft == RightToLeft.Yes)
        {
            flags |= TextFormatFlags.RightToLeft;
        }

        Rectangle bounds = ClientRectangle;

        if (AutoEllipsis)
        {
            flags |= TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | VerticalFlag(TextAlign);
        }
        else
        {
            flags |= TextFormatFlags.WordBreak;
            Size needed = TextRenderer.MeasureText(g, Text, Font, new Size(bounds.Width, int.MaxValue), flags);
            bounds = PlaceVertically(bounds, needed.Height, TextAlign);
        }

        TextRenderer.DrawText(g, Text, Font, bounds, ForeColor, flags);
    }

    private static Rectangle PlaceVertically(Rectangle bounds, int textHeight, ContentAlignment align)
    {
        int slack = Math.Max(0, bounds.Height - textHeight);

        int top = align switch
        {
            ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => bounds.Top,
            ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => bounds.Top + slack,
            _ => bounds.Top + (slack / 2),
        };

        return new Rectangle(bounds.Left, top, bounds.Width, Math.Max(textHeight, bounds.Height - (top - bounds.Top)));
    }

    private static TextFormatFlags HorizontalFlag(ContentAlignment align) => align switch
    {
        ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter
            => TextFormatFlags.HorizontalCenter,
        ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight
            => TextFormatFlags.Right,
        _ => TextFormatFlags.Left,
    };

    private static TextFormatFlags VerticalFlag(ContentAlignment align) => align switch
    {
        ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight
            => TextFormatFlags.Top,
        ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight
            => TextFormatFlags.Bottom,
        _ => TextFormatFlags.VerticalCenter,
    };
}
