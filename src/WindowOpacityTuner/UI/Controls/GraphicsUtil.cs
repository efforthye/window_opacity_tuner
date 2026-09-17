using System.Drawing.Drawing2D;

namespace WindowOpacityTuner.UI.Controls;

internal static class GraphicsUtil
{
    /// <summary>Builds a rounded rectangle path, degrading to a plain rectangle at radius 0.</summary>
    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();

        if (radius <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);

        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    public static void EnableQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }
}
