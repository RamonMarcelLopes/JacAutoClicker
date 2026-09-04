using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.ValueObjects;
using JacaAutoClicker.Tests.Fakes;
using Xunit;

namespace JacaAutoClicker.Tests.Application;

public class CaptureTriggerUseCaseTests
{
    [Fact]
    public void Execute_ForwardsCallbacksToTheTriggerListener()
    {
        var listener = new FakeTriggerListener();
        var useCase = new CaptureTriggerUseCase(listener);
        Trigger? captured = null;
        var cancelled = false;

        useCase.Execute(t => captured = t, () => cancelled = true);
        listener.OnCaptured!(new KeyTrigger(0x41));

        Assert.Equal(new KeyTrigger(0x41), captured);
        Assert.False(cancelled);
    }

    [Fact]
    public void Execute_ForwardsCancellationToTheTriggerListener()
    {
        var listener = new FakeTriggerListener();
        var useCase = new CaptureTriggerUseCase(listener);
        var cancelled = false;

        useCase.Execute(_ => { }, () => cancelled = true);
        listener.OnCancelled!();

        Assert.True(cancelled);
    }
}
