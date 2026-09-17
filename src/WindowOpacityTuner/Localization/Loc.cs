namespace WindowOpacityTuner.Localization;

/// <summary>
/// A small hand-rolled string table. Resx satellite assemblies would be the
/// conventional choice, but they bloat a single-file publish and this app has
/// a few dozen strings, so a dictionary keeps the exe small and the build simple.
/// </summary>
public static class Loc
{
    public sealed record LanguageInfo(string Tag, string NativeName, bool RightToLeft, string FontFamily);

    public static readonly IReadOnlyList<LanguageInfo> Languages = new List<LanguageInfo>
    {
        new("en", "English", false, "Segoe UI"),
        new("ko", "한국어", false, "Malgun Gothic"),
        new("ja", "日本語", false, "Yu Gothic UI"),
        new("zh", "简体中文", false, "Microsoft YaHei UI"),
        new("ar", "العربية", true, "Segoe UI"),
    };

    private static string _current = "en";

    /// <summary>Raised after <see cref="Current"/> changes so open forms can re-read their text.</summary>
    public static event EventHandler LanguageChanged;

    public static string Current
    {
        get => _current;
        set
        {
            string tag = Languages.Any(l => l.Tag == value) ? value : "en";
            if (tag == _current)
            {
                return;
            }

            _current = tag;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static LanguageInfo CurrentInfo => Languages.First(l => l.Tag == _current);

    public static bool IsRightToLeft => CurrentInfo.RightToLeft;

    /// <summary>Looks up a key, falling back to English and then to the key itself.</summary>
    public static string T(string key)
    {
        if (Tables.TryGetValue(_current, out Dictionary<string, string> table)
            && table.TryGetValue(key, out string value))
        {
            return value;
        }

        return Tables["en"].TryGetValue(key, out string fallback) ? fallback : key;
    }

    public static string T(string key, params object[] args)
    {
        try
        {
            return string.Format(T(key), args);
        }
        catch (FormatException)
        {
            return T(key);
        }
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Tables = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "Window Opacity Tuner",
            ["SectionSelect"] = "Select window",
            ["SelectedWindow"] = "Selected window: {0}",
            ["None"] = "none",
            ["PickWindow"] = "Pick a window",
            ["PickAnother"] = "Pick another window",
            ["PickingHint"] = "Move the cursor over a window, then release to select. Esc cancels.",
            ["SectionOpacity"] = "Opacity",
            ["OpacityPercent"] = "Opacity: {0}%",
            ["OpacityPercentOpaque"] = "Opacity: {0}% (opaque)",
            ["RawValue"] = "Value: {0} (0-255)",
            ["Reset"] = "Reset opacity",
            ["Refresh"] = "Refresh window info",
            ["Settings"] = "Settings",
            ["Appearance"] = "Appearance",
            ["Theme"] = "Theme",
            ["Light"] = "Light",
            ["Dark"] = "Dark",
            ["Language"] = "Language",
            ["Behavior"] = "Behavior",
            ["RestoreOnExit"] = "Restore every window when this app closes",
            ["RestoreOnExitHint"] = "Off by default. Opacity stays applied after the tuner exits.",
            ["AlwaysOnTop"] = "Keep this window in front of other windows",
            ["AlwaysOnTopHint"] = "On by default, so the tuner stays visible over the window you picked.",
            ["MinOpacity"] = "Lowest opacity allowed: {0}%",
            ["MinOpacityHint"] = "Stops a window from being dimmed until you cannot find it.",
            ["Managed"] = "Adjusted windows",
            ["ManagedHint"] = "Windows this app has made translucent. Restore one here if you lose track of it.",
            ["NoManaged"] = "No windows adjusted yet.",
            ["RestoreAll"] = "Restore all",
            ["RestoreOne"] = "Restore",
            ["Close"] = "Close",
            ["SelfOpacity"] = "This window's opacity",
            ["ToggleTheme"] = "Switch between light and dark",
            ["WindowGone"] = "That window is no longer open.",
            ["ApplyFailed"] = "Could not change that window's opacity. It may be running with higher privileges; try starting this app as an administrator.",
            ["OwnWindowBlocked"] = "Use the small slider in the top-right corner to change this app's own opacity.",
            ["ProcessLabel"] = "Process: {0}",
        },

        ["ko"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "창 투명도 조절기",
            ["SectionSelect"] = "창 선택",
            ["SelectedWindow"] = "선택된 창: {0}",
            ["None"] = "없음",
            ["PickWindow"] = "창 선택하기",
            ["PickAnother"] = "다른 창 선택하기",
            ["PickingHint"] = "커서를 창 위로 옮긴 뒤 놓으면 선택됩니다. Esc로 취소.",
            ["SectionOpacity"] = "투명도 조절",
            ["OpacityPercent"] = "투명도: {0}%",
            ["OpacityPercentOpaque"] = "투명도: {0}% (불투명)",
            ["RawValue"] = "값: {0} (0-255)",
            ["Reset"] = "투명도 초기화",
            ["Refresh"] = "창 정보 새로고침",
            ["Settings"] = "설정",
            ["Appearance"] = "모양",
            ["Theme"] = "테마",
            ["Light"] = "라이트",
            ["Dark"] = "다크",
            ["Language"] = "언어",
            ["Behavior"] = "동작",
            ["RestoreOnExit"] = "프로그램을 닫을 때 모든 창 복원",
            ["RestoreOnExitHint"] = "기본값은 꺼짐. 조절기를 닫아도 투명도는 그대로 유지됩니다.",
            ["AlwaysOnTop"] = "이 창을 항상 맨 앞에 표시",
            ["AlwaysOnTopHint"] = "기본값 켜짐. 선택한 창에 가려지지 않고 조절기가 계속 보입니다.",
            ["MinOpacity"] = "최소 투명도: {0}%",
            ["MinOpacityHint"] = "창이 아예 보이지 않을 만큼 흐려지는 것을 막아줍니다.",
            ["Managed"] = "조절된 창 목록",
            ["ManagedHint"] = "이 프로그램이 투명하게 만든 창들입니다. 놓친 창이 있으면 여기서 되돌리세요.",
            ["NoManaged"] = "아직 조절한 창이 없습니다.",
            ["RestoreAll"] = "모두 복원",
            ["RestoreOne"] = "복원",
            ["Close"] = "닫기",
            ["SelfOpacity"] = "이 창의 투명도",
            ["ToggleTheme"] = "라이트 / 다크 전환",
            ["WindowGone"] = "그 창은 이미 닫혔습니다.",
            ["ApplyFailed"] = "해당 창의 투명도를 바꿀 수 없습니다. 더 높은 권한으로 실행 중인 창일 수 있으니 이 프로그램을 관리자 권한으로 실행해 보세요.",
            ["OwnWindowBlocked"] = "이 프로그램 자체의 투명도는 오른쪽 위의 작은 슬라이더로 조절하세요.",
            ["ProcessLabel"] = "프로세스: {0}",
        },

        ["ja"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "ウィンドウ透明度調整",
            ["SectionSelect"] = "ウィンドウの選択",
            ["SelectedWindow"] = "選択中のウィンドウ: {0}",
            ["None"] = "なし",
            ["PickWindow"] = "ウィンドウを選ぶ",
            ["PickAnother"] = "別のウィンドウを選ぶ",
            ["PickingHint"] = "カーソルをウィンドウの上に移動して離すと選択されます。Esc で中止。",
            ["SectionOpacity"] = "透明度の調整",
            ["OpacityPercent"] = "不透明度: {0}%",
            ["OpacityPercentOpaque"] = "不透明度: {0}%（不透明）",
            ["RawValue"] = "値: {0}（0-255）",
            ["Reset"] = "透明度をリセット",
            ["Refresh"] = "ウィンドウ情報を更新",
            ["Settings"] = "設定",
            ["Appearance"] = "外観",
            ["Theme"] = "テーマ",
            ["Light"] = "ライト",
            ["Dark"] = "ダーク",
            ["Language"] = "言語",
            ["Behavior"] = "動作",
            ["RestoreOnExit"] = "終了時にすべてのウィンドウを元に戻す",
            ["RestoreOnExitHint"] = "既定はオフ。本アプリを閉じても透明度はそのまま残ります。",
            ["AlwaysOnTop"] = "このウィンドウを常に手前に表示",
            ["AlwaysOnTopHint"] = "既定はオン。選んだウィンドウに隠れず調整パネルが見え続けます。",
            ["MinOpacity"] = "最低不透明度: {0}%",
            ["MinOpacityHint"] = "ウィンドウが見えなくなるまで薄くなるのを防ぎます。",
            ["Managed"] = "調整済みのウィンドウ",
            ["ManagedHint"] = "本アプリが半透明にしたウィンドウの一覧です。見失った場合はここから戻せます。",
            ["NoManaged"] = "まだ調整したウィンドウはありません。",
            ["RestoreAll"] = "すべて元に戻す",
            ["RestoreOne"] = "元に戻す",
            ["Close"] = "閉じる",
            ["SelfOpacity"] = "このウィンドウの不透明度",
            ["ToggleTheme"] = "ライト / ダークを切り替え",
            ["WindowGone"] = "そのウィンドウはすでに閉じられています。",
            ["ApplyFailed"] = "そのウィンドウの透明度を変更できませんでした。より高い権限で実行されている可能性があります。本アプリを管理者として実行してみてください。",
            ["OwnWindowBlocked"] = "本アプリ自身の不透明度は右上の小さなスライダーで変更してください。",
            ["ProcessLabel"] = "プロセス: {0}",
        },

        ["zh"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "窗口透明度调节器",
            ["SectionSelect"] = "选择窗口",
            ["SelectedWindow"] = "已选窗口：{0}",
            ["None"] = "无",
            ["PickWindow"] = "选择窗口",
            ["PickAnother"] = "选择其他窗口",
            ["PickingHint"] = "将光标移到窗口上方后松开即可选中。按 Esc 取消。",
            ["SectionOpacity"] = "透明度调节",
            ["OpacityPercent"] = "不透明度：{0}%",
            ["OpacityPercentOpaque"] = "不透明度：{0}%（不透明）",
            ["RawValue"] = "数值：{0}（0-255）",
            ["Reset"] = "重置透明度",
            ["Refresh"] = "刷新窗口信息",
            ["Settings"] = "设置",
            ["Appearance"] = "外观",
            ["Theme"] = "主题",
            ["Light"] = "浅色",
            ["Dark"] = "深色",
            ["Language"] = "语言",
            ["Behavior"] = "行为",
            ["RestoreOnExit"] = "关闭本程序时还原所有窗口",
            ["RestoreOnExitHint"] = "默认关闭。退出调节器后透明度仍会保留。",
            ["AlwaysOnTop"] = "让本窗口始终显示在最前",
            ["AlwaysOnTopHint"] = "默认开启。调节器不会被所选窗口挡住。",
            ["MinOpacity"] = "最低不透明度：{0}%",
            ["MinOpacityHint"] = "避免窗口被调得完全看不见。",
            ["Managed"] = "已调节的窗口",
            ["ManagedHint"] = "本程序调成半透明的窗口。若找不到某个窗口，可在此还原。",
            ["NoManaged"] = "尚未调节任何窗口。",
            ["RestoreAll"] = "全部还原",
            ["RestoreOne"] = "还原",
            ["Close"] = "关闭",
            ["SelfOpacity"] = "本窗口的不透明度",
            ["ToggleTheme"] = "切换浅色 / 深色",
            ["WindowGone"] = "该窗口已经关闭。",
            ["ApplyFailed"] = "无法更改该窗口的透明度。它可能以更高权限运行，请尝试以管理员身份启动本程序。",
            ["OwnWindowBlocked"] = "请使用右上角的小滑块调节本程序自身的不透明度。",
            ["ProcessLabel"] = "进程：{0}",
        },

        ["ar"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "مُعدِّل شفافية النوافذ",
            ["SectionSelect"] = "اختيار النافذة",
            ["SelectedWindow"] = "النافذة المحددة: {0}",
            ["None"] = "لا شيء",
            ["PickWindow"] = "اختر نافذة",
            ["PickAnother"] = "اختر نافذة أخرى",
            ["PickingHint"] = "حرِّك المؤشر فوق نافذة ثم اترك الزر للاختيار. اضغط Esc للإلغاء.",
            ["SectionOpacity"] = "ضبط الشفافية",
            ["OpacityPercent"] = "درجة العتامة: {0}%",
            ["OpacityPercentOpaque"] = "درجة العتامة: {0}% (معتم)",
            ["RawValue"] = "القيمة: {0} (0-255)",
            ["Reset"] = "إعادة تعيين الشفافية",
            ["Refresh"] = "تحديث معلومات النافذة",
            ["Settings"] = "الإعدادات",
            ["Appearance"] = "المظهر",
            ["Theme"] = "السمة",
            ["Light"] = "فاتح",
            ["Dark"] = "داكن",
            ["Language"] = "اللغة",
            ["Behavior"] = "السلوك",
            ["RestoreOnExit"] = "استعادة كل النوافذ عند إغلاق التطبيق",
            ["RestoreOnExitHint"] = "معطَّل افتراضيًا. تبقى الشفافية مطبَّقة بعد إغلاق التطبيق.",
            ["AlwaysOnTop"] = "إبقاء هذه النافذة أمام النوافذ الأخرى",
            ["AlwaysOnTopHint"] = "مُفعَّل افتراضيًا، فيظل التطبيق مرئيًا فوق النافذة المختارة.",
            ["MinOpacity"] = "أقل عتامة مسموحة: {0}%",
            ["MinOpacityHint"] = "يمنع أن تصبح النافذة باهتة إلى حد عدم العثور عليها.",
            ["Managed"] = "النوافذ المعدَّلة",
            ["ManagedHint"] = "النوافذ التي جعلها التطبيق شبه شفافة. استعِد أيًّا منها من هنا إذا فقدت أثرها.",
            ["NoManaged"] = "لم يتم تعديل أي نافذة بعد.",
            ["RestoreAll"] = "استعادة الكل",
            ["RestoreOne"] = "استعادة",
            ["Close"] = "إغلاق",
            ["SelfOpacity"] = "عتامة هذه النافذة",
            ["ToggleTheme"] = "التبديل بين الفاتح والداكن",
            ["WindowGone"] = "لم تعد تلك النافذة مفتوحة.",
            ["ApplyFailed"] = "تعذّر تغيير شفافية تلك النافذة. قد تكون تعمل بصلاحيات أعلى؛ جرّب تشغيل هذا التطبيق كمسؤول.",
            ["OwnWindowBlocked"] = "استخدم الشريط الصغير في الأعلى لتغيير عتامة هذا التطبيق نفسه.",
            ["ProcessLabel"] = "العملية: {0}",
        },
    };
}
