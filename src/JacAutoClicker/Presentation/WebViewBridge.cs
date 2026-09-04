using System.Text.Json;
using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.Entities;
using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;
using Microsoft.Web.WebView2.WinForms;

namespace JacaAutoClicker.Presentation;

/// <summary>
/// Owns clicker orchestration for the web UI: receives commands posted from the page via
/// `window.chrome.webview.postMessage`, drives the same use cases the WinForms UI used to call
/// directly, and pushes the resulting state back into the page.
/// </summary>
public sealed class WebViewBridge : IDisposable
{
    private readonly StartClickingUseCase _startClickingUseCase;
    private readonly StopClickingUseCase _stopClickingUseCase;
    private readonly ResetClickCountUseCase _resetClickCountUseCase;
    private readonly CaptureTriggerUseCase _captureTriggerUseCase;
    private readonly SaveConfigUseCase _saveConfigUseCase;
    private readonly ITriggerListener _triggerListener;
    private readonly ClickSession _clickSession;

    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly System.Windows.Forms.Timer _cpsTimer;

    private WebView2? _webView;
    private ClickerConfig _config;
    private bool _capturingTrigger;
    private CancellationTokenSource? _clickingCts;
    private Task? _clickingTask;

    public event Action? MinimizeRequested;
    public event Action? CloseRequested;
    public event Action? DragRequested;

    public WebViewBridge(
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
        _saveConfigUseCase = saveConfigUseCase;
        _triggerListener = triggerListener;
        _clickSession = clickSession;
        _config = loadConfigUseCase.Execute();

        _pollTimer = new System.Windows.Forms.Timer { Interval = 30 };
        _pollTimer.Tick += async (_, _) =>
        {
            if (!_capturingTrigger && _triggerListener.IsTriggered(_config.Trigger)) await ToggleClickingAsync();
        };
        _pollTimer.Start();

        _cpsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _cpsTimer.Tick += (_, _) => { _clickSession.TickClickRate(); PushState(); };
        _cpsTimer.Start();
    }

    public void AttachWebView(WebView2 webView)
    {
        _webView = webView;
        PushState();
    }

    public void HandleMessage(string webMessageAsJson)
    {
        using var doc = JsonDocument.Parse(webMessageAsJson);
        var root = doc.RootElement;
        switch (root.GetProperty("type").GetString())
        {
            case "toggleClicking":
                _ = ToggleClickingAsync();
                break;

            case "resetCount":
                _resetClickCountUseCase.Execute(_clickSession);
                PushState();
                break;

            case "startTriggerCapture":
                StartTriggerCapture();
                break;

            case "updateInterval":
                var interval = new TimeSpan(
                    root.GetProperty("hours").GetInt32(),
                    root.GetProperty("minutes").GetInt32(),
                    root.GetProperty("seconds").GetInt32())
                    + TimeSpan.FromMilliseconds(root.GetProperty("milliseconds").GetInt32());
                _config = _config with { Interval = interval };
                _saveConfigUseCase.Execute(_config);
                PushState();
                break;

            case "updateClickButton":
                var clickButton = root.GetProperty("value").GetString() == "right" ? ClickButton.Right : ClickButton.Left;
                _config = _config with { ClickButton = clickButton };
                _saveConfigUseCase.Execute(_config);
                PushState();
                break;

            case "updateClickLimit":
                _config = _config with { ClickLimit = root.GetProperty("value").GetInt32() };
                _saveConfigUseCase.Execute(_config);
                PushState();
                break;

            case "minimizeWindow":
                MinimizeRequested?.Invoke();
                break;

            case "closeWindow":
                CloseRequested?.Invoke();
                break;

            case "startWindowDrag":
                DragRequested?.Invoke();
                break;
        }
    }

    private void StartTriggerCapture()
    {
        if (_clickingCts is not null || _capturingTrigger) return;
        _capturingTrigger = true;
        PushState();
        _captureTriggerUseCase.Execute(
            onCaptured: trigger =>
            {
                _config = _config with { Trigger = trigger };
                _capturingTrigger = false;
                _saveConfigUseCase.Execute(_config);
                PushState();
            },
            onCancelled: () =>
            {
                _capturingTrigger = false;
                PushState();
            });
    }

    private async Task ToggleClickingAsync()
    {
        if (_clickingCts is not null)
        {
            var cts = _clickingCts; var task = _clickingTask!;
            _clickingCts = null; _clickingTask = null;
            await _stopClickingUseCase.ExecuteAsync(cts, task);
            PushState();
            return;
        }

        _clickingCts = new CancellationTokenSource();
        var progress = new Progress<ClickSession>(_ => PushState());
        PushState();

        _clickingTask = _startClickingUseCase.ExecuteAsync(_config, _clickSession, progress, _clickingCts.Token);
        await _clickingTask;

        _clickingCts = null; _clickingTask = null;
        PushState();
    }

    private void PushState()
    {
        if (_webView?.CoreWebView2 is null) return;

        var state = new
        {
            running = _clickingCts is not null,
            capturingTrigger = _capturingTrigger,
            clickCount = _clickSession.ClickCount,
            cps = _clickSession.ClickRate,
            triggerLabel = TriggerLabelFormatter.Format(_config.Trigger),
            interval = new
            {
                hours = _config.Interval.Hours,
                minutes = _config.Interval.Minutes,
                seconds = _config.Interval.Seconds,
                milliseconds = _config.Interval.Milliseconds,
            },
            clickButton = _config.ClickButton == ClickButton.Right ? "right" : "left",
            clickLimit = _config.ClickLimit,
        };

        var json = JsonSerializer.Serialize(state);
        _ = _webView.ExecuteScriptAsync($"window.__hostBridge && window.__hostBridge.receive({json})");
    }

    public void Dispose()
    {
        _clickingCts?.Cancel();
        _triggerListener.EndCapture();
        _pollTimer.Stop(); _pollTimer.Dispose();
        _cpsTimer.Stop(); _cpsTimer.Dispose();
        _saveConfigUseCase.Execute(_config);
    }
}
