using JacaAutoClicker.Domain.ValueObjects;
using Xunit;

namespace JacaAutoClicker.Tests.Domain;

public class ClickerConfigTests
{
    [Fact]
    public void Default_HasF6AsTrigger()
    {
        var trigger = Assert.IsType<KeyTrigger>(ClickerConfig.Default.Trigger);
        Assert.Equal(0x75, trigger.VirtualKeyCode);
    }

    [Fact]
    public void Default_HasHundredMillisecondInterval_LeftClickButton_AndUnlimitedClicks()
    {
        var config = ClickerConfig.Default;

        Assert.Equal(TimeSpan.FromMilliseconds(100), config.Interval);
        Assert.Equal(ClickButton.Left, config.ClickButton);
        Assert.Equal(0, config.ClickLimit);
    }
}
