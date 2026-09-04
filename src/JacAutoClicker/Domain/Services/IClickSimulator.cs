using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Domain.Services;

public interface IClickSimulator
{
    void Click(ClickButton button);
}
