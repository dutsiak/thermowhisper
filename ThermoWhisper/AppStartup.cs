using Microsoft.Extensions.Logging;

using ThermoWhisper.Application;
using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper;

/// <summary>Runs the startup sequence once per process, from a lifecycle event registered in <see cref="MauiProgram"/>.</summary>
public sealed class AppStartup(
    StartApplication startApplication,
    IDialogService dialogService,
    ILocalizer localizer,
    ILogger<AppStartup> logger)
{
    private int _started;

    /// <summary>Raised on a background thread when the check and the first read are done, before the limited-mode popup; the UI marshals to its own thread.</summary>
    public event EventHandler? Completed;

    public async Task RunAsync()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        try
        {
            var result = await startApplication.ExecuteAsync();
            LogProblems(result);
            Completed?.Invoke(this, EventArgs.Empty);
            await ShowLimitedModePopupAsync(result);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Startup sequence failed");
        }
    }

    private void LogProblems(CompatibilityResult result)
    {
        foreach (var problem in result.Problems)
        {
            logger.LogWarning("Compatibility problem {Key}: {Detail}", problem.TextKey, problem.Detail);
        }
    }

    // Waits until the box is closed; the tray is already refreshed (Completed) and shows limited mode meanwhile.
    private Task ShowLimitedModePopupAsync(CompatibilityResult result) =>
        result.IsSupported
            ? Task.CompletedTask
            : dialogService.ShowMessageAsync(localizer.Get(TextKeys.AppTitle), LimitedModeMessage.Build(result, localizer));
}
