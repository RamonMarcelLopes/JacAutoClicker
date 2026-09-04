using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Application.UseCases;

public sealed class CaptureTriggerUseCase
{
    private readonly ITriggerListener _triggerListener;

    public CaptureTriggerUseCase(ITriggerListener triggerListener)
    {
        _triggerListener = triggerListener;
    }

    public void Execute(Action<Trigger> onCaptured, Action onCancelled) =>
        _triggerListener.BeginCapture(onCaptured, onCancelled);
}
