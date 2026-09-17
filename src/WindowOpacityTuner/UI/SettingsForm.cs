using WindowOpacityTuner.Core;
using WindowOpacityTuner.Interop;
using WindowOpacityTuner.Localization;
using WindowOpacityTuner.Theming;
using WindowOpacityTuner.UI.Controls;

namespace WindowOpacityTuner.UI;

/// <summary>
/// Everything that is not the one thing the app is for: theme, language, the
/// safety behaviour on exit, and the list of windows already made translucent.
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly OpacityService _service;

    private ThemePalette _palette;

    private TableLayoutPanel _outer;
    private Panel _scroll;
    private Panel _footer;
    private TableLayoutPanel _root;

    private CardPanel _cardAppearance;
    private FlatLabel _lblAppearance;
    private FlatLabel _lblTheme;
    private FlatButton _btnLight;
    private FlatButton _btnDark;
    private FlatLabel _lblLanguage;
    private ComboBox _cmbLanguage;

    private CardPanel _cardBehavior;
    private FlatLabel _lblBehavior;
    private CheckBox _chkRestoreOnExit;
    private FlatLabel _lblRestoreHint;
    private FlatLabel _lblMinOpacity;
    private ModernSlider _minOpacitySlider;
    private FlatLabel _lblMinHint;

    private CardPanel _cardManaged;
    private TableLayoutPanel _managedInner;
    private int _managedListRow;
    private FlatLabel _lblManaged;
    private FlatLabel _lblManagedHint;
    private Panel _managedList;
    private FlatButton _btnRestoreAll;

    private FlatButton _btnClose;

    private bool _suppressLanguageEvent;

    private const int ManagedRowHeight = 34;
    private const int ManagedListMaxHeight = 170;

    public SettingsForm(AppSettings settings, OpacityService service)
    {
        _settings = settings;
        _service = service;
        _palette = ThemePalette.For(settings.Theme);

        BuildUi();
        ApplyLanguage();
        ApplyPalette();
        RebuildManagedList();
        FitToContent();
    }

    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    private void BuildUi()
    {
        SuspendLayout();

        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Icon = AppIcon.Shared;

        // Matches the tuner's own translucency, so opening settings does not flash a
        // fully opaque window over everything.
        Opacity = Math.Clamp(_settings.SelfOpacityPercent, AppSettings.SelfOpacityFloorPercent, 100) / 100.0;

        // Two rows: the cards scroll, Close does not. Putting Close inside the
        // scrolling area is what pushed it off the bottom edge of the dialog.
        _outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0),
        };
        _outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        _outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(18, 16, 18, 0),
            Margin = new Padding(0),
        };

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        BuildAppearanceCard();
        BuildBehaviorCard();
        BuildManagedCard();

        _footer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 8, 18, 16),
            Margin = new Padding(0),
        };

        _btnClose = new FlatButton
        {
            Kind = FlatButtonKind.Accent,
            Dock = DockStyle.Top,
            Height = 40,
            Margin = new Padding(0),
        };
        _btnClose.Click += (_, _) => Close();
        _footer.Controls.Add(_btnClose);

        LayoutHelper.AddRow(_root, _cardAppearance);
        LayoutHelper.AddRow(_root, _cardBehavior);
        LayoutHelper.AddRow(_root, _cardManaged);

        _scroll.Controls.Add(_root);
        _outer.Controls.Add(_scroll, 0, 0);
        _outer.Controls.Add(_footer, 0, 1);

        Controls.Add(_outer);
        AcceptButton = null;
        ClientSize = new Size(470, 580);
        ResumeLayout(true);
    }

    /// <summary>
    /// Shrinks the dialog to whatever the cards actually need, capped to the screen so
    /// it never opens taller than the desktop. Anything left over still scrolls.
    /// </summary>
    private void FitToContent()
    {
        int content = _root.PreferredSize.Height
                      + _scroll.Padding.Vertical
                      + _btnClose.Height + _footer.Padding.Vertical
                      + 4;

        Rectangle workingArea = (Screen.FromControl(this) ?? Screen.PrimaryScreen).WorkingArea;
        int max = Math.Max(320, workingArea.Height - 120);

        ClientSize = new Size(ClientSize.Width, Math.Min(content, max));
    }

    private void BuildAppearanceCard()
    {
        _cardAppearance = NewCard();
        TableLayoutPanel inner = LayoutHelper.NewColumn(new Padding(0));

        _lblAppearance = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);

        var themeRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 6, 0, 4),
        };
        themeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        themeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88f));
        themeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88f));

        _lblTheme = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);
        _lblTheme.Height = 34;

        _btnLight = new FlatButton { Dock = DockStyle.Fill, Height = 32, CornerRadius = 5, Margin = new Padding(4, 2, 2, 2) };
        _btnDark = new FlatButton { Dock = DockStyle.Fill, Height = 32, CornerRadius = 5, Margin = new Padding(2, 2, 0, 2) };
        _btnLight.Click += (_, _) => SetTheme(ThemeMode.Light);
        _btnDark.Click += (_, _) => SetTheme(ThemeMode.Dark);

        themeRow.Controls.Add(_lblTheme, 0, 0);
        themeRow.Controls.Add(_btnLight, 1, 0);
        themeRow.Controls.Add(_btnDark, 2, 0);

        var langRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 2, 0, 0),
        };
        langRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        langRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));

        _lblLanguage = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);
        _lblLanguage.Height = 34;

        _cmbLanguage = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22,
            Margin = new Padding(4, 4, 0, 4),
        };
        foreach (Loc.LanguageInfo info in Loc.Languages)
        {
            _cmbLanguage.Items.Add(info.NativeName);
        }

        _cmbLanguage.DrawItem += OnDrawLanguageItem;
        _cmbLanguage.SelectedIndexChanged += OnLanguageSelected;

        langRow.Controls.Add(_lblLanguage, 0, 0);
        langRow.Controls.Add(_cmbLanguage, 1, 0);

        LayoutHelper.AddRow(inner, _lblAppearance, 26);
        LayoutHelper.AddRow(inner, themeRow);
        LayoutHelper.AddRow(inner, langRow);

        _cardAppearance.Controls.Add(inner);
    }

    private void BuildBehaviorCard()
    {
        _cardBehavior = NewCard();
        TableLayoutPanel inner = LayoutHelper.NewColumn(new Padding(0));

        _lblBehavior = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);

        _chkRestoreOnExit = new CheckBox
        {
            Dock = DockStyle.Fill,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            AutoSize = false,
            Checked = _settings.RestoreAllOnExit,
            Margin = new Padding(0, 6, 0, 0),
        };
        _chkRestoreOnExit.CheckedChanged += (_, _) =>
        {
            _settings.RestoreAllOnExit = _chkRestoreOnExit.Checked;
            _settings.Save();
        };

        _lblRestoreHint = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);
        _lblRestoreHint.Margin = new Padding(0, 0, 0, 8);

        _lblMinOpacity = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);

        _minOpacitySlider = new ModernSlider
        {
            Dock = DockStyle.Fill,
            Minimum = AppSettings.OpacityFloorPercent,
            Maximum = 90,
            Value = _settings.MinimumOpacityPercent,
            Height = 28,
            Margin = new Padding(0, 2, 0, 0),
        };
        _minOpacitySlider.ValueChanged += (_, _) =>
        {
            _settings.MinimumOpacityPercent = _minOpacitySlider.Value;
            _lblMinOpacity.SetText(Loc.T("MinOpacity", _minOpacitySlider.Value));
        };
        _minOpacitySlider.ValueCommitted += (_, _) => _settings.Save();

        _lblMinHint = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);

        LayoutHelper.AddRow(inner, _lblBehavior, 26);
        LayoutHelper.AddRow(inner, _chkRestoreOnExit, 30);
        LayoutHelper.AddRow(inner, _lblRestoreHint, 32);
        LayoutHelper.AddRow(inner, _lblMinOpacity, 26);
        LayoutHelper.AddRow(inner, _minOpacitySlider, 28);
        LayoutHelper.AddRow(inner, _lblMinHint, 30);

        _cardBehavior.Controls.Add(inner);
    }

    private void BuildManagedCard()
    {
        _cardManaged = NewCard();
        TableLayoutPanel inner = LayoutHelper.NewColumn(new Padding(0));

        _lblManaged = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);

        _lblManagedHint = LayoutHelper.NewLabel(ContentAlignment.MiddleLeft);
        _lblManagedHint.Margin = new Padding(0, 0, 0, 6);

        _managedList = new Panel
        {
            Dock = DockStyle.Top,
            Height = ManagedListMaxHeight,
            AutoScroll = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        _btnRestoreAll = new FlatButton
        {
            Kind = FlatButtonKind.Accent,
            Dock = DockStyle.Fill,
            Height = 36,
        };
        _btnRestoreAll.Click += (_, _) =>
        {
            _service.RestoreAll();
            RebuildManagedList();
        };

        LayoutHelper.AddRow(inner, _lblManaged, 26);
        LayoutHelper.AddRow(inner, _lblManagedHint, 34);
        _managedListRow = inner.RowStyles.Count;
        LayoutHelper.AddRow(inner, _managedList, ManagedListMaxHeight);
        LayoutHelper.AddRow(inner, _btnRestoreAll);

        _managedInner = inner;
        _cardManaged.Controls.Add(inner);
    }

    // ------------------------------------------------------------------
    // Managed window rows
    // ------------------------------------------------------------------

    private void RebuildManagedList()
    {
        _managedList.SuspendLayout();

        foreach (Control control in _managedList.Controls.Cast<Control>().ToList())
        {
            _managedList.Controls.Remove(control);
            control.Dispose();
        }

        List<WindowEntry> entries = _service.Tracked
            .OrderBy(entry => entry.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        SetManagedListHeight(entries.Count);

        if (entries.Count == 0)
        {
            Label empty = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);
            empty.Dock = DockStyle.Top;
            empty.Height = 40;
            empty.Text = Loc.T("NoManaged");
            empty.ForeColor = _palette.TextSecondary;
            empty.Font = FontProvider.Get(8.5f);
            _managedList.Controls.Add(empty);
            _btnRestoreAll.Enabled = false;
            _managedList.ResumeLayout(true);
            FitToContent();
            return;
        }

        _btnRestoreAll.Enabled = true;

        // Added in reverse so the first entry ends up at the top once docked.
        foreach (WindowEntry entry in Enumerable.Reverse(entries))
        {
            _managedList.Controls.Add(BuildManagedRow(entry));
        }

        _managedList.ResumeLayout(true);
        FitToContent();
    }

    /// <summary>Gives the list exactly the height its rows need, up to the scrolling cap.</summary>
    private void SetManagedListHeight(int rowCount)
    {
        int wanted = rowCount == 0
            ? 44
            : Math.Min(ManagedListMaxHeight, (rowCount * ManagedRowHeight) + 6);

        if (_managedList.Height == wanted)
        {
            return;
        }

        _managedList.Height = wanted;
        _managedInner.RowStyles[_managedListRow] = new RowStyle(
            SizeType.Absolute,
            wanted + _managedList.Margin.Vertical);
    }

    private Control BuildManagedRow(WindowEntry entry)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = ManagedRowHeight,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0, 2, 0, 2),
            BackColor = _palette.CardBackground,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74f));

        var name = new FlatLabel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            AutoEllipsis = true,
            UseMnemonic = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = entry.DisplayName,
            ForeColor = _palette.TextPrimary,
            BackColor = _palette.CardBackground,
            Font = FontProvider.Get(8.5f),
            Margin = new Padding(0),
        };

        var percent = new FlatLabel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Text = $"{entry.Percent}%",
            ForeColor = _palette.TextSecondary,
            BackColor = _palette.CardBackground,
            Font = FontProvider.Get(8f),
            Margin = new Padding(0),
        };

        var slider = new ModernSlider
        {
            Dock = DockStyle.Fill,
            Minimum = OpacityService.AlphaForPercent(_settings.MinimumOpacityPercent),
            Maximum = 255,
            TrackThickness = 4,
            ThumbRadius = 6,
            Height = 26,
            Margin = new Padding(4, 2, 4, 2),
            BackColor = _palette.CardBackground,
            TrackColor = _palette.SliderTrack,
            FillColor = _palette.Accent,
            ThumbColor = _palette.SliderThumb,
            Mirrored = Loc.IsRightToLeft,
        };
        slider.SetValueSilently(entry.CurrentAlpha);

        var restore = new FlatButton
        {
            Kind = FlatButtonKind.Subtle,
            Dock = DockStyle.Fill,
            Height = 26,
            CornerRadius = 5,
            Text = Loc.T("RestoreOne"),
            Font = FontProvider.Get(8f),
            NormalColor = _palette.SubtleButton,
            HoverColor = _palette.SubtleButtonHover,
            PressedColor = _palette.SubtleButtonHover,
            DisabledColor = _palette.SubtleButton,
            ForeColor = _palette.TextPrimary,
            Margin = new Padding(0, 2, 0, 2),
        };

        slider.ValueChanged += (_, _) =>
        {
            if (!NativeMethods.IsWindow(entry.Handle))
            {
                RebuildManagedList();
                return;
            }

            _service.Apply(entry.Handle, (byte)slider.Value);
            percent.SetText($"{(int)Math.Round(slider.Value / 255.0 * 100.0)}%");
        };

        restore.Click += (_, _) =>
        {
            _service.Restore(entry.Handle);
            RebuildManagedList();
        };

        row.Controls.Add(name, 0, 0);
        row.Controls.Add(slider, 1, 0);
        row.Controls.Add(percent, 2, 0);
        row.Controls.Add(restore, 3, 0);
        return row;
    }

    // ------------------------------------------------------------------
    // Theme and language
    // ------------------------------------------------------------------

    private void SetTheme(ThemeMode mode)
    {
        if (_settings.Theme == mode)
        {
            return;
        }

        _settings.Theme = mode;
        _palette = ThemePalette.For(mode);
        _settings.Save();
        ApplyPalette();
        RebuildManagedList();
    }

    private void OnLanguageSelected(object sender, EventArgs e)
    {
        if (_suppressLanguageEvent || _cmbLanguage.SelectedIndex < 0)
        {
            return;
        }

        string tag = Loc.Languages[_cmbLanguage.SelectedIndex].Tag;
        if (tag == _settings.Language)
        {
            return;
        }

        _settings.Language = tag;
        Loc.Current = tag;
        _settings.Save();

        ApplyLanguage();
        ApplyPalette();
        RebuildManagedList();
        FitToContent();
    }

    private void OnDrawLanguageItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color back = selected ? _palette.Accent : _palette.CardBackground;
        Color fore = selected ? _palette.TextOnAccent : _palette.TextPrimary;

        using (var brush = new SolidBrush(back))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix;
        var textBounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, _cmbLanguage.Items[e.Index].ToString(), _cmbLanguage.Font, textBounds, fore, flags);
    }

    private void ApplyLanguage()
    {
        SuspendLayout();

        RightToLeft = Loc.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;

        // Left-aligned labels follow the reading direction; WinForms does not
        // mirror ContentAlignment on its own.
        ContentAlignment leading = Loc.IsRightToLeft
            ? ContentAlignment.MiddleRight
            : ContentAlignment.MiddleLeft;

        foreach (Label label in new Label[]
                 {
                     _lblAppearance, _lblTheme, _lblLanguage, _lblBehavior,
                     _lblRestoreHint, _lblMinOpacity, _lblMinHint, _lblManaged, _lblManagedHint,
                 })
        {
            label.TextAlign = leading;
        }

        _chkRestoreOnExit.CheckAlign = leading;
        _chkRestoreOnExit.TextAlign = leading;


        Font = FontProvider.Get(9f);
        _lblAppearance.Font = FontProvider.Get(10.5f, FontStyle.Bold);
        _lblBehavior.Font = FontProvider.Get(10.5f, FontStyle.Bold);
        _lblManaged.Font = FontProvider.Get(10.5f, FontStyle.Bold);
        _lblTheme.Font = FontProvider.Get(9f);
        _lblLanguage.Font = FontProvider.Get(9f);
        _lblMinOpacity.Font = FontProvider.Get(9f);
        _lblRestoreHint.Font = FontProvider.Get(8f);
        _lblMinHint.Font = FontProvider.Get(8f);
        _lblManagedHint.Font = FontProvider.Get(8f);
        _chkRestoreOnExit.Font = FontProvider.Get(9f);
        _cmbLanguage.Font = FontProvider.Get(9f);
        _btnLight.Font = FontProvider.Get(9f);
        _btnDark.Font = FontProvider.Get(9f);
        _btnRestoreAll.Font = FontProvider.Get(9.5f);
        _btnClose.Font = FontProvider.Get(9.5f);


        Text = Loc.T("Settings");
        _lblAppearance.Text = Loc.T("Appearance");
        _lblTheme.Text = Loc.T("Theme");
        _btnLight.Text = Loc.T("Light");
        _btnDark.Text = Loc.T("Dark");
        _lblLanguage.Text = Loc.T("Language");
        _lblBehavior.Text = Loc.T("Behavior");
        _chkRestoreOnExit.Text = Loc.T("RestoreOnExit");
        _lblRestoreHint.Text = Loc.T("RestoreOnExitHint");
        _lblMinOpacity.Text = Loc.T("MinOpacity", _settings.MinimumOpacityPercent);
        _lblMinHint.Text = Loc.T("MinOpacityHint");
        _lblManaged.Text = Loc.T("Managed");
        _lblManagedHint.Text = Loc.T("ManagedHint");
        _btnRestoreAll.Text = Loc.T("RestoreAll");
        _btnClose.Text = Loc.T("Close");

        _suppressLanguageEvent = true;
        int index = 0;
        for (int i = 0; i < Loc.Languages.Count; i++)
        {
            if (Loc.Languages[i].Tag == Loc.Current)
            {
                index = i;
                break;
            }
        }

        _cmbLanguage.SelectedIndex = index;
        _suppressLanguageEvent = false;

        _minOpacitySlider.Mirrored = Loc.IsRightToLeft;

        ResumeLayout(true);
        PerformLayout();
    }

    private void ApplyPalette()
    {
        SuspendLayout();

        BackColor = _palette.FormBackground;
        ForeColor = _palette.TextPrimary;
        _root.BackColor = _palette.FormBackground;
        _outer.BackColor = _palette.FormBackground;
        _scroll.BackColor = _palette.FormBackground;
        _footer.BackColor = _palette.FormBackground;

        foreach (CardPanel card in new[] { _cardAppearance, _cardBehavior, _cardManaged })
        {
            card.BackColor = _palette.CardBackground;
            card.BorderColor = _palette.CardBorder;
            PaintChildren(card, _palette.CardBackground);
        }

        _lblAppearance.ForeColor = _palette.TextPrimary;
        _lblBehavior.ForeColor = _palette.TextPrimary;
        _lblManaged.ForeColor = _palette.TextPrimary;
        _lblTheme.ForeColor = _palette.TextPrimary;
        _lblLanguage.ForeColor = _palette.TextPrimary;
        _lblMinOpacity.ForeColor = _palette.TextPrimary;
        _chkRestoreOnExit.ForeColor = _palette.TextPrimary;
        _lblRestoreHint.ForeColor = _palette.TextSecondary;
        _lblMinHint.ForeColor = _palette.TextSecondary;
        _lblManagedHint.ForeColor = _palette.TextSecondary;

        _cmbLanguage.BackColor = _palette.CardBackground;
        _cmbLanguage.ForeColor = _palette.TextPrimary;

        _managedList.BackColor = _palette.CardBackground;

        StyleSlider(_minOpacitySlider);

        bool light = _settings.Theme == ThemeMode.Light;
        StyleSegment(_btnLight, light);
        StyleSegment(_btnDark, !light);

        StyleAccent(_btnRestoreAll);
        StyleAccent(_btnClose);

        if (IsHandleCreated)
        {
            NativeMethods.SetImmersiveDarkMode(Handle, _palette.IsDark);
        }

        ResumeLayout(true);
        Invalidate(true);
    }

    private void PaintChildren(Control parent, Color background)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is FlatButton or ModernSlider)
            {
                child.BackColor = background;
                continue;
            }

            child.BackColor = background;
            if (child.HasChildren)
            {
                PaintChildren(child, background);
            }
        }
    }

    private void StyleSlider(ModernSlider slider)
    {
        slider.TrackColor = _palette.SliderTrack;
        slider.FillColor = _palette.Accent;
        slider.ThumbColor = _palette.SliderThumb;
        slider.Invalidate();
    }

    private void StyleSegment(FlatButton button, bool active)
    {
        button.NormalColor = active ? _palette.Accent : _palette.SubtleButton;
        button.HoverColor = active ? _palette.AccentHover : _palette.SubtleButtonHover;
        button.PressedColor = active ? _palette.AccentPressed : _palette.SubtleButtonHover;
        button.DisabledColor = _palette.SubtleButton;
        button.ForeColor = active ? _palette.TextOnAccent : _palette.TextPrimary;
        button.Invalidate();
    }

    private void StyleAccent(FlatButton button)
    {
        button.NormalColor = _palette.Accent;
        button.HoverColor = _palette.AccentHover;
        button.PressedColor = _palette.AccentPressed;
        button.DisabledColor = _palette.AccentDisabled;
        button.ForeColor = _palette.TextOnAccent;
        button.DisabledForeColor = _palette.TextSecondary;
        button.Invalidate();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        NativeMethods.SetImmersiveDarkMode(Handle, _palette.IsDark);
    }

    // ------------------------------------------------------------------
    // Shared builders
    // ------------------------------------------------------------------

    private static CardPanel NewCard() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(0, 0, 0, 12),
        CornerRadius = 10,
    };
}
