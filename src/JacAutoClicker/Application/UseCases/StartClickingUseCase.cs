using JacaAutoClicker.Domain.Entities;
using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Application.UseCases;

public sealed class StartClickingUseCase
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromMilliseconds(1);

    private readonly IClickSimulator _clickSimulator;

    public StartClickingUseCase(IClickSimulator clickSimulator)
    {
        _clickSimulator = clickSimulator;
    }

    public async Task ExecuteAsync(
        ClickerConfig config,
        ClickSession session,
        IProgress<ClickSession>? progress,
        CancellationToken cancellationToken)
    {
        var interval = config.Interval < MinimumInterval ? MinimumInterval : config.Interval;

        while (!cancellationToken.IsCancellationRequested && !session.HasReachedLimit(config.ClickLimit))
        {
            _clickSimulator.Click(config.ClickButton);
            session.RecordClick();
            progress?.Report(session);

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
