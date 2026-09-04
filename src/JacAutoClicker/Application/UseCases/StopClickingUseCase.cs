namespace JacaAutoClicker.Application.UseCases;

public sealed class StopClickingUseCase
{
    public async Task ExecuteAsync(CancellationTokenSource clickingCts, Task clickingTask)
    {
        clickingCts.Cancel();
        try
        {
            await clickingTask;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
