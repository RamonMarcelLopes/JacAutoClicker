using System.Drawing.Drawing2D;

namespace JacaAutoClicker.Presentation.Controls;

public class DarkNumeric : UserControl
{
    public int Value { get; private set; } = 0;
    public int Maximum { get; set; } = 999;
    public int Minimum { get; set; } = 0;
    private static readonly Color BG = Color.FromArgb(20, 20, 30);
    private static readonly Color BORDER = Color.FromArgb(60, 60, 82);
    private static readonly Color TXT = Color.FromArgb(220, 220, 240);
    private static readonly Color SEP = Color.FromArgb(45, 45, 62);
    private readonly Label lblVal;
    private readonly Button btnUp, btnDn;
    private const int R = 6;

    public DarkNumeric()
    {
        Size = new Size(72, 40); BackColor = BG; DoubleBuffered = true;
        lblVal = new Label { Text = "0", TextAlign = ContentAlignment.MiddleCenter, ForeColor = TXT, BackColor = Color.Transparent, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Bounds = new Rectangle(1, 1, 48, 38), Cursor = Cursors.IBeam };
        lblVal.Click += (s, e) => StartEdit();
        btnUp = MakeArrow("▲", 50, 0);
        btnDn = MakeArrow("▼", 50, 20);
        btnUp.Click += (s, e) => { if (Value < Maximum) { Value++; lblVal.Text = Value.ToString(); } };
        btnDn.Click += (s, e) => { if (Value > Minimum) { Value--; lblVal.Text = Value.ToString(); } };
        Controls.AddRange(new Control[] { lblVal, btnUp, btnDn });
    }

    private Button MakeArrow(string t, int x, int y)
    {
        var b = new Button { Text = t, Bounds = new Rectangle(x, y, 22, 20), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(28, 28, 42), ForeColor = Color.FromArgb(130, 130, 160), Font = new Font("Segoe UI", 7f), Cursor = Cursors.Hand, TabStop = false };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 70);
        return b;
    }

    private void StartEdit()
    {
        var tb = new TextBox { Text = Value.ToString(), Bounds = new Rectangle(1, 1, 48, 38), BackColor = BG, ForeColor = TXT, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
        tb.SelectAll();
        tb.LostFocus += (s, e) => CommitEdit(tb);
        tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) CommitEdit(tb); };
        Controls.Add(tb); tb.BringToFront(); tb.Focus();
    }

    private void CommitEdit(TextBox tb)
    {
        if (int.TryParse(tb.Text, out int v)) Value = Math.Clamp(v, Minimum, Maximum);
        lblVal.Text = Value.ToString();
        Controls.Remove(tb); tb.Dispose();
    }

    public void SetValue(int v) { Value = Math.Clamp(v, Minimum, Maximum); lblVal.Text = Value.ToString(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundPanel.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), R);
        using var pen = new Pen(BORDER, 1f);
        g.DrawPath(pen, path);
        using var penS = new Pen(SEP);
        g.DrawLine(penS, 50, 1, 50, Height - 2);
        g.DrawLine(penS, 50, 20, Width - 2, 20);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundPanel.RoundRect(new Rectangle(0, 0, Width, Height), R);
        using var brush = new SolidBrush(BG);
        g.FillPath(brush, path);
    }
}
