using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper.Application.Power;

/// <summary>Reads the real state every <see cref="Interval"/> until cancelled (Settings is visible). A tick is skipped while a power action runs
/// or the system is not supported (limited mode, check not finished). Read errors are ignored; the next tick tries again.</summary>
public sealed class StatusRefreshLoop(ReadPowerState readPowerState, SystemSupport support, PowerActionGate gate, TimeProvider timeProvider)
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

    /// <summary>Runs until cancelled; completes normally on cancellation (no OperationCanceledException to the caller).</summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (support.IsSupported && !gate.IsBusy)
                {
                    await readPowerState.ExecuteAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled on purpose: the window was hidden or shown again.
        }
    }
}
