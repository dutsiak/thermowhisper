using Microsoft.Extensions.Logging;

using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper;

/// <summary>Runs the startup sequence once per process, from a lifecycle event registered in <see cref="MauiProgram"/>.</summary>
public sealed class AppStartup(StartApplication startApplication, ILogger<AppStartup> logger)
{
    private int _started;

    public async Task RunAsync()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        try
        {
            var result = await startApplication.ExecuteAsync();

            // The limited-mode popup comes with TW-4; until then the problems only go to the log.
            foreach (var problem in result.Problems)
            {
                logger.LogWarning("Compatibility problem {Key}: {Detail}", problem.TextKey, problem.Detail);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Startup sequence failed");
        }
    }
}
