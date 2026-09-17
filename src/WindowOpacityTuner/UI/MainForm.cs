using WindowOpacityTuner.Core;
using WindowOpacityTuner.Interop;
using WindowOpacityTuner.Localization;
using WindowOpacityTuner.Theming;
using WindowOpacityTuner.UI.Controls;

namespace WindowOpacityTuner.UI;

public sealed class MainForm : Form
{
    private const int VkEscape = 0x1B;

    /// <summary>WS_EX_COMPOSITED. Composites the whole window off-screen, which is what
    /// stops the readouts flashing while a slider is dragged on a translucent window.</summary>
    private const int WsExComposited = 0x02000000;

    /// <summary>Floor for this app's own window, low enough to nearly vanish but still be grabbed.</summary>
    private const int SelfOpacityFloor = AppSettings.SelfOpacityFloorPercent;
    private const string CreditText = "@efforthye";
    private const string CreditUrl = "https://github.com/efforthye";
    private const int PickButtonMinWidth = 230;
    private const int PickButtonHeight = 40;

    private readonly AppSettings _settings;
    private readonly OpacityService _service = new();
    private readonly HighlightForm _highlight = new();
    private readonly System.Windows.Forms.Timer _pickTimer = new() { Interval = 40 };

    private ThemePalette _palette;
    private IntPtr _selected = IntPtr.Zero;
    private IntPtr _pickTarget = IntPtr.Zero;
    private bool _picking;
    private bool _suppressSelfSlider;

    // Layout
    private TableLayoutPanel _root;
    private FlowLayoutPanel _header;
    private FlatLabel _lblSelfPercent;
    private ModernSlider _selfSlider;
    private FlatButton _btnTheme;
    private FlatButton _btnSettings;
    private FlatLabel _lblTitle;

    private CardPanel _cardSelect;
    private FlatLabel _lblSelectHeading;
    private FlatLabel _lblSelectedWindow;
    private FlatButton _btnPick;

    private CardPanel _cardOpacity;
    private FlatLabel _lblOpacityHeading;
    private FlatLabel _lblOpacityPercent;
    private ModernSlider _opacitySlider;
    private FlatLabel _lblRawValue;

    private TableLayoutPanel _footer;
    private FlatButton _btnReset;
    private FlatButton _btnRefresh;
    private FlatLabel _lblStatus;
    private FlatLabel _lblCredit;

