using System.Windows.Forms;
using Microsoft.Win32;
using JacaAutoClicker.Domain.Repositories;
using JacaAutoClicker.Domain.ValueObjects;
using JacaAutoClicker.Infrastructure.Input;

namespace JacaAutoClicker.Infrastructure.Persistence;

public sealed class RegistrySettingsRepository : ISettingsRepository
{
    private const string RegistryKeyPath = @"Software\JacaAutoClicker";

    public ClickerConfig Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            if (key == null) return ClickerConfig.Default;

            var mouseButtonCode = Convert.ToInt32(key.GetValue("BindMouse", -1));
            Trigger trigger = mouseButtonCode >= 0
                ? new MouseTrigger(MouseVirtualKeyCodes.ToTriggerButton(mouseButtonCode))
                : LoadKeyTrigger(key);

            var interval = new TimeSpan(
                Convert.ToInt32(key.GetValue("Hours", 0)),
                Convert.ToInt32(key.GetValue("Min", 0)),
                Convert.ToInt32(key.GetValue("Sec", 0)))
                + TimeSpan.FromMilliseconds(Convert.ToInt32(key.GetValue("Ms", 100)));

            var clickButton = Convert.ToBoolean(key.GetValue("ClickLeft", true))
                ? ClickButton.Left
                : ClickButton.Right;

            var clickLimit = Convert.ToInt32(key.GetValue("Limit", 0));

            return new ClickerConfig(trigger, interval, clickButton, clickLimit);
        }
        catch
        {
            return ClickerConfig.Default;
        }
    }

    public void Save(ClickerConfig config)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            if (key == null) return;

            switch (config.Trigger)
            {
                case MouseTrigger mouse:
                    key.SetValue("BindMouse", MouseVirtualKeyCodes.From(mouse.Button));
                    key.SetValue("BindKey", Keys.None.ToString());
                    break;
                case KeyTrigger keyTrigger:
                    key.SetValue("BindMouse", -1);
                    key.SetValue("BindKey", ((Keys)keyTrigger.VirtualKeyCode).ToString());
                    break;
            }

            key.SetValue("Hours", config.Interval.Hours);
            key.SetValue("Min", config.Interval.Minutes);
            key.SetValue("Sec", config.Interval.Seconds);
            key.SetValue("Ms", config.Interval.Milliseconds);
            key.SetValue("Limit", config.ClickLimit);
            key.SetValue("ClickLeft", config.ClickButton == ClickButton.Left);
        }
        catch
        {
        }
    }

    private static Trigger LoadKeyTrigger(RegistryKey key)
    {
        var keyName = key.GetValue("BindKey", "F6")?.ToString() ?? "F6";
        return Enum.TryParse<Keys>(keyName, out var parsedKey) && parsedKey != Keys.None
            ? new KeyTrigger((int)parsedKey)
            : ClickerConfig.Default.Trigger;
    }
}
