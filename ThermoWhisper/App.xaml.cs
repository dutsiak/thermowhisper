using Microsoft.Extensions.Logging;

using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly StartApplication _startApplication;
    private readonly ILogger<App> _logger;
    private int _startupStarted;

    public App(StartApplication startApplication, ILogger<App> logger)
    {
        _startApplication = startApplication;
        _logger = logger;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // CreateWindow can run again (window reopened); the startup sequence must run once.
        if (Interlocked.Exchange(ref _startupStarted, 1) == 0)
        {
            _ = RunStartupAsync();
        }

        return new Window(new AppShell());
    }

    private async Task RunStartupAsync()
    {
        try
        {
            var result = await _startApplication.ExecuteAsync();

            // The limited-mode popup comes with TW-4; until then the problems only go to the log.
            foreach (var problem in result.Problems)
            {
                _logger.LogWarning("Compatibility problem {Key}: {Detail}", problem.TextKey, problem.Detail);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Startup sequence failed");
        }
    }
}
