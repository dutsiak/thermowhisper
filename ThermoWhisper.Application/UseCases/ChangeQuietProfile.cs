using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Settings;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.UseCases;

/// <summary>Saves the quiet profile and, if the system is slowed now, applies it at once (UC-3). A value outside the options is a bug and is not saved.</summary>
public sealed class ChangeQuietProfile(
    ISettingsStore settings,
    SystemSupport support,
    ReadPowerState readPowerState,
    ApplyCpuLimit applyCpuLimit)
{
    public async Task<PowerActionResult> ExecuteAsync(CpuLimit quietProfile, CancellationToken cancellationToken = default)
    {
        if (!QuietProfile.IsOption(quietProfile))
        {
            throw new ArgumentOutOfRangeException(nameof(quietProfile), quietProfile, "Quiet profile must be one of the options.");
        }

        settings.SetQuietProfilePercent(quietProfile.Percent);

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
            ? await applyCpuLimit.ExecuteAsync(quietProfile, cancellationToken)
            : readResult;
    }
}
