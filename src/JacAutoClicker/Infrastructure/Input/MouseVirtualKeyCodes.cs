using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Infrastructure.Input;

internal static class MouseVirtualKeyCodes
{
    public const int Left = 0x01;
    public const int Right = 0x02;
    public const int Middle = 0x04;

    public static int From(TriggerButton button) => button switch
    {
        TriggerButton.Left => Left,
        TriggerButton.Right => Right,
        _ => Middle
    };

    public static TriggerButton ToTriggerButton(int virtualKeyCode) => virtualKeyCode switch
    {
        Left => TriggerButton.Left,
        Right => TriggerButton.Right,
        _ => TriggerButton.Middle
    };
}
