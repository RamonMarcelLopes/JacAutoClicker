using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.Entities;
using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;
using JacaAutoClicker.Presentation.Controls;

namespace JacaAutoClicker.Presentation;

public class MainForm : Form
{
    private static readonly Color BG = Color.FromArgb(15, 15, 22);
    private static readonly Color SURFACE = Color.FromArgb(24, 24, 34);
    private static readonly Color CARD = Color.FromArgb(28, 28, 40);
    private static readonly Color ACCENT = Color.FromArgb(99, 102, 241);
    private static readonly Color ACCENT_DARK = Color.FromArgb(67, 70, 172);
    private static readonly Color TEXT_PRI = Color.FromArgb(220, 220, 240);
    private static readonly Color TEXT_SEC = Color.FromArgb(110, 110, 145);
    private static readonly Color BORDER = Color.FromArgb(50, 50, 68);
    private static readonly Color GREEN = Color.FromArgb(52, 211, 153);
    private static readonly Color RED = Color.FromArgb(248, 113, 113);
    private static readonly Color AMBER = Color.FromArgb(251, 191, 36);

    private const int MARGIN = 13;
    private const int CWIDTH = 374;

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

    private RoundPanel pnlBind = null!, pnlCps = null!, pnlInterval = null!, pnlButton = null!, pnlCountRow = null!, pnlStatus = null!;
    private Button btnBind = null!, btnStart = null!, btnReset = null!;
    private Label lblBindValue = null!, lblBindHint = null!, lblStatus = null!, lblCount = null!, lblCps = null!;
    private DarkNumeric nudHours = null!, nudMin = null!, nudSec = null!, nudMs = null!, nudLimit = null!;
    private DarkRadio rdLeft = null!, rdRight = null!;

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
    }

    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); DwmHelper.Dark(Handle); }

    // ── Settings ──────────────────────────────────────────────────
    private void LoadSettings()
    {
        var config = _loadConfigUseCase.Execute();
        _currentTrigger = config.Trigger;
        lblBindValue.Text = TriggerLabelFormatter.Format(_currentTrigger);

        nudHours.SetValue(config.Interval.Hours);
        nudMin.SetValue(config.Interval.Minutes);
        nudSec.SetValue(config.Interval.Seconds);
        nudMs.SetValue(config.Interval.Milliseconds);
        nudLimit.SetValue(config.ClickLimit);

        if (config.ClickButton == ClickButton.Right)
        {
            rdLeft.Uncheck();
            rdRight.Check();
        }
    }

    private void SaveSettings() => _saveConfigUseCase.Execute(BuildConfigFromUi());

    private ClickerConfig BuildConfigFromUi()
    {
        var interval = new TimeSpan(nudHours.Value, nudMin.Value, nudSec.Value) + TimeSpan.FromMilliseconds(nudMs.Value);
        var clickButton = rdLeft.Checked ? ClickButton.Left : ClickButton.Right;
        return new ClickerConfig(_currentTrigger, interval, clickButton, nudLimit.Value);
    }

    // ── UI ────────────────────────────────────────────────────────
    private void InitUI()
    {
        Text = "JacAuto Clicker";
        ClientSize = new Size(400, 680);
        BackColor = BG; ForeColor = TEXT_PRI;
        Font = new Font("Segoe UI", 10f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;

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
        CardLabel(pnlCountRow, "Cliques", 14, 9);
        lblCount = MakeLabel("0", 14, 30, 120, 22);
        lblCount.Font = new Font("Segoe UI", 11f, FontStyle.Bold); lblCount.ForeColor = GREEN;
        pnlCountRow.Controls.Add(lblCount);

        btnReset = new Button { Text = "Reset", Location = new Point(90, 28), Size = new Size(46, 22), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(35, 35, 50), ForeColor = TEXT_SEC, Font = new Font("Segoe UI", 7.5f), Cursor = Cursors.Hand };
        btnReset.FlatAppearance.BorderColor = BORDER; btnReset.FlatAppearance.BorderSize = 1;
        btnReset.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 70);
        btnReset.Click += (s, e) => { _resetClickCountUseCase.Execute(_clickSession); lblCount.Text = "0"; };
        pnlCountRow.Controls.Add(btnReset);

        var div = new Panel { Location = new Point(CWIDTH / 2, 8), Size = new Size(1, 56), BackColor = BORDER };
        pnlCountRow.Controls.Add(div);

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
        btnStart.Click += async (s, e) => await ToggleClickingAsync();
        Controls.Add(btnStart);
    }

    private void RdLeft_Changed(object? s, EventArgs e) { rdRight.Uncheck(); SaveSettings(); }
    private void RdRight_Changed(object? s, EventArgs e) { rdLeft.Uncheck(); SaveSettings(); }

    // ── Timers ────────────────────────────────────────────────────
    private void StartTimers()
    {
        pollTimer = new System.Windows.Forms.Timer { Interval = 30 };
        pollTimer.Tick += PollTimer_Tick; pollTimer.Start();

        cpsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        cpsTimer.Tick += (s, e) =>
        {
            _clickSession.TickClickRate();
            lblCps.Text = _clickSession.ClickRate.ToString("0.0") + " CPS";
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
        }
        lblBindValue.ForeColor = ACCENT; SaveSettings();
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
    private RoundPanel MakeCard(int top, int height)
    {
        var p = new RoundPanel { Location = new Point(MARGIN, top), Size = new Size(CWIDTH, height), BackColor = CARD };
        Controls.Add(p);
        return p;
    }

    private void CardLabel(Control parent, string text, int x, int y)
    {
        var l = new Label { Text = text, Location = new Point(x, y), AutoSize = true, BackColor = Color.Transparent, ForeColor = TEXT_SEC, Font = new Font("Segoe UI", 8f) };
        parent.Controls.Add(l);
    }

    private Label MakeLabel(string text, int x, int y, int w, int h) =>
        new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.Transparent };

    private Button MakeButton(string text, int x, int y, int w, int h)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = SURFACE, ForeColor = TEXT_PRI, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = BORDER; b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 38, 54);
        return b;
    }

    private Button MakeRoundButton(string text, int x, int y, int w, int h)
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
        _clickingCts?.Cancel();
        _triggerListener.EndCapture();
        pollTimer?.Stop(); cpsTimer?.Stop();
        SaveSettings();
        base.OnFormClosed(e);
    }
}
