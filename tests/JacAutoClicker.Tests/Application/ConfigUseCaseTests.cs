using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.ValueObjects;
using JacaAutoClicker.Tests.Fakes;
using Xunit;

namespace JacaAutoClicker.Tests.Application;

public class ConfigUseCaseTests
{
    [Fact]
    public void LoadConfigUseCase_ReturnsWhatTheRepositoryHas()
    {
        var repository = new FakeSettingsRepository
        {
            ConfigToLoad = ClickerConfig.Default with { ClickLimit = 50 }
        };
        var useCase = new LoadConfigUseCase(repository);

        var result = useCase.Execute();

        Assert.Equal(50, result.ClickLimit);
    }

    [Fact]
    public void SaveConfigUseCase_PersistsTheGivenConfig()
    {
        var repository = new FakeSettingsRepository();
        var useCase = new SaveConfigUseCase(repository);
        var config = ClickerConfig.Default with { ClickButton = ClickButton.Right };

        useCase.Execute(config);

        Assert.Equal(config, repository.SavedConfig);
    }
}
