using System.Drawing.Drawing2D;

namespace JacaAutoClicker.Presentation.Controls;

public class SegmentedToggle : UserControl
{
    public event EventHandler? SelectedIndexChanged;

    public int SelectedIndex { get; private set; }

    private readonly string _leftText;
    private readonly string _rightText;

    private static readonly Color TRACK = Color.FromArgb(20, 27, 35);
    private static readonly Color SELECTED_BG = Color.FromArgb(11, 18, 25);
    private static readonly Color TEXT = Color.FromArgb(227, 237, 244);
    private static readonly Color TEXT_MUTED = Color.FromArgb(121, 137, 148);

    public SegmentedToggle(string leftText, string rightText)
    {
        _leftText = leftText;
        _rightText = rightText;
        Size = new Size(180, 32);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        var mousePos = PointToClient(MousePosition);
        var newIndex = mousePos.X < Width / 2 ? 0 : 1;
        if (newIndex != SelectedIndex)
        {
            SelectedIndex = newIndex;
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }

    public void SetSelectedIndex(int index)
    {
        SelectedIndex = index;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var trackPath = RoundPanel.RoundRect(new Rectangle(0, 0, Width, Height), Height / 2);
        using var trackBrush = new SolidBrush(TRACK);
        g.FillPath(trackBrush, trackPath);

        int half = Width / 2;
        var selectedRect = new Rectangle(SelectedIndex == 0 ? 2 : half, 2, half - 4, Height - 4);
        using var selectedPath = RoundPanel.RoundRect(selectedRect, (Height - 4) / 2);
        using var selectedBrush = new SolidBrush(SELECTED_BG);
        g.FillPath(selectedBrush, selectedPath);

        using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        DrawLabel(g, font, _leftText, new Rectangle(0, 0, half, Height), SelectedIndex == 0);
        DrawLabel(g, font, _rightText, new Rectangle(half, 0, Width - half, Height), SelectedIndex == 1);
    }

    private static void DrawLabel(Graphics g, Font font, string text, Rectangle bounds, bool selected)
    {
        using var brush = new SolidBrush(selected ? TEXT : TEXT_MUTED);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, font, brush, bounds, format);
    }
}
