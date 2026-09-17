using WindowOpacityTuner.UI.Controls;

namespace WindowOpacityTuner.UI;

internal static class LayoutHelper
{
    /// <summary>
    /// Adds a control as the next row of a single-column table.
    ///
    /// Rows carrying a fixed-height control get an absolute row style. Labels measure
    /// their own text, and the five supported languages produce very different string
    /// lengths, so letting rows auto-size from label text makes the layout jump around
    /// when the language changes. Pinning those rows keeps every language aligned.
    /// </summary>
    public static void AddRow(TableLayoutPanel panel, Control control, int absoluteHeight = 0)
    {
        int row = panel.RowStyles.Count;

        panel.RowStyles.Add(absoluteHeight > 0
            ? new RowStyle(SizeType.Absolute, absoluteHeight + control.Margin.Vertical)
            : new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(control, 0, row);
    }

    public static TableLayoutPanel NewColumn(Padding margin) => new()
    {
        Dock = DockStyle.Top,
        ColumnCount = 1,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Margin = margin,
        ColumnStyles = { new ColumnStyle(SizeType.Percent, 100f) },
    };

    /// <summary>A label sized by its row rather than by its text, so it never reflows the card.</summary>
    public static FlatLabel NewLabel(ContentAlignment align) => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = false,
        TextAlign = align,
        UseMnemonic = false,
        Margin = new Padding(0),
    };
}
