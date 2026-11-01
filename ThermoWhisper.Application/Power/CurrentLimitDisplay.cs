using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Power;

/// <summary>What Settings shows as the current limit. Pure; the view model maps TextKey and the numbers through ILocalizer.</summary>
public sealed record CurrentLimitDisplay(string TextKey, int? AcPercent, int? DcPercent)
{
    /// <summary>null -> Unknown; not slowed -> Full; slowed and AC == DC -> Percent (AC); slowed and AC != DC -> Split (AC, DC).</summary>
    public static CurrentLimitDisplay From(PowerState? state)
    {
        if (state is null)
        {
            return new CurrentLimitDisplay(TextKeys.SettingsLimitUnknown, null, null);
        }

        if (!state.IsSlowed)
        {
            return new CurrentLimitDisplay(TextKeys.SettingsLimitFull, null, null);
        }

        return state.Ac == state.Dc
            ? new CurrentLimitDisplay(TextKeys.SettingsLimitPercent, state.Ac.Percent, null)
            : new CurrentLimitDisplay(TextKeys.SettingsLimitSplit, state.Ac.Percent, state.Dc.Percent);
    }
}
