using CommunityToolkit.Maui;

using H.NotifyIcon;

using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Infrastructure.Compatibility;
using ThermoWhisper.Infrastructure.Power;
using ThermoWhisper.MauiInfrastructure.SettingsMauiStorage;
using ThermoWhisper.ViewModels;

namespace ThermoWhisper;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseNotifyIcon()
            .ConfigureLifecycleEvents(RunStartupOnLaunch)
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

    // OnLaunched runs once per process; AppStartup also guards against a second call.
    private static void RunStartupOnLaunch(ILifecycleBuilder lifecycle)
    {
#if WINDOWS
        lifecycle.AddWindows(windows => windows.OnLaunched((_, _) =>
            _ = IPlatformApplication.Current!.Services.GetRequiredService<AppStartup>().RunAsync()));
#endif
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<SystemSupport>();
        services.AddSingleton<PowerStatus>();
        services.AddSingleton<PowerActionGate>();
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<ISettingsStore, SettingsMauiStorage>();
        services.AddSingleton<ISystemCompatibilityProbe, SystemCompatibilityProbe>();
        services.AddSingleton<CheckCompatibility>();
        services.AddSingleton<ReadPowerState>();
        services.AddSingleton<StartApplication>();
        services.AddSingleton<ApplyCpuLimit>();
        services.AddSingleton<ToggleSlowDown>();
        services.AddSingleton<RestoreSpeedOnExit>();
        services.AddSingleton<ChangeQuietProfile>();
        services.AddSingleton<LoadSettings>();
        services.AddSingleton<AppStartup>();
        services.AddSingleton<TrayViewModel>();
        services.AddTransient<MainPage>();
        services.AddSingleton<ILocalizer, Localization.ResxLocalizer>();
#if WINDOWS
        services.AddSingleton<IWindowService, Platforms.Windows.WindowService>();
        services.AddSingleton<IDialogService, Platforms.Windows.Dialogs.MessageBoxDialogService>();
#endif
    }
}
