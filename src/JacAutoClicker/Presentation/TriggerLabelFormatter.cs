using System.Windows.Forms;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Presentation;

internal static class TriggerLabelFormatter
{
    public static string Format(Trigger trigger) => trigger switch
    {
        KeyTrigger key => ((Keys)key.VirtualKeyCode).ToString(),
        MouseTrigger mouse => mouse.Button switch
        {
            TriggerButton.Left => "Mouse Esq",
            TriggerButton.Right => "Mouse Dir",
            TriggerButton.Middle => "Mouse Mid",
            _ => mouse.Button.ToString()
        },
        _ => trigger.ToString() ?? string.Empty
    };
}
