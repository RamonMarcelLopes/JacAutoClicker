using JacaAutoClicker.Application.UseCases;
using JacaAutoClicker.Domain.Entities;
using JacaAutoClicker.Domain.Repositories;
using JacaAutoClicker.Domain.Services;
using JacaAutoClicker.Infrastructure.Input;
using JacaAutoClicker.Infrastructure.Persistence;
using JacaAutoClicker.Presentation;
using Microsoft.Extensions.DependencyInjection;
using WinFormsApplication = System.Windows.Forms.Application;

namespace JacaAutoClicker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        WinFormsApplication.EnableVisualStyles();
        WinFormsApplication.SetCompatibleTextRenderingDefault(false);

        using var serviceProvider = BuildServiceProvider();
        WinFormsApplication.Run(serviceProvider.GetRequiredService<MainForm>());
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISettingsRepository, RegistrySettingsRepository>();
        services.AddSingleton<IClickSimulator, Win32ClickSimulator>();
        services.AddSingleton<ITriggerListener, Win32TriggerListener>();
        services.AddSingleton<ClickSession>();

        services.AddTransient<StartClickingUseCase>();
        services.AddTransient<StopClickingUseCase>();
        services.AddTransient<ResetClickCountUseCase>();
        services.AddTransient<CaptureTriggerUseCase>();
        services.AddTransient<LoadConfigUseCase>();
        services.AddTransient<SaveConfigUseCase>();

        services.AddTransient<MainForm>();

        return services.BuildServiceProvider();
    }
}
