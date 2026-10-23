using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.UseCases;

/// <summary>On Exit (UC-5): restores 100 % if the system is slowed, even when another tool set it. Power errors are returned, never thrown; the caller shows one popup and quits anyway.</summary>
public sealed class RestoreSpeedOnExit(SystemSupport support, ReadPowerState readPowerState, ApplyCpuLimit applyCpuLimit)
{
    public async Task<PowerActionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!support.IsSupported)
        {
            return PowerActionResult.Success();
        }

        var readResult = await readPowerState.ExecuteAsync(cancellationToken);

        if (!readResult.IsSuccess)
        {
            return readResult;
        }

        return readResult.State!.IsSlowed
            ? await applyCpuLimit.ExecuteAsync(CpuLimit.Full, cancellationToken)
            : readResult;
    }
}
