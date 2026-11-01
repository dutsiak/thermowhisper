using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.Application.Power;

public enum TrayIconKind { Normal, Slowed }

/// <summary>What the tray shows for a state. Pure; the view model maps TooltipKey/Percent through ILocalizer.</summary>
public sealed record TrayAppearance(TrayIconKind Icon, string TooltipKey, int? Percent)
{
    public static TrayAppearance From(PowerState? state, bool isLimitedMode)
    {
        if (isLimitedMode)
        {
            return new TrayAppearance(TrayIconKind.Normal, TextKeys.TrayLimitedMode, null);
        }

        if (state is null)
        {
            return new TrayAppearance(TrayIconKind.Normal, TextKeys.AppTitle, null);
        }

        if (!state.IsSlowed)
        {
            return new TrayAppearance(TrayIconKind.Normal, TextKeys.TrayFullSpeed, null);
        }

        return new TrayAppearance(TrayIconKind.Slowed, TextKeys.TrayLimit, state.Shown.Percent);
    }
}
