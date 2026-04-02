using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using Microsoft.Win32;

namespace JacaAutoClicker
{
    static class DwmHelper
    {
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public static void Dark(IntPtr hwnd) { int v = 1; DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref v, sizeof(int)); }
    }

    // ── Painel com bordas arredondadas ────────────────────────────────
    class RoundPanel : Panel
    {
        public int Radius { get; set; } = 8;
        static readonly Color BORDER = Color.FromArgb(50, 50, 68);
        public RoundPanel() { DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius);
            using var pen = new Pen(BORDER, 1f);
            g.DrawPath(pen, path);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundRect(new Rectangle(0, 0, Width, Height), Radius);
            using var brush = new SolidBrush(BackColor);
            g.FillPath(brush, path);
        }
        public static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad, rad, 180, 90);
            p.AddArc(r.Right - rad, r.Y, rad, rad, 270, 90);
            p.AddArc(r.Right - rad, r.Bottom - rad, rad, rad, 0, 90);
            p.AddArc(r.X, r.Bottom - rad, rad, rad, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // ── Numeric escuro arredondado ─────────────────────────────────────
    class DarkNumeric : UserControl
    {
        public int Value { get; private set; } = 0;
        public int Maximum { get; set; } = 999;
        public int Minimum { get; set; } = 0;
        static readonly Color BG = Color.FromArgb(20, 20, 30);
        static readonly Color BORDER = Color.FromArgb(60, 60, 82);
        static readonly Color TXT = Color.FromArgb(220, 220, 240);
        static readonly Color SEP = Color.FromArgb(45, 45, 62);
        Label lblVal;
        Button btnUp, btnDn;
        const int R = 6;

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

        Button MakeArrow(string t, int x, int y)
        {
            var b = new Button { Text = t, Bounds = new Rectangle(x, y, 22, 20), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(28, 28, 42), ForeColor = Color.FromArgb(130, 130, 160), Font = new Font("Segoe UI", 7f), Cursor = Cursors.Hand, TabStop = false };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 70);
            return b;
        }

        void StartEdit()
        {
            var tb = new TextBox { Text = Value.ToString(), Bounds = new Rectangle(1, 1, 48, 38), BackColor = BG, ForeColor = TXT, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11f, FontStyle.Bold), TextAlign = HorizontalAlignment.Center };
            tb.SelectAll();
            tb.LostFocus += (s, e) => CommitEdit(tb);
            tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) CommitEdit(tb); };
            Controls.Add(tb); tb.BringToFront(); tb.Focus();
        }

        void CommitEdit(TextBox tb)
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

    // ── Radio escuro arredondado ───────────────────────────────────────
    class DarkRadio : UserControl
    {
        public bool Checked { get; private set; }
        public string RadioLabel { get; set; }
        public event EventHandler CheckedChanged;
        static readonly Color ACCENT = Color.FromArgb(99, 102, 241);
        static readonly Color TXT = Color.FromArgb(210, 210, 235);
        static readonly Color BORDER = Color.FromArgb(75, 75, 100);

        public DarkRadio(string label, bool chk = false)
        {
            RadioLabel = label; Checked = chk;
            Size = new Size(155, 32); BackColor = Color.Transparent; Cursor = Cursors.Hand; DoubleBuffered = true;
            Click += (s, e) => { if (!Checked) { Checked = true; CheckedChanged?.Invoke(this, e); Invalidate(); } };
        }

        public void Uncheck() { Checked = false; Invalidate(); }

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

    // ══════════════════════════════════════════════════════════════════
    //  MAIN FORM
    // ══════════════════════════════════════════════════════════════════
    public class MainForm : Form
    {
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, int dx, int dy, uint cButtons, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
        delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] static extern IntPtr GetModuleHandle(string lpModuleName);

        const int WH_MOUSE_LL = 14;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_RBUTTONDOWN = 0x0204;
        const int WM_MBUTTONDOWN = 0x0207;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        const uint MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        static readonly Color BG = Color.FromArgb(15, 15, 22);
        static readonly Color SURFACE = Color.FromArgb(24, 24, 34);
        static readonly Color CARD = Color.FromArgb(28, 28, 40);
        static readonly Color ACCENT = Color.FromArgb(99, 102, 241);
        static readonly Color ACCENT_DARK = Color.FromArgb(67, 70, 172);
        static readonly Color TEXT_PRI = Color.FromArgb(220, 220, 240);
        static readonly Color TEXT_SEC = Color.FromArgb(110, 110, 145);
        static readonly Color BORDER = Color.FromArgb(50, 50, 68);
        static readonly Color GREEN = Color.FromArgb(52, 211, 153);
        static readonly Color RED = Color.FromArgb(248, 113, 113);
        static readonly Color AMBER = Color.FromArgb(251, 191, 36);

        const string REG_KEY = @"Software\JacaAutoClicker";
        const int MARGIN = 13;
        const int CWIDTH = 374;

        bool isRunning = false;
        bool isListening = false;
        Keys bindKey = Keys.None;
        int bindMouse = -1;
        string bindLabel = "F6";
        long clickCount = 0;
        long limitCount = 0; // 0 = infinito
        // CPS
        long cpsClicks = 0;
        float cpsValue = 0f;
        System.Windows.Forms.Timer cpsTimer;

        Thread clickThread;
        System.Windows.Forms.Timer pollTimer;
        IntPtr mouseHook = IntPtr.Zero;
        LowLevelMouseProc mouseHookProc;

        RoundPanel pnlBind, pnlCps, pnlInterval, pnlButton, pnlCountRow, pnlStatus;
        Button btnBind, btnStart, btnReset;
        Label lblBindValue, lblBindHint, lblStatus, lblCount, lblLimit, lblCps;
        DarkNumeric nudHours, nudMin, nudSec, nudMs, nudLimit;
        DarkRadio rdLeft, rdRight;

        public MainForm()
        {
            InitUI();
            LoadSettings();
            StartTimers();
        }

        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); DwmHelper.Dark(Handle); }

        // ── Settings ──────────────────────────────────────────────────
        void LoadSettings()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REG_KEY);
                if (key == null) return;
                bindLabel = key.GetValue("BindLabel", "F6")?.ToString() ?? "F6";
                bindMouse = Convert.ToInt32(key.GetValue("BindMouse", -1));
                string kn = key.GetValue("BindKey", "None")?.ToString() ?? "None";
                bindKey = Enum.TryParse<Keys>(kn, out var k) ? k : Keys.None;
                nudHours.SetValue(Convert.ToInt32(key.GetValue("Hours", 0)));
                nudMin.SetValue(Convert.ToInt32(key.GetValue("Min", 0)));
                nudSec.SetValue(Convert.ToInt32(key.GetValue("Sec", 0)));
                nudMs.SetValue(Convert.ToInt32(key.GetValue("Ms", 100)));
                nudLimit.SetValue(Convert.ToInt32(key.GetValue("Limit", 0)));
                bool left = Convert.ToBoolean(key.GetValue("ClickLeft", true));
                if (!left) { rdLeft.Uncheck(); rdRight.CheckedChanged -= RdRight_Changed; rdRight = new DarkRadio("Botão direito", true) { Location = rdRight.Location }; pnlButton.Controls.Add(rdRight); rdRight.CheckedChanged += RdRight_Changed; }
                lblBindValue.Text = bindLabel;
            }
            catch { }
        }

        void SaveSettings()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(REG_KEY);
                key.SetValue("BindLabel", bindLabel); key.SetValue("BindMouse", bindMouse); key.SetValue("BindKey", bindKey.ToString());
                key.SetValue("Hours", nudHours.Value); key.SetValue("Min", nudMin.Value); key.SetValue("Sec", nudSec.Value); key.SetValue("Ms", nudMs.Value);
                key.SetValue("Limit", nudLimit.Value); key.SetValue("ClickLeft", rdLeft.Checked);
            }
            catch { }
        }

        // ── UI ────────────────────────────────────────────────────────
        void InitUI()
        {
            Text = "JacAuto Clicker";
            ClientSize = new Size(400, 680);
            BackColor = BG; ForeColor = TEXT_PRI;
            Font = new Font("Segoe UI", 10f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true; KeyDown += Form_KeyDown;

            string icoPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
            if (File.Exists(icoPath)) Icon = new Icon(icoPath);

            int y = 18;

            // Título
            var lblTitle = MakeLabel("JacAuto Clicker", MARGIN, y, CWIDTH, 32);
            lblTitle.Font = new Font("Segoe UI", 15f, FontStyle.Bold); lblTitle.ForeColor = TEXT_PRI;
            Controls.Add(lblTitle);
            y += 44;

            // ── Bind ─────────────────────────────────────────────────
            pnlBind = MakeCard(y, 108);
            CardLabel(pnlBind, "Tecla / Botão de ativação", 14, 10);

            lblBindValue = MakeLabel("F6", 14, 33, 200, 28);
            lblBindValue.Font = new Font("Segoe UI", 13f, FontStyle.Bold); lblBindValue.ForeColor = ACCENT;
            pnlBind.Controls.Add(lblBindValue);

            btnBind = MakeButton("Alterar bind", CWIDTH - 144, 29, 130, 32);
            btnBind.Click += BtnBind_Click;
            pnlBind.Controls.Add(btnBind);

            lblBindHint = MakeLabel("Pressione uma tecla ou clique com o botão desejado... [ESC = cancelar]", 14, 70, CWIDTH - 28, 22);
            lblBindHint.ForeColor = AMBER; lblBindHint.Font = new Font("Segoe UI", 8.5f, FontStyle.Italic); lblBindHint.Visible = false;
            pnlBind.Controls.Add(lblBindHint);
            y += 122;

            // ── CPS ───────────────────────────────────────────────────
            pnlCps = MakeCard(y, 52);
            CardLabel(pnlCps, "Cliques por segundo (CPS)", 14, 8);
            lblCps = MakeLabel("0.0", 14, 26, 340, 20);
            lblCps.Font = new Font("Segoe UI", 11f, FontStyle.Bold); lblCps.ForeColor = ACCENT;
            pnlCps.Controls.Add(lblCps);
            y += 66;

            // ── Intervalo ─────────────────────────────────────────────
            pnlInterval = MakeCard(y, 96);
            CardLabel(pnlInterval, "Intervalo entre cliques", 14, 9);
            int nx = 14;
            foreach (var (lbl, max, ctrl) in new (string, int, DarkNumeric)[]
            {
                ("Horas", 23, nudHours = new DarkNumeric()),
                ("Min",   59, nudMin   = new DarkNumeric()),
                ("Seg",   59, nudSec   = new DarkNumeric()),
                ("ms",   999, nudMs    = new DarkNumeric()),
            })
            {
                var l = MakeLabel(lbl, nx, 32, 72, 16); l.ForeColor = TEXT_SEC; l.Font = new Font("Segoe UI", 8f);
                pnlInterval.Controls.Add(l);
                ctrl.Maximum = max; ctrl.Location = new Point(nx, 50);
                pnlInterval.Controls.Add(ctrl);
                nx += 84;
            }
            nudMs.SetValue(100);
            y += 110;

            // ── Botão do mouse ─────────────────────────────────────────
            pnlButton = MakeCard(y, 72);
            CardLabel(pnlButton, "Botão do mouse", 14, 9);
            rdLeft = new DarkRadio("Botão esquerdo", true) { Location = new Point(14, 34) };
            rdRight = new DarkRadio("Botão direito", false) { Location = new Point(180, 34) };
            rdLeft.CheckedChanged += RdLeft_Changed;
            rdRight.CheckedChanged += RdRight_Changed;
            pnlButton.Controls.AddRange(new Control[] { rdLeft, rdRight });
            y += 86;

            // ── Contagem + Limite (lado a lado) ────────────────────────
            pnlCountRow = MakeCard(y, 72);
            // metade esquerda: cliques
            CardLabel(pnlCountRow, "Cliques", 14, 9);
            lblCount = MakeLabel("0", 14, 30, 120, 22);
            lblCount.Font = new Font("Segoe UI", 11f, FontStyle.Bold); lblCount.ForeColor = GREEN;
            pnlCountRow.Controls.Add(lblCount);

            btnReset = new Button { Text = "Reset", Location = new Point(90, 28), Size = new Size(46, 22), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(35, 35, 50), ForeColor = TEXT_SEC, Font = new Font("Segoe UI", 7.5f), Cursor = Cursors.Hand };
            btnReset.FlatAppearance.BorderColor = BORDER; btnReset.FlatAppearance.BorderSize = 1;
            btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 70);
            btnReset.Click += (s, e) => { clickCount = 0; lblCount.Text = "0"; };
            pnlCountRow.Controls.Add(btnReset);

            // divisor vertical
            var div = new Panel { Location = new Point(CWIDTH / 2, 8), Size = new Size(1, 56), BackColor = BORDER };
            pnlCountRow.Controls.Add(div);

            // metade direita: limite
            int rx = CWIDTH / 2 + 12;
            CardLabel(pnlCountRow, "Limite  (0 = ∞)", rx, 9);
            nudLimit = new DarkNumeric { Maximum = 999999, Location = new Point(rx, 30) };
            pnlCountRow.Controls.Add(nudLimit);

            y += 86;

            // ── Status ────────────────────────────────────────────────
            pnlStatus = MakeCard(y, 52);
            CardLabel(pnlStatus, "Status", 14, 8);
            lblStatus = MakeLabel("Parado", 14, 26, 350, 20);
            lblStatus.ForeColor = RED; lblStatus.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            pnlStatus.Controls.Add(lblStatus);
            y += 66;

            // ── Iniciar/Parar ─────────────────────────────────────────
            btnStart = MakeRoundButton("Iniciar", MARGIN, y, CWIDTH, 46);
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);
        }

        void RdLeft_Changed(object s, EventArgs e) { rdRight.Uncheck(); SaveSettings(); }
        void RdRight_Changed(object s, EventArgs e) { rdLeft.Uncheck(); SaveSettings(); }

        // ── Timers ────────────────────────────────────────────────────
        void StartTimers()
        {
            pollTimer = new System.Windows.Forms.Timer { Interval = 30 };
            pollTimer.Tick += PollTimer_Tick; pollTimer.Start();

            cpsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            cpsTimer.Tick += (s, e) =>
            {
                cpsValue = cpsClicks;
                cpsClicks = 0;
                lblCps.Text = cpsValue.ToString("0.0") + " CPS";
            };
            cpsTimer.Start();
        }

        void PollTimer_Tick(object sender, EventArgs e)
        {
            if (isListening) return;
            bool triggered = bindMouse >= 0
                ? (GetAsyncKeyState(bindMouse) & 0x0001) != 0
                : bindKey != Keys.None && (GetAsyncKeyState((int)bindKey) & 0x0001) != 0;
            if (triggered) ToggleClicking();
        }

        // ── Bind ──────────────────────────────────────────────────────
        void BtnBind_Click(object sender, EventArgs e)
        {
            if (isRunning || isListening) return;
            isListening = true; lblBindValue.Visible = false; lblBindHint.Visible = true; btnBind.Enabled = false;
            mouseHookProc = MouseHookCallback;
            var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, mouseHookProc, GetModuleHandle(mod.ModuleName), 0);
        }

        IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && isListening)
            {
                int vk = (int)wParam switch { WM_LBUTTONDOWN => 1, WM_RBUTTONDOWN => 2, WM_MBUTTONDOWN => 4, _ => 0 };
                if (vk > 0)
                {
                    RemoveMouseHook();
                    bindMouse = vk; bindKey = Keys.None;
                    bindLabel = vk == 1 ? "Mouse Esq" : vk == 2 ? "Mouse Dir" : "Mouse Mid";
                    Invoke((Action)(() => FinishBindCapture()));
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }

        void RemoveMouseHook() { if (mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; } }

        void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isListening) return;
            if (e.KeyCode == Keys.Escape) { RemoveMouseHook(); FinishBindCapture(cancelled: true); }
            else { RemoveMouseHook(); bindKey = e.KeyCode; bindMouse = -1; bindLabel = e.KeyCode.ToString(); FinishBindCapture(); }
            e.Handled = true; e.SuppressKeyPress = true;
        }

        void FinishBindCapture(bool cancelled = false)
        {
            isListening = false; lblBindHint.Visible = false; lblBindValue.Visible = true; btnBind.Enabled = true;
            if (!cancelled) lblBindValue.Text = bindLabel;
            lblBindValue.ForeColor = ACCENT; SaveSettings();
        }

        // ── Start/Stop ────────────────────────────────────────────────
        void BtnStart_Click(object sender, EventArgs e) => ToggleClicking();

        void ToggleClicking()
        {
            if (isRunning) StopClicking();
            else StartClicking();
        }

        void StartClicking()
        {
            isRunning = true; UpdateStatusUI(); SaveSettings();
            limitCount = nudLimit.Value;

            int interval = nudHours.Value * 3600000 + nudMin.Value * 60000 + nudSec.Value * 1000 + nudMs.Value;
            if (interval < 1) interval = 1;
            bool leftClick = rdLeft.Checked;

            clickThread = new Thread(() =>
            {
                while (isRunning)
                {
                    if (limitCount > 0 && clickCount >= limitCount) { Invoke((Action)StopClicking); break; }
                    if (leftClick) { mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero); mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero); }
                    else { mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero); mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero); }
                    clickCount++; cpsClicks++;
                    Invoke((Action)(() => lblCount.Text = clickCount.ToString("N0")));
                    Thread.Sleep(interval);
                }
            });
            clickThread.IsBackground = true; clickThread.Start();
        }

        void StopClicking()
        {
            isRunning = false; clickThread?.Join(200); UpdateStatusUI();
        }

        void UpdateStatusUI()
        {
            if (InvokeRequired) { Invoke((Action)UpdateStatusUI); return; }
            if (isRunning)
            {
                lblStatus.Text = "Clicando..."; lblStatus.ForeColor = GREEN;
                btnStart.Text = "Parar"; btnStart.BackColor = Color.FromArgb(180, 40, 40);
            }
            else
            {
                lblStatus.Text = "Parado"; lblStatus.ForeColor = RED;
                btnStart.Text = "Iniciar"; btnStart.BackColor = ACCENT;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────
        RoundPanel MakeCard(int top, int height)
        {
            var p = new RoundPanel { Location = new Point(MARGIN, top), Size = new Size(CWIDTH, height), BackColor = CARD };
            Controls.Add(p);
            return p;
        }

        void CardLabel(Control parent, string text, int x, int y)
        {
            var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent, ForeColor = TEXT_SEC, Font = new Font("Segoe UI", 8f) };
            parent.Controls.Add(l);
        }

        Label MakeLabel(string text, int x, int y, int w, int h) =>
            new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.Transparent };

        Button MakeButton(string text, int x, int y, int w, int h)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = SURFACE, ForeColor = TEXT_PRI, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = BORDER; b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 38, 54);
            return b;
        }

        Button MakeRoundButton(string text, int x, int y, int w, int h)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = ACCENT, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ACCENT_DARK;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 53, 140);
            b.Region = new Region(RoundPanel.RoundRect(new Rectangle(0, 0, w, h), 8));
            return b;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            isRunning = false; RemoveMouseHook(); pollTimer?.Stop(); cpsTimer?.Stop(); SaveSettings();
            base.OnFormClosed(e);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}