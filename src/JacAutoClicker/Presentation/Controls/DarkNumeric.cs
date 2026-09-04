using System.Drawing.Drawing2D;

namespace JacaAutoClicker.Presentation.Controls;

public class DarkNumeric : UserControl
{
    public int Value { get; private set; } = 0;
    public int Maximum { get; set; } = 999;
    public int Minimum { get; set; } = 0;
    public event EventHandler? ValueChanged;

    private static readonly Color BG = Color.FromArgb(9, 14, 20);
    private static readonly Color BORDER = Color.FromArgb(34, 44, 55);
    private static readonly Color TXT = Color.FromArgb(227, 237, 244);
    private readonly Label lblVal;
    private const int R = 10;
    private const int LeftInset = 12;

    public DarkNumeric()
    {
        BackColor = BG; DoubleBuffered = true;
        lblVal = new Label { Text = "0", TextAlign = ContentAlignment.MiddleLeft, ForeColor = TXT, BackColor = Color.Transparent, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Cursor = Cursors.IBeam };
        lblVal.Click += (s, e) => StartEdit();
        Controls.Add(lblVal);
        Size = new Size(72, 40);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        if (lblVal is null) return;
        lblVal.Bounds = new Rectangle(LeftInset, 1, Math.Max(0, Width - LeftInset - 4), Height - 2);
        Invalidate();
    }

    private void StartEdit()
    {
        lblVal.Visible = false;
        var tb = new TextBox { Text = Value.ToString(), BackColor = BG, ForeColor = TXT, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11f, FontStyle.Bold), TextAlign = HorizontalAlignment.Left };
        tb.Width = lblVal.Bounds.Width;
        tb.Location = new Point(lblVal.Bounds.X, lblVal.Bounds.Y + (lblVal.Bounds.Height - tb.PreferredHeight) / 2);
        tb.SelectAll();
        tb.LostFocus += (s, e) => CommitEdit(tb);
        tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) CommitEdit(tb); };
        Controls.Add(tb); tb.BringToFront(); tb.Focus();
    }

    private void CommitEdit(TextBox tb)
    {
        var previous = Value;
        if (int.TryParse(tb.Text, out int v)) Value = Math.Clamp(v, Minimum, Maximum);
        lblVal.Text = Value.ToString();
        Controls.Remove(tb); tb.Dispose();
        lblVal.Visible = true;
        if (Value != previous) ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetValue(int v) { Value = Math.Clamp(v, Minimum, Maximum); lblVal.Text = Value.ToString(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundPanel.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), R);
        using var pen = new Pen(BORDER, 1f);
        g.DrawPath(pen, path);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var g = e.Graphics;
        using var parentBrush = new SolidBrush(Parent?.BackColor ?? BG);
        g.FillRectangle(parentBrush, ClientRectangle);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundPanel.RoundRect(new Rectangle(0, 0, Width, Height), R);
        using var brush = new SolidBrush(BG);
        g.FillPath(brush, path);
    }
}
