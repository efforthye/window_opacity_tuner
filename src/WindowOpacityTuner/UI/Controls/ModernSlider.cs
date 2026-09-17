namespace WindowOpacityTuner.UI.Controls;

/// <summary>
/// A flat slider. The stock TrackBar cannot be recoloured, which makes a dark
/// theme impossible, so this draws its own track and thumb.
/// </summary>
public sealed class ModernSlider : Control
{
    private int _minimum;
    private int _maximum = 255;
    private int _value;
    private bool _dragging;
    private bool _hovering;

    public ModernSlider()
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
        Height = 28;
        BackColor = Color.Transparent;
    }

    public event EventHandler ValueChanged;

    /// <summary>Fires only when the user lets go, for work too expensive to do per pixel.</summary>
    public event EventHandler ValueCommitted;

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            Value = _value;
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            Value = _value;
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, _minimum, _maximum);
            if (clamped == _value)
            {
                return;
            }

            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Sets the value without raising <see cref="ValueChanged"/>, for programmatic sync.</summary>
    public void SetValueSilently(int value)
    {
        _value = Math.Clamp(value, _minimum, _maximum);
        Invalidate();
    }

    public int SmallChange { get; set; } = 1;

    public int LargeChange { get; set; } = 10;

    public int TrackThickness { get; set; } = 6;

    public int ThumbRadius { get; set; } = 9;

    public Color TrackColor { get; set; } = Color.FromArgb(0xC9, 0xCD, 0xD4);

    public Color FillColor { get; set; } = Color.FromArgb(0x1E, 0x7C, 0xE0);

    public Color ThumbColor { get; set; } = Color.FromArgb(0x1E, 0x7C, 0xE0);

    /// <summary>Draws right-to-left, so the control reads naturally in Arabic.</summary>
    public bool Mirrored { get; set; }

    private Rectangle TrackBounds
    {
        get
        {
            int pad = ThumbRadius + 1;
            int y = (Height - TrackThickness) / 2;
            return new Rectangle(pad, y, Math.Max(1, Width - (pad * 2)), TrackThickness);
        }
    }

    private float Ratio => _maximum == _minimum ? 0f : (_value - _minimum) / (float)(_maximum - _minimum);

    private int ThumbCenterX
    {
        get
        {
            Rectangle track = TrackBounds;
            float r = Mirrored ? 1f - Ratio : Ratio;
            return track.Left + (int)Math.Round(track.Width * r);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        GraphicsUtil.EnableQuality(g);

        Rectangle track = TrackBounds;
        int radius = TrackThickness / 2;

        using (var path = GraphicsUtil.RoundedRect(track, radius))
        using (var brush = new SolidBrush(Enabled ? TrackColor : Blend(TrackColor, BackColorSafe(), 0.4f)))
        {
            g.FillPath(brush, path);
        }

        int thumbX = ThumbCenterX;

        Rectangle filled = Mirrored
            ? Rectangle.FromLTRB(thumbX, track.Top, track.Right, track.Bottom)
            : Rectangle.FromLTRB(track.Left, track.Top, thumbX, track.Bottom);

        if (filled.Width > 0)
        {
            using var fillPath = GraphicsUtil.RoundedRect(filled, radius);
            using var fillBrush = new SolidBrush(Enabled ? FillColor : Blend(FillColor, BackColorSafe(), 0.55f));
            g.FillPath(fillBrush, fillPath);
        }

        int r2 = _dragging || _hovering ? ThumbRadius + 1 : ThumbRadius;
        var thumb = new Rectangle(thumbX - r2, (Height / 2) - r2, r2 * 2, r2 * 2);

        using (var thumbBrush = new SolidBrush(Enabled ? ThumbColor : Blend(ThumbColor, BackColorSafe(), 0.55f)))
        {
            g.FillEllipse(thumbBrush, thumb);
        }

        if (Focused && Enabled)
        {
            using var focusPen = new Pen(Color.FromArgb(120, Color.White), 2f);
            g.DrawEllipse(focusPen, Rectangle.Inflate(thumb, -3, -3));
        }
    }

    private Color BackColorSafe() => Parent?.BackColor ?? Color.Gray;

    private static Color Blend(Color color, Color towards, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(color.R + ((towards.R - color.R) * amount)),
            (int)(color.G + ((towards.G - color.G) * amount)),
            (int)(color.B + ((towards.B - color.B) * amount)));
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        _dragging = true;
        Capture = true;
        SetValueFromPoint(e.X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
        {
            SetValueFromPoint(e.X);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Capture = false;
        Invalidate();
        ValueCommitted?.Invoke(this, EventArgs.Empty);
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
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!Enabled)
        {
            return;
        }

        Value += Math.Sign(e.Delta) * SmallChange;
        ValueCommitted?.Invoke(this, EventArgs.Empty);
    }

    protected override bool IsInputKey(Keys keyData) => keyData switch
    {
        Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown => true,
        _ => base.IsInputKey(keyData),
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled)
        {
            return;
        }

        int step = Mirrored ? -SmallChange : SmallChange;
        int page = Mirrored ? -LargeChange : LargeChange;

        switch (e.KeyCode)
        {
            case Keys.Left:
            case Keys.Down:
                Value -= step;
                break;
            case Keys.Right:
            case Keys.Up:
                Value += step;
                break;
            case Keys.PageDown:
                Value -= page;
                break;
            case Keys.PageUp:
                Value += page;
                break;
            case Keys.Home:
                Value = Mirrored ? Maximum : Minimum;
                break;
            case Keys.End:
                Value = Mirrored ? Minimum : Maximum;
                break;
            default:
                return;
        }

        e.Handled = true;
        ValueCommitted?.Invoke(this, EventArgs.Empty);
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

    private void SetValueFromPoint(int x)
    {
        Rectangle track = TrackBounds;
        float ratio = track.Width <= 0 ? 0f : (x - track.Left) / (float)track.Width;
        ratio = Math.Clamp(ratio, 0f, 1f);

        if (Mirrored)
        {
            ratio = 1f - ratio;
        }

        Value = _minimum + (int)Math.Round(ratio * (_maximum - _minimum));
    }
}
