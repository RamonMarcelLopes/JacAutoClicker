using JacaAutoClicker.Domain.Entities;

namespace JacaAutoClicker.Application.UseCases;

public sealed class ResetClickCountUseCase
{
    public void Execute(ClickSession session) => session.Reset();
}
