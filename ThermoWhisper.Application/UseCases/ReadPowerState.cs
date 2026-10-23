using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;

namespace ThermoWhisper.Application.UseCases;

/// <summary>Reads the real system state and publishes it to <see cref="PowerStatus"/>.</summary>
public sealed class ReadPowerState(IPowerService power, PowerStatus status)
{
    public async Task<PowerActionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await power.ReadAsync(cancellationToken);
            status.Update(state);
            return PowerActionResult.Success(state);
        }
        catch (PowerControlException exception)
        {
            return PowerActionResult.Failure(TextKeys.PowerReadFailed, exception.Message);
        }
    }
}
