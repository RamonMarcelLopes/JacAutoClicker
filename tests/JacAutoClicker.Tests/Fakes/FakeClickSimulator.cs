using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Tests.Fakes;

public sealed class FakeClickSimulator : IClickSimulator
{
    public List<ClickButton> Clicks { get; } = new();

    public void Click(ClickButton button) => Clicks.Add(button);
}
