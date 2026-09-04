using JacaAutoClicker.Domain.Repositories;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Application.UseCases;

public sealed class SaveConfigUseCase
{
    private readonly ISettingsRepository _settingsRepository;

    public SaveConfigUseCase(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public void Execute(ClickerConfig config) => _settingsRepository.Save(config);
}
