using System.Drawing.Drawing2D;

namespace JacaAutoClicker.Presentation.Controls;

public class DarkRadio : UserControl
{
    public bool Checked { get; private set; }
    public string RadioLabel { get; set; }
    public event EventHandler? CheckedChanged;
    private static readonly Color ACCENT = Color.FromArgb(99, 102, 241);
    private static readonly Color TXT = Color.FromArgb(210, 210, 235);
    private static readonly Color BORDER = Color.FromArgb(75, 75, 100);

    public DarkRadio(string label, bool chk = false)
    {
        RadioLabel = label; Checked = chk;
        Size = new Size(155, 32); BackColor = Color.Transparent; Cursor = Cursors.Hand; DoubleBuffered = true;
        Click += (s, e) => { if (!Checked) { Checked = true; CheckedChanged?.Invoke(this, e); Invalidate(); } };
    }

    public void Uncheck() { Checked = false; Invalidate(); }

    public void Check() { Checked = true; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Color.Black);
        int cy = Height / 2, r = 8;
        var rect = new Rectangle(2, cy - r, r * 2, r * 2);
        using var bp = new Pen(Checked ? ACCENT : BORDER, 1.5f);
        g.DrawEllipse(bp, rect);
        if (Checked) { using var fill = new SolidBrush(ACCENT); g.FillEllipse(fill, new Rectangle(rect.X + 4, rect.Y + 4, r * 2 - 8, r * 2 - 8)); }
        using var tb = new SolidBrush(TXT);
        using var f = new Font("Segoe UI", 9.5f);
        g.DrawString(RadioLabel, f, tb, new PointF(r * 2 + 8, cy - 8));
    }
}
