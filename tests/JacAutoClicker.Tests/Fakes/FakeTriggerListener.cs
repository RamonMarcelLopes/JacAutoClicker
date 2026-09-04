using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Tests.Fakes;

public sealed class FakeTriggerListener : ITriggerListener
{
    public Action<Trigger>? OnCaptured { get; private set; }
    public Action? OnCancelled { get; private set; }
    public bool CaptureEnded { get; private set; }
    public bool TriggeredResult { get; set; }

    public bool IsTriggered(Trigger trigger) => TriggeredResult;

    public void BeginCapture(Action<Trigger> onCaptured, Action onCancelled)
    {
        OnCaptured = onCaptured;
        OnCancelled = onCancelled;
    }

    public void EndCapture() => CaptureEnded = true;
}
