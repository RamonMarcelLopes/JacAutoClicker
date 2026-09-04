namespace JacaAutoClicker.Domain.ValueObjects;

public sealed record ClickerConfig(
    Trigger Trigger,
    TimeSpan Interval,
    ClickButton ClickButton,
    int ClickLimit)
{
    public static ClickerConfig Default { get; } = new(
        Trigger: new KeyTrigger(VirtualKeys.F6),
        Interval: TimeSpan.FromMilliseconds(100),
        ClickButton: ClickButton.Left,
        ClickLimit: 0);
}
