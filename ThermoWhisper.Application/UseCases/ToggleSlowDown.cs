using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Settings;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.UseCases;

/// <summary>Restores full speed when the system is slowed, otherwise applies the quiet profile. Decides from a fresh read.</summary>
public sealed class ToggleSlowDown(
    SystemSupport support,
    ReadPowerState readPowerState,
    ISettingsStore settings,
    ApplyCpuLimit applyCpuLimit)
{
    public async Task<PowerActionResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!support.IsSupported)
        {
            return PowerActionResult.Failure(TextKeys.LimitedMode);
        }

        var readResult = await readPowerState.ExecuteAsync(cancellationToken);

        if (!readResult.IsSuccess)
        {
            return readResult;
        }

        var target = readResult.State!.IsSlowed
            ? CpuLimit.Full
            : QuietProfile.FromStored(settings.GetQuietProfilePercent());

        return await applyCpuLimit.ExecuteAsync(target, cancellationToken);
    }
}
