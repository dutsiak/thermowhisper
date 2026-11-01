using CommunityToolkit.Maui;

using Microsoft.Extensions.Logging;

using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Infrastructure.Compatibility;
using ThermoWhisper.Infrastructure.Power;
using ThermoWhisper.MauiInfrastructure.SettingsMauiStorage;

namespace ThermoWhisper;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<SystemSupport>();
        services.AddSingleton<PowerStatus>();
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<ISettingsStore, SettingsMauiStorage>();
        services.AddSingleton<ISystemCompatibilityProbe, SystemCompatibilityProbe>();
        services.AddSingleton<CheckCompatibility>();
        services.AddSingleton<ReadPowerState>();
        services.AddSingleton<StartApplication>();
        services.AddSingleton<ILocalizer, Localization.ResxLocalizer>();
#if WINDOWS
        services.AddSingleton<IWindowService, Platforms.Windows.WindowService>();
        services.AddSingleton<IDialogService, Platforms.Windows.Dialogs.MessageBoxDialogService>();
#endif
    }
}
