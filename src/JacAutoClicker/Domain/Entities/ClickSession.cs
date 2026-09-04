namespace JacaAutoClicker.Domain.Entities;

public sealed class ClickSession
{
    public long ClickCount { get; private set; }
    public double ClickRate { get; private set; }

    private long _clicksSinceLastTick;

    public void RecordClick()
    {
        ClickCount++;
        _clicksSinceLastTick++;
    }

    public void TickClickRate()
    {
        ClickRate = _clicksSinceLastTick;
        _clicksSinceLastTick = 0;
    }

    public void Reset()
    {
        ClickCount = 0;
        ClickRate = 0;
        _clicksSinceLastTick = 0;
    }

    public bool HasReachedLimit(int clickLimit) => clickLimit > 0 && ClickCount >= clickLimit;
}
