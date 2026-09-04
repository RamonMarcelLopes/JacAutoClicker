using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Domain.Repositories;

public interface ISettingsRepository
{
    ClickerConfig Load();
    void Save(ClickerConfig config);
}
