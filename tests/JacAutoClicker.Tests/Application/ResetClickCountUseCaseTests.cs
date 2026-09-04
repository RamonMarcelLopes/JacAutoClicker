using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.Entities;
using Xunit;

namespace JacaAutoClicker.Tests.Application;

public class ResetClickCountUseCaseTests
{
    [Fact]
    public void Execute_ResetsTheSession()
    {
        var session = new ClickSession();
        session.RecordClick();
        session.RecordClick();
        var useCase = new ResetClickCountUseCase();

        useCase.Execute(session);

        Assert.Equal(0, session.ClickCount);
    }
}
