using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;

namespace ThermoWhisper.Application.UseCases;

/// <summary>Startup check (architecture section 4). Stores the result in <see cref="SystemSupport"/>. Never writes and never throws, except cancellation: any other exception becomes a listed problem.</summary>
public sealed class CheckCompatibility(ISystemCompatibilityProbe probe, IPowerService power, SystemSupport support)
{
    public static Version MinimumWindows { get; } = new(10, 0, 17763);

    public async Task<CompatibilityResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var problems = new List<CompatibilityProblem>();

        try
        {
            var facts = probe.GetFacts();
            AddWindowsProblem(facts.WindowsVersion, problems);
            await AddPowerProblemsAsync(facts, problems, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            problems.Add(new CompatibilityProblem(TextKeys.CompatibilityCheckFailed, $"{exception.GetType().FullName}: {exception.Message}"));
        }

        var result = new CompatibilityResult(problems);
        support.Set(result);
        return result;
    }

    private static void AddWindowsProblem(Version windows, List<CompatibilityProblem> problems)
    {
        // Only major.minor.build count: 10.0.17763.0 is not older than 10.0.17763.
        var build = new Version(windows.Major, windows.Minor, windows.Build);

        if (build < MinimumWindows)
        {
            problems.Add(new CompatibilityProblem(TextKeys.WindowsTooOld, windows.ToString()));
        }
    }

    private async Task AddPowerProblemsAsync(SystemFacts facts, List<CompatibilityProblem> problems, CancellationToken cancellationToken)
    {
        // Both power paths unavailable: nothing to read with, so the read is not tried.
        if (!facts.PowrprofUsable && !facts.PowercfgExists)
        {
            problems.Add(new CompatibilityProblem(TextKeys.PowerControlUnavailable, DescribeMissingPowerPaths(facts)));
            return;
        }

        try
        {
            await power.ReadAsync(cancellationToken);
        }
        catch (PowerControlException exception)
        {
            problems.Add(new CompatibilityProblem(TextKeys.PowerSchemeUnreadable, exception.Message));
        }
    }

    private static string DescribeMissingPowerPaths(SystemFacts facts) =>
        $"powrprof.dll loaded: {facts.PowrprofLoaded}, missing exports: [{string.Join(", ", facts.MissingPowrprofExports)}]; powercfg.exe exists: {facts.PowercfgExists}";
}
