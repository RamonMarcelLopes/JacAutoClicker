using JacaAutoClicker.Domain.Repositories;
using JacaAutoClicker.Domain.ValueObjects;

namespace JacaAutoClicker.Application.UseCases;

public sealed class LoadConfigUseCase
{
    private readonly ISettingsRepository _settingsRepository;

    public LoadConfigUseCase(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public ClickerConfig Execute() => _settingsRepository.Load();
}
