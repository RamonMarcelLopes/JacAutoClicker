using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using FontAwesome.Sharp;
using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.Entities;
using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;
using JacaAutoClicker.Presentation.Controls;

namespace JacaAutoClicker.Presentation;

public class MainForm : Form
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private static readonly Color CARD = Color.FromArgb(11, 18, 25);
    private static readonly Color INNER_CARD = Color.FromArgb(9, 14, 20);
    private static readonly Color BORDER = Color.FromArgb(34, 44, 55);
    private static readonly Color TEXT = Color.FromArgb(227, 237, 244);
    private static readonly Color TEXT_MUTED = Color.FromArgb(121, 137, 148);
    private static readonly Color PRIMARY = Color.FromArgb(0, 206, 182);
    private static readonly Color PRIMARY_FG = Color.FromArgb(0, 8, 11);
    private static readonly Color DESTRUCTIVE = Color.FromArgb(214, 80, 76);
    private static readonly Color DESTRUCTIVE_FG = Color.FromArgb(255, 246, 246);
    private static readonly Color RUNNING_DOT = Color.FromArgb(52, 211, 153);
    private static readonly Color IDLE_DOT = Color.FromArgb(90, 100, 110);
    private static readonly Color AMBER = Color.FromArgb(251, 191, 36);

    private static readonly Color CLOSE_DOT = Color.FromArgb(255, 95, 87);
    private static readonly Color CLOSE_ICON = Color.FromArgb(104, 27, 23);
    private static readonly Color MINIMIZE_DOT = Color.FromArgb(254, 188, 46);
    private static readonly Color MINIMIZE_ICON = Color.FromArgb(107, 67, 0);
    private static readonly Color DISABLED_DOT = Color.FromArgb(18, 70, 32);
    private static readonly Color DISABLED_ICON = Color.FromArgb(14, 46, 20);

    private const int MARGIN = 21;
    private const int CWIDTH = 428;
    private const int CARD_RADIUS = 14;
    private const int ROW_GAP = 14;
    private const int HEADER_HEIGHT = 108;

    private readonly StartClickingUseCase _startClickingUseCase;
    private readonly StopClickingUseCase _stopClickingUseCase;
    private readonly ResetClickCountUseCase _resetClickCountUseCase;
    private readonly CaptureTriggerUseCase _captureTriggerUseCase;
    private readonly LoadConfigUseCase _loadConfigUseCase;
    private readonly SaveConfigUseCase _saveConfigUseCase;
    private readonly ITriggerListener _triggerListener;
    private readonly ClickSession _clickSession;

    private Trigger _currentTrigger = ClickerConfig.Default.Trigger;
    private bool _isCapturingTrigger;
    private CancellationTokenSource? _clickingCts;
    private Task? _clickingTask;

    private System.Windows.Forms.Timer pollTimer = null!;
    private System.Windows.Forms.Timer cpsTimer = null!;

    private Label lblBindValue = null!, lblBindHint = null!, lblStatus = null!, lblStatusDot = null!, lblCount = null!, lblCps = null!, lblIntervalSummary = null!, lblShortcutHint = null!, lblShortcutValue = null!, lblClicksCaption = null!;
    private RoundPanel shortcutChip = null!;
    private RoundPanel statusPill = null!;
    private int _shortcutFooterY;
    private Button btnBind = null!, btnStart = null!;
    private IconButton btnReset = null!;
    private DarkNumeric nudHours = null!, nudMin = null!, nudSec = null!, nudMs = null!, nudLimit = null!;
    private SegmentedToggle mouseButtonToggle = null!;

    public MainForm(
        StartClickingUseCase startClickingUseCase,
        StopClickingUseCase stopClickingUseCase,
        ResetClickCountUseCase resetClickCountUseCase,
        CaptureTriggerUseCase captureTriggerUseCase,
        LoadConfigUseCase loadConfigUseCase,
        SaveConfigUseCase saveConfigUseCase,
        ITriggerListener triggerListener,
        ClickSession clickSession)
    {
        _startClickingUseCase = startClickingUseCase;
        _stopClickingUseCase = stopClickingUseCase;
        _resetClickCountUseCase = resetClickCountUseCase;
        _captureTriggerUseCase = captureTriggerUseCase;
        _loadConfigUseCase = loadConfigUseCase;
        _saveConfigUseCase = saveConfigUseCase;
        _triggerListener = triggerListener;
        _clickSession = clickSession;

        InitUI();
        LoadSettings();
        StartTimers();
        UpdateStatusUI();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            return cp;
        }
    }

    // ── Settings ──────────────────────────────────────────────────
    private void LoadSettings()
    {
        var config = _loadConfigUseCase.Execute();
        _currentTrigger = config.Trigger;
        lblBindValue.Text = TriggerLabelFormatter.Format(_currentTrigger);
        RefreshShortcutFooter();

        nudHours.SetValue(config.Interval.Hours);
        nudMin.SetValue(config.Interval.Minutes);
        nudSec.SetValue(config.Interval.Seconds);
        nudMs.SetValue(config.Interval.Milliseconds);
        nudLimit.SetValue(config.ClickLimit);
        UpdateIntervalSummary();
        UpdateClicksCaption();

        mouseButtonToggle.SetSelectedIndex(config.ClickButton == ClickButton.Right ? 1 : 0);
    }

    private void SaveSettings() => _saveConfigUseCase.Execute(BuildConfigFromUi());

    private ClickerConfig BuildConfigFromUi()
    {
        var interval = new TimeSpan(nudHours.Value, nudMin.Value, nudSec.Value) + TimeSpan.FromMilliseconds(nudMs.Value);
        var clickButton = mouseButtonToggle.SelectedIndex == 1 ? ClickButton.Right : ClickButton.Left;
        return new ClickerConfig(_currentTrigger, interval, clickButton, nudLimit.Value);
    }

    private void UpdateIntervalSummary() =>
        lblIntervalSummary.Text = $"{nudHours.Value}h {nudMin.Value:00}m {nudSec.Value:00}s {nudMs.Value}ms";

    private void UpdateClicksCaption() =>
        lblClicksCaption.Text = nudLimit.Value > 0 ? $"limite: {nudLimit.Value}" : "sem limite definido";

    // ── UI ────────────────────────────────────────────────────────
    private void InitUI()
    {
        Text = "JacAuto Clicker";
        ClientSize = new Size(470, 730);
        BackColor = CARD; ForeColor = TEXT;
        Font = new Font("Segoe UI", 10f);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false; MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        string icoPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
        if (File.Exists(icoPath)) Icon = new System.Drawing.Icon(icoPath);

        Region = new Region(RoundPanel.RoundRect(new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), 22));

        BuildTitleBar(icoPath);
        BuildContent();
    }

    private void BuildTitleBar(string icoPath)
    {
        int dotSize = 17, dotContainer = 22, dotGap = 9;
        int dotsRight = ClientSize.Width - MARGIN;
        int dotY = 14;

        var closeBtn = MakeDotButton(new Point(dotsRight - dotContainer * 3 - dotGap * 2, dotY), IconChar.Xmark, CLOSE_DOT, CLOSE_ICON, dotContainer, dotSize);
        closeBtn.Click += (s, e) => Close();

        var minimizeBtn = MakeDotButton(new Point(dotsRight - dotContainer * 2 - dotGap, dotY), IconChar.Minus, MINIMIZE_DOT, MINIMIZE_ICON, dotContainer, dotSize);
        minimizeBtn.Click += (s, e) => WindowState = FormWindowState.Minimized;

        var fullscreenBtn = MakeDotButton(new Point(dotsRight - dotContainer, dotY), IconChar.Expand, DISABLED_DOT, DISABLED_ICON, dotContainer, dotSize);
        fullscreenBtn.Enabled = false;
        fullscreenBtn.Cursor = Cursors.Default;

        int headerY = dotY + dotContainer + 1;
        const int iconBoxSize = 45;
        int iconCenterY = headerY + iconBoxSize / 2;

        var iconBox = new RoundPanel { Location = new Point(MARGIN, headerY), Size = new Size(iconBoxSize, iconBoxSize), Radius = 13, BackColor = PRIMARY, ShowBorder = false };
        Controls.Add(iconBox);
        if (File.Exists(icoPath))
        {
            const int imgSize = 40;
            int imgInset = (iconBoxSize - imgSize) / 2;
            var appIconBox = new PictureBox { Image = LoadIconSmoothed(icoPath, imgSize), SizeMode = PictureBoxSizeMode.CenterImage, Location = new Point(imgInset, imgInset), Size = new Size(imgSize, imgSize), BackColor = Color.Transparent };
            iconBox.Controls.Add(appIconBox);
        }

        const int titleBlockHeight = 22 + 4 + 16;
        int titleBlockTop = iconCenterY - titleBlockHeight / 2;

        var lblTitle = MakeLabel("Jacaclicker", MARGIN + iconBoxSize + 12, titleBlockTop, 240, 22);
        lblTitle.Font = new Font("Cascadia Mono", 13f, FontStyle.Bold); lblTitle.ForeColor = TEXT;
        Controls.Add(lblTitle);

        var lblSubtitle = MakeLabel("CLICK UTILITY", MARGIN + iconBoxSize + 12, titleBlockTop + 22 + 4, 240, 16);
        lblSubtitle.Font = new Font("Segoe UI", 8f); lblSubtitle.ForeColor = TEXT_MUTED;
        Controls.Add(lblSubtitle);

        statusPill = new RoundPanel { Size = new Size(26, 29), Radius = 14, BackColor = Color.FromArgb(20, 27, 35), BorderColor = BORDER };
        statusPill.Location = new Point(ClientSize.Width - MARGIN - statusPill.Width, iconCenterY - statusPill.Height / 2);
        Controls.Add(statusPill);

        lblStatusDot = new Label { Bounds = new Rectangle(11, 10, 8, 8), BackColor = Color.Transparent };
        using (var dotPath = new GraphicsPath()) { dotPath.AddEllipse(0, 0, 8, 8); lblStatusDot.Region = new Region(dotPath); }
        statusPill.Controls.Add(lblStatusDot);

        lblStatus = new Label { Text = "Parado", Location = new Point(25, 5), Height = 19, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f), ForeColor = TEXT_MUTED, BackColor = Color.Transparent };
        statusPill.Controls.Add(lblStatus);
        LayoutStatusPill();

        var headerBorder = new Panel { Location = new Point(0, HEADER_HEIGHT), Size = new Size(ClientSize.Width, 1), BackColor = BORDER };
        Controls.Add(headerBorder);

        var dragArea = new Panel { Location = new Point(0, 0), Size = new Size(ClientSize.Width, HEADER_HEIGHT), BackColor = Color.Transparent };
        dragArea.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) StartWindowDrag(); };
        Controls.Add(dragArea);
        dragArea.SendToBack();
    }

    private static Bitmap LoadIconSmoothed(string icoPath, int targetSize)
    {
        using var icon = new System.Drawing.Icon(icoPath);
        using var source = icon.ToBitmap();
        var result = new Bitmap(targetSize, targetSize);
        using var g = Graphics.FromImage(result);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, 0, 0, targetSize, targetSize);
        return result;
    }

    private void StartWindowDrag()
    {
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }

    /// <summary>
    /// `containerSlot` is the reserved footprint used for spacing between dots; the visible circle
    /// (`circleSize`) is centered inside it with a margin, so anti-aliasing/rounding at the circle's
    /// own edge never touches the control's outer bounds.
    /// </summary>
    private IconButton MakeDotButton(Point containerSlot, IconChar icon, Color dotColor, Color iconColor, int containerSlotSize, int circleSize)
    {
        int inset = (containerSlotSize - circleSize) / 2;
        var location = new Point(containerSlot.X + inset, containerSlot.Y + inset);

        // Added before `backing` on purpose: earlier-added controls render in front in WinForms z-order,
        // and this one must stay clickable/in front of the purely-decorative antialiased circle behind it.
        var b = new IconButton
        {
            Location = location,
            Size = new Size(circleSize, circleSize),
            FlatStyle = FlatStyle.Flat,
            BackColor = dotColor,
            IconChar = icon,
            IconColor = Color.Transparent,
            IconSize = circleSize - 6,
            Cursor = Cursors.Hand,
            TabStop = false,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = dotColor;
        b.FlatAppearance.MouseDownBackColor = dotColor;
        b.Region = new Region(RoundPanel.RoundRect(new Rectangle(0, 0, circleSize, circleSize), circleSize / 2));
        b.MouseEnter += (s, e) => { if (b.Enabled) b.IconColor = iconColor; };
        b.MouseLeave += (s, e) => b.IconColor = Color.Transparent;
        Controls.Add(b);

        var backing = new Panel { Location = location, Size = new Size(circleSize, circleSize), BackColor = Color.Transparent };
        backing.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(dotColor);
            e.Graphics.FillEllipse(brush, 0, 0, circleSize, circleSize);
        };
        Controls.Add(backing);

        return b;
    }

    private void BuildContent()
    {
        int y = HEADER_HEIGHT + ROW_GAP;
        int halfWidth = (CWIDTH - 12) / 2;

        // ── CPS + Cliques ────────────────────────────────────────
        const int statCardHeight = 113;
        var cpsCard = MakeCard(MARGIN, y, halfWidth, statCardHeight);
        SectionCaption(cpsCard, "CPS ATUAL", 16, 14);
        lblCps = MakeLabel("0.0", 14, 34, halfWidth - 28, 40);
        lblCps.Font = new Font("Cascadia Mono", 24f, FontStyle.Bold); lblCps.ForeColor = PRIMARY;
        cpsCard.Controls.Add(lblCps);
        var lblCpsCaption = MakeLabel("cliques / segundo", 16, 84, halfWidth - 32, 16);
        lblCpsCaption.Font = new Font("Segoe UI", 8f); lblCpsCaption.ForeColor = TEXT_MUTED;
        cpsCard.Controls.Add(lblCpsCaption);

        var clicksCard = MakeCard(MARGIN + halfWidth + 12, y, halfWidth, statCardHeight);
        SectionCaption(clicksCard, "CLIQUES DADOS", 16, 14);
        lblCount = MakeLabel("0", 14, 34, halfWidth - 28, 40);
        lblCount.Font = new Font("Cascadia Mono", 24f, FontStyle.Bold); lblCount.ForeColor = TEXT;
        clicksCard.Controls.Add(lblCount);
        lblClicksCaption = MakeLabel("sem limite definido", 16, 84, halfWidth - 32, 16);
        lblClicksCaption.Font = new Font("Segoe UI", 8f); lblClicksCaption.ForeColor = TEXT_MUTED;
        clicksCard.Controls.Add(lblClicksCaption);

        y += statCardHeight + ROW_GAP;

        // ── Tecla de ativação ────────────────────────────────────
        const int bindCardHeight = 101;
        var bindCard = MakeCard(MARGIN, y, CWIDTH, bindCardHeight);
        SectionHeader(bindCard, IconChar.Keyboard, "Tecla de ativação", 16, 14);
        var lblBindHeaderHint = MakeLabel("pressione para alternar", 0, 16, CWIDTH - 32, 14);
        lblBindHeaderHint.TextAlign = ContentAlignment.MiddleRight;
        lblBindHeaderHint.Font = new Font("Segoe UI", 7.5f); lblBindHeaderHint.ForeColor = TEXT_MUTED;
        bindCard.Controls.Add(lblBindHeaderHint);

        var bindBox = new RoundPanel { Location = new Point(16, 42), Size = new Size(CWIDTH - 32 - 108, 40), Radius = 10, BackColor = INNER_CARD, BorderColor = BORDER };
        bindCard.Controls.Add(bindBox);
        lblBindValue = new Label { Text = "F6", Dock = DockStyle.Fill, Padding = new Padding(14, 0, 0, 0), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Cascadia Mono", 12f, FontStyle.Bold), ForeColor = TEXT, BackColor = Color.Transparent };
        bindBox.Controls.Add(lblBindValue);

        btnBind = MakeOutlineButton(bindCard, "Alterar", CWIDTH - 16 - 96, 42, 96, 40);
        btnBind.Click += BtnBind_Click;

        lblBindHint = MakeLabel("Pressione uma tecla ou clique do mouse... [ESC cancela]", 16, 84, CWIDTH - 32, 16);
        lblBindHint.ForeColor = AMBER; lblBindHint.Font = new Font("Segoe UI", 8f, FontStyle.Italic); lblBindHint.Visible = false;
        bindCard.Controls.Add(lblBindHint);

        y += bindCardHeight + ROW_GAP;

        // ── Intervalo entre cliques ──────────────────────────────
        const int intervalCardHeight = 119;
        var intervalCard = MakeCard(MARGIN, y, CWIDTH, intervalCardHeight);
        SectionHeader(intervalCard, IconChar.SlidersH, "Intervalo entre cliques", 16, 14);
        lblIntervalSummary = MakeLabel("0h 00m 00s 100ms", 0, 16, CWIDTH - 32, 16);
        lblIntervalSummary.TextAlign = ContentAlignment.MiddleRight;
        lblIntervalSummary.Font = new Font("Cascadia Mono", 8.5f); lblIntervalSummary.ForeColor = TEXT_MUTED;
        intervalCard.Controls.Add(lblIntervalSummary);

        int nx = 16;
        int numWidth = (CWIDTH - 32 - 3 * 12) / 4;
        foreach (var (lbl, max, ctrl) in new (string, int, DarkNumeric)[]
        {
            ("HR",  23, nudHours = new DarkNumeric()),
            ("MIN", 59, nudMin   = new DarkNumeric()),
            ("SEG", 59, nudSec   = new DarkNumeric()),
            ("MS",  999, nudMs   = new DarkNumeric()),
        })
        {
            var l = MakeLabel(lbl, nx, 42, numWidth, 14); l.ForeColor = TEXT_MUTED; l.Font = new Font("Segoe UI", 7.5f);
            intervalCard.Controls.Add(l);
            ctrl.Maximum = max; ctrl.Size = new Size(numWidth, 44); ctrl.Location = new Point(nx, 62);
            ctrl.ValueChanged += (s, e) => { UpdateIntervalSummary(); SaveSettings(); };
            intervalCard.Controls.Add(ctrl);
            nx += numWidth + 12;
        }
        nudMs.SetValue(100);

        y += intervalCardHeight + ROW_GAP;

        // ── Botão do mouse + Limite ──────────────────────────────
        const int lowerRowHeight = 120;
        var buttonCard = MakeCard(MARGIN, y, halfWidth, lowerRowHeight);
        SectionHeader(buttonCard, IconChar.MousePointer, "Botão do mouse", 16, 14);
        mouseButtonToggle = new SegmentedToggle("Esquerdo", "Direito") { Location = new Point(16, 48), Size = new Size(halfWidth - 32, 40) };
        mouseButtonToggle.SelectedIndexChanged += (s, e) => SaveSettings();
        buttonCard.Controls.Add(mouseButtonToggle);

        var limitCard = MakeCard(MARGIN + halfWidth + 12, y, halfWidth, lowerRowHeight);
        PlainHeader(limitCard, "Limite de cliques", 16, 14);
        nudLimit = new DarkNumeric { Maximum = 999999, Location = new Point(16, 38), Size = new Size(halfWidth - 32, 44) };
        nudLimit.ValueChanged += (s, e) => { SaveSettings(); UpdateClicksCaption(); };
        limitCard.Controls.Add(nudLimit);
        var lblLimitHint = MakeLabel("0 = ilimitado", 16, 90, halfWidth - 32, 14);
        lblLimitHint.Font = new Font("Segoe UI", 7.5f); lblLimitHint.ForeColor = TEXT_MUTED;
        limitCard.Controls.Add(lblLimitHint);

        y += lowerRowHeight + ROW_GAP;

        // ── Iniciar/Parar + Reset ────────────────────────────────
        const int actionHeight = 44;
        btnStart = MakeRoundButton("⬤  Iniciar autoclick", MARGIN, y, CWIDTH - actionHeight - 8, actionHeight);
        btnStart.Click += async (s, e) => await ToggleClickingAsync();
        Controls.Add(btnStart);

        var resetFrame = new RoundPanel { Location = new Point(MARGIN + CWIDTH - actionHeight, y), Size = new Size(actionHeight, actionHeight), Radius = 12, BackColor = CARD, BorderColor = BORDER };
        Controls.Add(resetFrame);

        btnReset = new IconButton
        {
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            IconChar = IconChar.RotateLeft,
            IconColor = TEXT_MUTED,
            IconSize = 18,
            Cursor = Cursors.Hand,
            TabStop = false,
        };
        btnReset.FlatAppearance.BorderSize = 0;
        btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 35, 44);
        btnReset.Click += (s, e) => { _resetClickCountUseCase.Execute(_clickSession); lblCount.Text = "0"; };
        resetFrame.Controls.Add(btnReset);

        y += actionHeight + ROW_GAP;

        BuildShortcutFooter(y);
    }

    private static readonly Font ShortcutPrefixFont = new("Segoe UI", 8.5f);
    private static readonly Font ShortcutValueFont = new("Cascadia Mono", 8.5f, FontStyle.Bold);
    private const string ShortcutPrefix = "Atalho ativo: ";

    private void BuildShortcutFooter(int y)
    {
        _shortcutFooterY = y;

        lblShortcutHint = new Label { Text = ShortcutPrefix, AutoSize = true, Font = ShortcutPrefixFont, ForeColor = TEXT_MUTED, BackColor = Color.Transparent };
        Controls.Add(lblShortcutHint);

        shortcutChip = new RoundPanel { Size = new Size(10, 23), Radius = 6, BackColor = INNER_CARD, BorderColor = BORDER };
        Controls.Add(shortcutChip);
        lblShortcutValue = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = ShortcutValueFont, ForeColor = TEXT, BackColor = Color.Transparent };
        shortcutChip.Controls.Add(lblShortcutValue);

        RefreshShortcutFooter();
    }

    private void RefreshShortcutFooter()
    {
        var value = TriggerLabelFormatter.Format(_currentTrigger);
        lblShortcutValue.Text = value;

        var prefixSize = TextRenderer.MeasureText(ShortcutPrefix, ShortcutPrefixFont);
        var chipTextSize = TextRenderer.MeasureText(value, ShortcutValueFont);
        int chipWidth = chipTextSize.Width + 16;
        int totalWidth = prefixSize.Width + 6 + chipWidth;
        int startX = (ClientSize.Width - totalWidth) / 2;

        lblShortcutHint.Location = new Point(startX, _shortcutFooterY + 3);
        shortcutChip.Location = new Point(startX + prefixSize.Width + 6, _shortcutFooterY);
        shortcutChip.Size = new Size(chipWidth, 20);
    }

    // ── Timers ────────────────────────────────────────────────────
    private void StartTimers()
    {
        pollTimer = new System.Windows.Forms.Timer { Interval = 30 };
        pollTimer.Tick += PollTimer_Tick; pollTimer.Start();

        cpsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        cpsTimer.Tick += (s, e) =>
        {
            _clickSession.TickClickRate();
            lblCps.Text = _clickSession.ClickRate.ToString("0.0");
        };
        cpsTimer.Start();
    }

    private async void PollTimer_Tick(object? sender, EventArgs e)
    {
        if (_isCapturingTrigger) return;
        if (_triggerListener.IsTriggered(_currentTrigger)) await ToggleClickingAsync();
    }

    // ── Bind ──────────────────────────────────────────────────────
    private void BtnBind_Click(object? sender, EventArgs e)
    {
        if (_clickingCts is not null || _isCapturingTrigger) return;
        _isCapturingTrigger = true; lblBindValue.Visible = false; lblBindHint.Visible = true; btnBind.Enabled = false;
        _captureTriggerUseCase.Execute(
            onCaptured: trigger => FinishBindCapture(trigger),
            onCancelled: () => FinishBindCapture(null));
    }

    private void FinishBindCapture(Trigger? capturedTrigger)
    {
        _isCapturingTrigger = false; lblBindHint.Visible = false; lblBindValue.Visible = true; btnBind.Enabled = true;
        if (capturedTrigger is not null)
        {
            _currentTrigger = capturedTrigger;
            lblBindValue.Text = TriggerLabelFormatter.Format(_currentTrigger);
            RefreshShortcutFooter();
        }
        SaveSettings();
    }

    // ── Start/Stop ────────────────────────────────────────────────
    private async Task ToggleClickingAsync()
    {
        if (_clickingCts is not null)
        {
            var cts = _clickingCts; var task = _clickingTask!;
            _clickingCts = null; _clickingTask = null;
            await _stopClickingUseCase.ExecuteAsync(cts, task);
            UpdateStatusUI();
            return;
        }

        SaveSettings();
        var config = BuildConfigFromUi();
        _clickingCts = new CancellationTokenSource();
        var progress = new Progress<ClickSession>(session => lblCount.Text = session.ClickCount.ToString("N0"));
        UpdateStatusUI();

        _clickingTask = _startClickingUseCase.ExecuteAsync(config, _clickSession, progress, _clickingCts.Token);
        await _clickingTask;

        _clickingCts = null; _clickingTask = null;
        UpdateStatusUI();
    }

    private void UpdateStatusUI()
    {
        bool isRunning = _clickingCts is not null;
        if (isRunning)
        {
            lblStatus.Text = "Rodando"; lblStatus.ForeColor = TEXT;
            lblStatusDot.BackColor = RUNNING_DOT;
            btnStart.Text = "⬤  Parar autoclick"; btnStart.BackColor = DESTRUCTIVE; btnStart.ForeColor = DESTRUCTIVE_FG;
        }
        else
        {
            lblStatus.Text = "Parado"; lblStatus.ForeColor = TEXT_MUTED;
            lblStatusDot.BackColor = IDLE_DOT;
            btnStart.Text = "⬤  Iniciar autoclick"; btnStart.BackColor = PRIMARY; btnStart.ForeColor = PRIMARY_FG;
        }
        LayoutStatusPill();
    }

    private void LayoutStatusPill()
    {
        const int textX = 25, rightPadding = 11;
        var textWidth = TextRenderer.MeasureText(lblStatus.Text, lblStatus.Font).Width;
        lblStatus.Width = textWidth;

        int pillWidth = textX + textWidth + rightPadding;
        statusPill.Size = new Size(pillWidth, statusPill.Height);
        statusPill.Location = new Point(ClientSize.Width - MARGIN - pillWidth, statusPill.Location.Y);
    }

    // ── Helpers ───────────────────────────────────────────────────
    private RoundPanel MakeCard(int x, int y, int width, int height)
    {
        var p = new RoundPanel { Location = new Point(x, y), Size = new Size(width, height), Radius = CARD_RADIUS, BackColor = INNER_CARD, BorderColor = BORDER };
        Controls.Add(p);
        return p;
    }

    private void SectionCaption(Control parent, string text, int x, int y)
    {
        var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent, ForeColor = TEXT_MUTED, Font = new Font("Segoe UI", 7f, FontStyle.Bold) };
        parent.Controls.Add(l);
    }

    private void SectionHeader(Control parent, IconChar icon, string text, int x, int y)
    {
        var iconBox = new IconPictureBox { IconChar = icon, IconColor = PRIMARY, IconSize = 14, Location = new Point(x, y), Size = new Size(16, 16), BackColor = Color.Transparent };
        parent.Controls.Add(iconBox);
        var l = new Label { Text = text, Location = new Point(x + 20, y), AutoSize = true, BackColor = Color.Transparent, ForeColor = TEXT, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        parent.Controls.Add(l);
    }

    private void PlainHeader(Control parent, string text, int x, int y)
    {
        var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent, ForeColor = TEXT, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        parent.Controls.Add(l);
    }

    private Label MakeLabel(string text, int x, int y, int w, int h) =>
        new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.Transparent, ForeColor = TEXT };

    private Button MakeOutlineButton(Control parent, string text, int x, int y, int w, int h)
    {
        var frame = new RoundPanel { Location = new Point(x, y), Size = new Size(w, h), Radius = 10, BackColor = INNER_CARD, BorderColor = BORDER };
        parent.Controls.Add(frame);

        var b = new Button { Text = text, Dock = DockStyle.Fill, BackColor = Color.Transparent, ForeColor = TEXT_MUTED, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand, TabStop = false };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 35, 44);
        frame.Controls.Add(b);
        return b;
    }

    private Button MakeRoundButton(string text, int x, int y, int w, int h)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = PRIMARY, ForeColor = PRIMARY_FG, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10.5f), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0;
        b.Region = new Region(RoundPanel.RoundRect(new Rectangle(0, 0, w, h), 12));
        return b;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clickingCts?.Cancel();
        _triggerListener.EndCapture();
        pollTimer?.Stop(); cpsTimer?.Stop();
        SaveSettings();
        base.OnFormClosed(e);
    }
}
