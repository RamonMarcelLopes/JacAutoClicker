using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Domain.Services;

public interface ITriggerListener
{
    bool IsTriggered(Trigger trigger);
    void BeginCapture(Action<Trigger> onCaptured, Action onCancelled);
    void EndCapture();
}
