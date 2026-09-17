namespace WindowOpacityTuner.UI.Controls;

public enum FlatButtonKind
{
    /// <summary>Solid accent fill — the primary action in a section.</summary>
    Accent,

    /// <summary>Quiet neutral fill — secondary actions.</summary>
    Subtle,
}

/// <summary>
/// Rounded flat button. Built on Control rather than Button because the stock
/// control cannot draw rounded corners over a themed background without seams.
/// </summary>
public sealed class FlatButton : Control
{
    private bool _hovering;
    private bool _pressed;

    public FlatButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.UserPaint
            | ControlStyles.Selectable
            | ControlStyles.SupportsTransparentBackColor,
            true);

        TabStop = true;
        Height = 40;
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    public FlatButtonKind Kind { get; set; } = FlatButtonKind.Accent;

    public int CornerRadius { get; set; } = 6;

    public Color NormalColor { get; set; } = Color.FromArgb(0x3C, 0x41, 0x4A);

    public Color HoverColor { get; set; } = Color.FromArgb(0x4C, 0x52, 0x5C);

    public Color PressedColor { get; set; } = Color.FromArgb(0x2C, 0x30, 0x37);

    public Color DisabledColor { get; set; } = Color.FromArgb(0xC2, 0xC6, 0xCC);

    /// <summary>
    /// Text colour while disabled. A disabled fill is pale, so the usual white label
    /// would wash out; left empty the old blend towards the fill is used instead.
    /// </summary>
    public Color DisabledForeColor { get; set; } = Color.Empty;

    /// <summary>Extra breathing room added around the measured text by <see cref="GetPreferredSize"/>.</summary>
    public Size TextPadding { get; set; } = new(24, 14);

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        GraphicsUtil.EnableQuality(g);

        using (var parentBrush = new SolidBrush(Parent?.BackColor ?? BackColor))
        {
            g.FillRectangle(parentBrush, ClientRectangle);
        }

        Color fill = !Enabled
            ? DisabledColor
            : _pressed ? PressedColor
            : _hovering ? HoverColor
            : NormalColor;

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = GraphicsUtil.RoundedRect(bounds, CornerRadius))
        using (var brush = new SolidBrush(fill))
        {
            g.FillPath(brush, path);

            if (Focused && Enabled)
            {
                using var pen = new Pen(Color.FromArgb(150, ForeColor), 1.5f);
                g.DrawPath(pen, GraphicsUtil.RoundedRect(Rectangle.Inflate(bounds, -3, -3), Math.Max(2, CornerRadius - 2)));
            }
        }

        TextFormatFlags flags = TextFormatFlags.HorizontalCenter
                                | TextFormatFlags.VerticalCenter
                                | TextFormatFlags.EndEllipsis
                                | TextFormatFlags.NoPrefix;

        if (RightToLeft == RightToLeft.Yes)
        {
            flags |= TextFormatFlags.RightToLeft;
        }

        Color textColor = Enabled
            ? ForeColor
            : DisabledForeColor.IsEmpty ? Blend(ForeColor, fill, 0.35f) : DisabledForeColor;

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor, flags);
    }

    private static Color Blend(Color color, Color towards, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(color.R + ((towards.R - color.R) * amount)),
            (int)(color.G + ((towards.G - color.G) * amount)),
            (int)(color.B + ((towards.B - color.B) * amount)));
    }

    /// <summary>
    /// Resizes to the current text, never narrower than <paramref name="minWidth"/>.
    ///
    /// Auto-sizing inside a TableLayoutPanel cell is positioned before the preferred
    /// size is known, so a button that grows when its caption changes ends up off
    /// centre. Sizing it explicitly keeps an Anchor.None cell centred.
    /// </summary>
    public void SizeToText(int minWidth, int height)
    {
        Size text = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
        var wanted = new Size(Math.Max(minWidth, text.Width + TextPadding.Width), height);

        if (Size != wanted)
        {
            Size = wanted;
            Parent?.PerformLayout();
        }
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        Size text = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix);
        return new Size(text.Width + TextPadding.Width, Math.Max(Height, text.Height + TextPadding.Height));
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovering = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovering = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        _pressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Space or Keys.Enter || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.Space or Keys.Enter)
        {
            _pressed = false;
            Invalidate();
            OnClick(EventArgs.Empty);
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }
}
