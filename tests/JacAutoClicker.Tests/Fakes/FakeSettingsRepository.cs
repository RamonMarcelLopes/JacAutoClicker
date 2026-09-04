using JacaAutoClicker.Domain.Repositories;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Tests.Fakes;

public sealed class FakeSettingsRepository : ISettingsRepository
{
    public ClickerConfig ConfigToLoad { get; set; } = ClickerConfig.Default;
    public ClickerConfig? SavedConfig { get; private set; }

    public ClickerConfig Load() => ConfigToLoad;

    public void Save(ClickerConfig config) => SavedConfig = config;
}
