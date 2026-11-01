using ThermoWhisper.Application.Compatibility;

namespace ThermoWhisper.Application.UseCases;

/// <summary>App start (UC-4, UC-6): check the system, then read the real state. Never writes.</summary>
public sealed class StartApplication(CheckCompatibility checkCompatibility, ReadPowerState readPowerState)
{
    public async Task<CompatibilityResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await checkCompatibility.ExecuteAsync(cancellationToken);

        if (result.IsSupported)
        {
            // ReadPowerState turns PowerControlException into a failure result; it is ignored here and the status stays empty.
            await readPowerState.ExecuteAsync(cancellationToken);
        }

        return result;
    }
}
