using JacaAutoClicker.Domain.Entities;
using Xunit;

namespace JacaAutoClicker.Tests.Domain;

public class ClickSessionTests
{
    [Fact]
    public void RecordClick_IncrementsClickCount()
    {
        var session = new ClickSession();

        session.RecordClick();
        session.RecordClick();

        Assert.Equal(2, session.ClickCount);
    }

    [Fact]
    public void HasReachedLimit_ReturnsFalse_WhenLimitIsZero()
    {
        var session = new ClickSession();
        for (int i = 0; i < 1000; i++) session.RecordClick();

        Assert.False(session.HasReachedLimit(0));
    }

    [Fact]
    public void HasReachedLimit_ReturnsTrue_WhenClickCountReachesLimit()
    {
        var session = new ClickSession();
        session.RecordClick();
        session.RecordClick();

        Assert.True(session.HasReachedLimit(2));
    }

    [Fact]
    public void HasReachedLimit_ReturnsFalse_BeforeLimitIsReached()
    {
        var session = new ClickSession();
        session.RecordClick();

        Assert.False(session.HasReachedLimit(2));
    }

    [Fact]
    public void TickClickRate_ReportsClicksSinceLastTick_ThenResetsCounter()
    {
        var session = new ClickSession();
        session.RecordClick();
        session.RecordClick();
        session.RecordClick();

        session.TickClickRate();
        Assert.Equal(3, session.ClickRate);

        session.TickClickRate();
        Assert.Equal(0, session.ClickRate);
    }

    [Fact]
    public void Reset_ZeroesClickCountAndRate()
    {
        var session = new ClickSession();
        session.RecordClick();
        session.TickClickRate();

        session.Reset();

        Assert.Equal(0, session.ClickCount);
        Assert.Equal(0, session.ClickRate);
        Assert.False(session.HasReachedLimit(1));
    }
}