    public MainForm(AppSettings settings)
    {
        _settings = settings;
        _palette = ThemePalette.For(settings.Theme);
        Loc.Current = settings.Language;

        Icon = AppIcon.Shared;

        BuildUi();
        ApplyLanguage();
        ApplyPalette();
        ApplySelfOpacity(_settings.SelfOpacityPercent, save: false);
        ApplyAlwaysOnTop();
        UpdateSelectionUi();

        _pickTimer.Tick += OnPickTick;
        _service.TrackedChanged += (_, _) => UpdateStatusForTracked();
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
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(460, 0);

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 12, 18, 14),
        };
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        BuildHeader();
        BuildTitle();
        BuildSelectCard();
        BuildOpacityCard();
        BuildFooter();
        BuildStatus();
        BuildCredit();

        LayoutHelper.AddRow(_root, _header);
        LayoutHelper.AddRow(_root, _lblTitle, 28);
        LayoutHelper.AddRow(_root, _cardSelect);
        LayoutHelper.AddRow(_root, _cardOpacity);
        LayoutHelper.AddRow(_root, _footer);
        LayoutHelper.AddRow(_root, _lblStatus, 46);
        LayoutHelper.AddRow(_root, _lblCredit, 18);

        Controls.Add(_root);
        ResumeLayout(true);
    }

    private void BuildHeader()
    {
        _header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 2),
        };

        _btnSettings = new FlatButton
        {
            Kind = FlatButtonKind.Subtle,
            Text = "⚙",
            Size = new Size(32, 26),
            CornerRadius = 5,
            Margin = new Padding(4, 0, 0, 0),
        };
        _btnSettings.Click += (_, _) => OpenSettings();

        _btnTheme = new FlatButton
        {
            Kind = FlatButtonKind.Subtle,
            Text = "◐",
            Size = new Size(32, 26),
            CornerRadius = 5,
            Margin = new Padding(4, 0, 0, 0),
        };
        _btnTheme.Click += (_, _) => ToggleTheme();

        _selfSlider = new ModernSlider
        {
            Minimum = SelfOpacityFloor,
            Maximum = 100,
            Value = 100,
            TrackThickness = 4,
            ThumbRadius = 6,
            Size = new Size(84, 26),
            Margin = new Padding(6, 0, 2, 0),
        };
        _selfSlider.ValueChanged += (_, _) =>
        {
            if (!_suppressSelfSlider)
            {
                ApplySelfOpacity(_selfSlider.Value, save: false);
            }
        };
        _selfSlider.ValueCommitted += (_, _) => _settings.Save();

        _lblSelfPercent = new FlatLabel
        {
            AutoSize = false,
            Size = new Size(38, 26),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0),
        };

        // FlowDirection.RightToLeft lays these out from the right edge inwards.
        _header.Controls.Add(_btnSettings);
        _header.Controls.Add(_btnTheme);
        _header.Controls.Add(_selfSlider);
        _header.Controls.Add(_lblSelfPercent);
    }

    private void BuildTitle()
    {
        _lblTitle = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);
        _lblTitle.AutoEllipsis = true;
        _lblTitle.Margin = new Padding(0, 0, 0, 6);
    }

    private void BuildSelectCard()
    {
        _cardSelect = NewCard();
        TableLayoutPanel inner = LayoutHelper.NewColumn(new Padding(0));

        _lblSelectHeading = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);

        _lblSelectedWindow = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);
        _lblSelectedWindow.AutoEllipsis = true;
        _lblSelectedWindow.Margin = new Padding(0, 2, 0, 6);

        // Sized explicitly (see FlatButton.SizeToText) rather than auto-sized: an
        // auto-sizing child of a TableLayoutPanel cell is placed before its preferred
        // width is known, which left this button hanging off to the right whenever the
        // caption grew from "Pick a window" to "Pick another window".
        _btnPick = new FlatButton
        {
            Kind = FlatButtonKind.Accent,
            Anchor = AnchorStyles.None,
            AutoSize = false,
            Size = new Size(PickButtonMinWidth, PickButtonHeight),
            Margin = new Padding(0, 2, 0, 0),
        };
        _btnPick.Click += (_, _) => BeginPick();

        LayoutHelper.AddRow(inner, _lblSelectHeading, 26);
        LayoutHelper.AddRow(inner, _lblSelectedWindow, 40);
        LayoutHelper.AddRow(inner, _btnPick);

        _cardSelect.Controls.Add(inner);
    }

    private void BuildOpacityCard()
    {
        _cardOpacity = NewCard();
        TableLayoutPanel inner = LayoutHelper.NewColumn(new Padding(0));

        _lblOpacityHeading = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);

        _lblOpacityPercent = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);
        _lblOpacityPercent.Margin = new Padding(0, 2, 0, 2);

        _opacitySlider = new ModernSlider
        {
            Dock = DockStyle.Fill,
            Minimum = OpacityService.AlphaForPercent(AppSettings.OpacityFloorPercent),
            Maximum = 255,
            Value = 255,
            Height = 30,
            Margin = new Padding(4, 4, 4, 2),
            Enabled = false,
        };
        _opacitySlider.ValueChanged += (_, _) => OnOpacitySliderChanged();

        _lblRawValue = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);

        LayoutHelper.AddRow(inner, _lblOpacityHeading, 26);
        LayoutHelper.AddRow(inner, _lblOpacityPercent, 28);
        LayoutHelper.AddRow(inner, _opacitySlider, 30);
        LayoutHelper.AddRow(inner, _lblRawValue, 22);

        _cardOpacity.Controls.Add(inner);
    }

    private void BuildFooter()
    {
        _footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 10, 0, 0),
        };
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        _footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _btnReset = new FlatButton
        {
            Kind = FlatButtonKind.Accent,
            Dock = DockStyle.Fill,
            Height = 40,
            Margin = new Padding(0, 0, 5, 0),
            Enabled = false,
        };
        _btnReset.Click += (_, _) => ResetSelected();

        _btnRefresh = new FlatButton
        {
            Kind = FlatButtonKind.Accent,
            Dock = DockStyle.Fill,
            Height = 40,
            Margin = new Padding(5, 0, 0, 0),
        };
        _btnRefresh.Click += (_, _) => RefreshSelected();

        _footer.Controls.Add(_btnReset, 0, 0);
        _footer.Controls.Add(_btnRefresh, 1, 0);
    }

    private void BuildStatus()
    {
        // Long strings (the picker hint, the privilege error) wrap rather than
        // truncate, so the row is tall enough for two lines in every language.
        _lblStatus = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);
        _lblStatus.Margin = new Padding(0, 8, 0, 0);
    }

    private void BuildCredit()
    {
        _lblCredit = LayoutHelper.NewLabel(ContentAlignment.MiddleCenter);
        _lblCredit.Text = CreditText;
        _lblCredit.Margin = new Padding(0, 0, 0, 2);
        _lblCredit.Cursor = Cursors.Hand;

        // Auto-sized and centred rather than filling the row: the control is then exactly
        // as wide as the text, so the hand cursor and the click only happen on the name
        // itself and not across the empty width of the window.
        _lblCredit.Dock = DockStyle.None;
        _lblCredit.AutoSize = true;
        _lblCredit.Anchor = AnchorStyles.None;

        _lblCredit.MouseEnter += (_, _) => SetCreditHot(true);
        _lblCredit.MouseLeave += (_, _) => SetCreditHot(false);
        _lblCredit.Click += (_, _) => OpenCreditUrl();
    }

    /// <summary>Brightens on hover. No underline: the hand cursor is enough of a hint.</summary>
    private void SetCreditHot(bool hot) =>
        _lblCredit.ForeColor = hot ? _palette.TextSecondary : CreditColor();

    /// <summary>Two steps quieter than secondary text, so the credit never competes with the controls.</summary>
    private Color CreditColor() => Blend(_palette.TextSecondary, _palette.FormBackground, 0.45f);

    private static Color Blend(Color color, Color towards, float amount) => Color.FromArgb(
        (int)(color.R + ((towards.R - color.R) * amount)),
        (int)(color.G + ((towards.G - color.G) * amount)),
        (int)(color.B + ((towards.B - color.B) * amount)));

    private void OpenCreditUrl()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(CreditUrl)
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // No browser, or the shell refused: a dead credit line is not worth an error dialog.
        }
    }

    private static CardPanel NewCard() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = new Padding(0, 0, 0, 12),
        CornerRadius = 10,
    };

    // ------------------------------------------------------------------
    // Theme and language
    // ------------------------------------------------------------------

    private void ToggleTheme()
    {
        _settings.Theme = _settings.Theme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        _palette = ThemePalette.For(_settings.Theme);
        ApplyPalette();
        _settings.Save();
    }

    internal void ApplyPalette()
    {
        SuspendLayout();

        BackColor = _palette.FormBackground;
        ForeColor = _palette.TextPrimary;
        _root.BackColor = _palette.FormBackground;
        _header.BackColor = _palette.FormBackground;
        _footer.BackColor = _palette.FormBackground;

        _lblTitle.ForeColor = _palette.TextPrimary;
        _lblSelfPercent.ForeColor = _palette.TextSecondary;
        _lblStatus.ForeColor = _palette.TextSecondary;
        _lblCredit.ForeColor = CreditColor();

        foreach (CardPanel card in new[] { _cardSelect, _cardOpacity })
        {
            card.BackColor = _palette.CardBackground;
            card.BorderColor = _palette.CardBorder;
            foreach (Control child in card.Controls)
            {
                child.BackColor = _palette.CardBackground;
            }
        }

        _lblSelectHeading.ForeColor = _palette.TextPrimary;
        _lblOpacityHeading.ForeColor = _palette.TextPrimary;
        _lblSelectedWindow.ForeColor = _palette.TextSecondary;
        _lblOpacityPercent.ForeColor = _palette.TextPrimary;
        _lblRawValue.ForeColor = _palette.TextSecondary;

        StyleSlider(_opacitySlider, _palette.CardBackground);
        StyleSlider(_selfSlider, _palette.FormBackground);

        StyleAccentButton(_btnPick);
        StyleAccentButton(_btnReset);
        StyleAccentButton(_btnRefresh);
        StyleSubtleButton(_btnTheme);
        StyleSubtleButton(_btnSettings);

        _btnTheme.Text = _palette.IsDark ? "◑" : "◐";
        _highlight.BorderColor = _palette.Highlight;

        // Guarded so painting the title bar never forces the handle into existence
        // early; OnShown applies it once the window really is there.
        if (IsHandleCreated)
        {
            NativeMethods.SetImmersiveDarkMode(Handle, _palette.IsDark);
        }

        ResumeLayout(true);
        Invalidate(true);
    }

    private void StyleSlider(ModernSlider slider, Color surface)
    {
        slider.BackColor = surface;
        slider.TrackColor = _palette.SliderTrack;
        slider.FillColor = _palette.Accent;
        slider.ThumbColor = _palette.SliderThumb;
        slider.Mirrored = Loc.IsRightToLeft;
        slider.Invalidate();
    }

    private void StyleAccentButton(FlatButton button)
    {
        button.NormalColor = _palette.Accent;
        button.HoverColor = _palette.AccentHover;
        button.PressedColor = _palette.AccentPressed;
        button.DisabledColor = _palette.AccentDisabled;
        button.ForeColor = _palette.TextOnAccent;
        button.DisabledForeColor = _palette.TextSecondary;
        button.Invalidate();
    }

    private void StyleSubtleButton(FlatButton button)
    {
        button.NormalColor = _palette.SubtleButton;
        button.HoverColor = _palette.SubtleButtonHover;
        button.PressedColor = _palette.SubtleButtonHover;
        button.DisabledColor = _palette.SubtleButton;
        button.ForeColor = _palette.TextPrimary;
        button.Invalidate();
    }

    internal void ApplyLanguage()
    {
        SuspendLayout();

        // FlowLayoutPanel reverses its own flow when RightToLeft is on, so the header
        // controls move to the mirrored corner for Arabic without touching FlowDirection.
        RightToLeft = Loc.IsRightToLeft ? RightToLeft.Yes : RightToLeft.No;


        Font = FontProvider.Get(9f);
        _lblTitle.Font = FontProvider.Get(11f, FontStyle.Bold);
        _lblSelectHeading.Font = FontProvider.Get(10.5f, FontStyle.Bold);
        _lblOpacityHeading.Font = FontProvider.Get(10.5f, FontStyle.Bold);
        _lblSelectedWindow.Font = FontProvider.Get(8.5f);
        _lblOpacityPercent.Font = FontProvider.Get(10.5f);
        _lblRawValue.Font = FontProvider.Get(8f);
        _lblStatus.Font = FontProvider.Get(8.5f);
        _lblCredit.Font = FontProvider.Get(8f);
        _lblSelfPercent.Font = FontProvider.Get(8f);
        _btnPick.Font = FontProvider.Get(9.5f);
        _btnReset.Font = FontProvider.Get(9.5f);
        _btnRefresh.Font = FontProvider.Get(9.5f);
        _btnTheme.Font = FontProvider.Get(11f);
        _btnSettings.Font = FontProvider.Get(11f);


        Text = Loc.T("AppTitle");
        _lblTitle.Text = Loc.T("AppTitle");
        _lblSelectHeading.Text = Loc.T("SectionSelect");
        _lblOpacityHeading.Text = Loc.T("SectionOpacity");
        _btnPick.Text = _selected == IntPtr.Zero ? Loc.T("PickWindow") : Loc.T("PickAnother");
        _btnReset.Text = Loc.T("Reset");
        _btnRefresh.Text = Loc.T("Refresh");

        _selfSlider.Mirrored = Loc.IsRightToLeft;
        _opacitySlider.Mirrored = Loc.IsRightToLeft;

        UpdateSelectionUi();
        UpdateSelfLabel();

        ResumeLayout(true);
        PerformLayout();
    }

    // ------------------------------------------------------------------
    // Picking
    // ------------------------------------------------------------------

    private void BeginPick()
    {
        if (_picking)
        {
            return;
        }

        _picking = true;
        _pickTarget = IntPtr.Zero;
        SetStatus(Loc.T("PickingHint"));
        Cursor = Cursors.Cross;

        // Capturing the mouse means the click that selects a window is delivered
        // to us and never reaches the app underneath.
        Capture = true;
        _pickTimer.Start();
    }

    private void EndPick(bool commit)
    {
        if (!_picking)
        {
            return;
        }

        _picking = false;
        _pickTimer.Stop();
        Cursor = Cursors.Default;
        _highlight.HideOutline();

        if (Capture)
        {
            Capture = false;
        }

        IntPtr target = _pickTarget;
        _pickTarget = IntPtr.Zero;

        if (commit && target != IntPtr.Zero)
        {
            SelectWindow(target);

            // The window that was just picked usually sits on top of us, and the
            // next thing the user wants is the slider, so come back to the front
            // with the slider already focused for the arrow keys and the wheel.
            BringTunerForward();
        }
        else
        {
            UpdateStatusForTracked();
        }
    }

    private void BringTunerForward()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        Activate();
        NativeMethods.ForceForeground(Handle, _settings.AlwaysOnTop);

        if (_opacitySlider.Enabled)
        {
            _opacitySlider.Focus();
        }
    }

    private void OnPickTick(object sender, EventArgs e)
    {
        if (!_picking)
        {
            return;
        }

        if ((NativeMethods.GetAsyncKeyState(VkEscape) & 0x8000) != 0)
        {
            EndPick(false);
            return;
        }

        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT cursor))
        {
            return;
        }

        IntPtr under = NativeMethods.WindowFromPoint(cursor);
        IntPtr root = under == IntPtr.Zero ? IntPtr.Zero : NativeMethods.GetAncestor(under, NativeMethods.GA_ROOT);

        if (root == IntPtr.Zero || IsOwnWindow(root))
        {
            _pickTarget = IntPtr.Zero;
            _highlight.HideOutline();
            SetStatus(root != IntPtr.Zero && IsOwnWindow(root) ? Loc.T("OwnWindowBlocked") : Loc.T("PickingHint"));
            return;
        }

        if (IsDesktopWindow(root))
        {
            _pickTarget = IntPtr.Zero;
            _highlight.HideOutline();
            return;
        }

        _pickTarget = root;
        _highlight.SurroundScreenRect(NativeMethods.GetVisibleBounds(root));

        string title = NativeMethods.GetWindowTitle(root);
        SetStatus(string.IsNullOrWhiteSpace(title) ? Loc.T("PickingHint") : title);
    }

    private static bool IsOwnWindow(IntPtr hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return pid == (uint)Environment.ProcessId;
    }

    private static bool IsDesktopWindow(IntPtr hwnd)
    {
        if (hwnd == NativeMethods.GetShellWindow())
        {
            return true;
        }

        string className = NativeMethods.GetWindowClass(hwnd);
        return className is "Progman" or "WorkerW" or "Shell_TrayWnd";
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (_picking)
        {
            EndPick(e.Button == MouseButtons.Left);
            return;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);

        // Losing capture (alt-tab, a system dialog) should not leave us stuck in pick mode.
        if (_picking && !Capture)
        {
            EndPick(false);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_picking && e.KeyCode == Keys.Escape)
        {
            EndPick(false);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    // ------------------------------------------------------------------
    // Selection and opacity
    // ------------------------------------------------------------------

    private void SelectWindow(IntPtr hwnd)
    {
        if (!NativeMethods.IsWindow(hwnd))
        {
            SetStatus(Loc.T("WindowGone"));
            return;
        }

        _selected = hwnd;
        byte alpha = OpacityService.ReadAlpha(hwnd);

        _opacitySlider.Minimum = MinimumAlpha();
        _opacitySlider.SetValueSilently(alpha);

        UpdateSelectionUi();
        UpdateStatusForTracked();
    }

    private int MinimumAlpha() => OpacityService.AlphaForPercent(_settings.MinimumOpacityPercent);

    private void OnOpacitySliderChanged()
    {
        UpdateOpacityLabels();

        if (_selected == IntPtr.Zero)
        {
            return;
        }

        if (!NativeMethods.IsWindow(_selected))
        {
            _service.Forget(_selected);
            _selected = IntPtr.Zero;
            UpdateSelectionUi();
            SetStatus(Loc.T("WindowGone"));
            return;
        }

        if (!_service.Apply(_selected, (byte)_opacitySlider.Value))
        {
            SetStatus(Loc.T("ApplyFailed"));
        }
    }

    private void ResetSelected()
    {
        if (_selected == IntPtr.Zero)
        {
            return;
        }

        _service.Restore(_selected);
        _opacitySlider.SetValueSilently(255);
        UpdateOpacityLabels();
        UpdateStatusForTracked();
    }

    private void RefreshSelected()
    {
        _service.PruneDeadWindows();
        _service.RefreshTitles();

        if (_selected != IntPtr.Zero && !NativeMethods.IsWindow(_selected))
        {
            _selected = IntPtr.Zero;
            SetStatus(Loc.T("WindowGone"));
        }
        else if (_selected != IntPtr.Zero)
        {
            _opacitySlider.SetValueSilently(OpacityService.ReadAlpha(_selected));
        }

        UpdateSelectionUi();
    }

    private void UpdateSelectionUi()
    {
        bool has = _selected != IntPtr.Zero && NativeMethods.IsWindow(_selected);

        _opacitySlider.Enabled = has;
        _btnReset.Enabled = has;
        _btnPick.Text = has ? Loc.T("PickAnother") : Loc.T("PickWindow");
        _btnPick.SizeToText(PickButtonMinWidth, PickButtonHeight);

        string name = Loc.T("None");
        if (has)
        {
            string title = NativeMethods.GetWindowTitle(_selected);
            string process = OpacityService.GetProcessName(_selected);
            name = string.IsNullOrWhiteSpace(title)
                ? (string.IsNullOrWhiteSpace(process) ? Loc.T("None") : process)
                : title;
        }

        _lblSelectedWindow.SetText(Loc.T("SelectedWindow", name));
        UpdateOpacityLabels();
    }

    private void UpdateOpacityLabels()
    {
        int alpha = _opacitySlider.Value;
        int percent = (int)Math.Round(alpha / 255.0 * 100.0);

        _lblOpacityPercent.SetText(alpha >= 255
            ? Loc.T("OpacityPercentOpaque", percent)
            : Loc.T("OpacityPercent", percent));

        _lblRawValue.SetText(Loc.T("RawValue", alpha));
    }

    private void UpdateStatusForTracked()
    {
        if (_picking)
        {
            return;
        }

        int count = _service.TrackedCount;
        SetStatus(count == 0 ? string.Empty : $"{Loc.T("Managed")}: {count}");
    }

    private void SetStatus(string text) => _lblStatus.SetText(text);

    // ------------------------------------------------------------------
    // Own-window opacity
    // ------------------------------------------------------------------

    private void ApplySelfOpacity(int percent, bool save)
    {
        percent = Math.Clamp(percent, SelfOpacityFloor, 100);
        _settings.SelfOpacityPercent = percent;
        Opacity = percent / 100.0;

        _suppressSelfSlider = true;
        _selfSlider.SetValueSilently(percent);
        _suppressSelfSlider = false;

        UpdateSelfLabel();

        if (save)
        {
            _settings.Save();
        }
    }

    private void UpdateSelfLabel() => _lblSelfPercent.SetText($"{_settings.SelfOpacityPercent}%");

    private void ApplyAlwaysOnTop() => TopMost = _settings.AlwaysOnTop;

    // ------------------------------------------------------------------
    // Settings dialog
    // ------------------------------------------------------------------

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settings, _service);
        dialog.ShowDialog(this);

        _palette = ThemePalette.For(_settings.Theme);
        Loc.Current = _settings.Language;

        ApplyLanguage();
        ApplyPalette();
        ApplyAlwaysOnTop();

        _opacitySlider.Minimum = MinimumAlpha();
        if (_selected != IntPtr.Zero && NativeMethods.IsWindow(_selected))
        {
            _opacitySlider.SetValueSilently(OpacityService.ReadAlpha(_selected));
        }

        UpdateSelectionUi();
        UpdateStatusForTracked();
        _settings.Save();
    }

    // ------------------------------------------------------------------
    // Lifetime
    // ------------------------------------------------------------------

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WsExComposited;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        NativeMethods.SetImmersiveDarkMode(Handle, _palette.IsDark);

        if (_settings.RememberWindowPosition && _settings.WindowLeft >= 0 && _settings.WindowTop >= 0)
        {
            var saved = new Point(_settings.WindowLeft, _settings.WindowTop);
            if (Screen.AllScreens.Any(s => s.WorkingArea.Contains(saved)))
            {
                Location = saved;
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        EndPick(false);

        if (_settings.RestoreAllOnExit)
        {
            _service.RestoreAll();
        }

        if (_settings.RememberWindowPosition)
        {
            _settings.WindowLeft = Location.X;
            _settings.WindowTop = Location.Y;
        }

        _settings.Save();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pickTimer.Dispose();
            _highlight.Dispose();
        }

        base.Dispose(disposing);
    }
}
