using System.Drawing.Drawing2D;

namespace JacaAutoClicker.Presentation.Controls;

public class RoundPanel : Panel
{
    public int Radius { get; set; } = 8;
    public bool ShowBorder { get; set; } = true;
    public Color BorderColor { get; set; } = Color.FromArgb(50, 50, 68);

    public RoundPanel() { DoubleBuffered = true; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!ShowBorder) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
        using var pen = new Pen(BorderColor, 1f);
        g.DrawPath(pen, path);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        using var parentBrush = new SolidBrush(Parent?.BackColor ?? BackColor);
        g.FillRectangle(parentBrush, ClientRectangle);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundRect(new Rectangle(0, 0, Width, Height), Radius);
        using var brush = new SolidBrush(BackColor);
        g.FillPath(brush, path);
    }

    /// <summary>Builds a rounded-rect path where <paramref name="rad"/> is the true corner radius (AddArc takes a diameter, so it's doubled internally).</summary>
    public static GraphicsPath RoundRect(Rectangle r, int rad)
    {
        int d = rad * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
