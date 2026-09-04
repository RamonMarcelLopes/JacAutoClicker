using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.Entities;
using JacaAutoClicker.Domain.ValueObjects;
using JacaAutoClicker.Tests.Fakes;
using Xunit;

namespace JacaAutoClicker.Tests.Application;

public class StartClickingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_StopsOnItsOwn_WhenClickLimitIsReached()
    {
        var clickSimulator = new FakeClickSimulator();
        var useCase = new StartClickingUseCase(clickSimulator);
        var session = new ClickSession();
        var config = ClickerConfig.Default with { Interval = TimeSpan.FromMilliseconds(1), ClickLimit = 3 };

        await useCase.ExecuteAsync(config, session, progress: null, CancellationToken.None);

        Assert.Equal(3, clickSimulator.Clicks.Count);
        Assert.Equal(3, session.ClickCount);
    }

    [Fact]
    public async Task ExecuteAsync_StopsWhenCancelled_EvenWithoutAClickLimit()
    {
        var clickSimulator = new FakeClickSimulator();
        var useCase = new StartClickingUseCase(clickSimulator);
        var session = new ClickSession();
        var config = ClickerConfig.Default with { Interval = TimeSpan.FromMilliseconds(20), ClickLimit = 0 };
        using var cts = new CancellationTokenSource();

        var task = useCase.ExecuteAsync(config, session, progress: null, cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await task;

        Assert.True(session.ClickCount > 0);
    }

    [Fact]
    public async Task ExecuteAsync_ClicksTheConfiguredButton()
    {
        var clickSimulator = new FakeClickSimulator();
        var useCase = new StartClickingUseCase(clickSimulator);
        var session = new ClickSession();
        var config = ClickerConfig.Default with { Interval = TimeSpan.FromMilliseconds(1), ClickLimit = 1, ClickButton = ClickButton.Right };

        await useCase.ExecuteAsync(config, session, progress: null, CancellationToken.None);

        Assert.Equal(ClickButton.Right, Assert.Single(clickSimulator.Clicks));
    }
}
