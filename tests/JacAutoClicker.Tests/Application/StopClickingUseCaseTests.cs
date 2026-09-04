using JacaAutoClicker.Application.UseCases;
using Xunit;

namespace JacaAutoClicker.Tests.Application;

public class StopClickingUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CancelsTheTokenAndAwaitsTheRunningTask()
    {
        var useCase = new StopClickingUseCase();
        using var cts = new CancellationTokenSource();
        var completed = false;
        var runningTask = Task.Run(async () =>
        {
            try { await Task.Delay(Timeout.Infinite, cts.Token); }
            catch (OperationCanceledException) { completed = true; throw; }
        });

        await useCase.ExecuteAsync(cts, runningTask);

        Assert.True(cts.IsCancellationRequested);
        Assert.True(completed);
    }
}
