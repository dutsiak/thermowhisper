using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.MauiInfrastructure.SettingsMauiStorage;

/// <summary>Keeps the user's own settings (not the CPU limit, the system owns that) in MAUI Preferences.</summary>
public sealed class SettingsMauiStorage : ISettingsStore
{
    private const string SharedName = "ThermoWhisper";
    private const string QuietProfilePercentKey = "QuietProfilePercent";

    public int? GetQuietProfilePercent() =>
        Preferences.Default.ContainsKey(QuietProfilePercentKey, SharedName)
            ? Preferences.Default.Get(QuietProfilePercentKey, 0, SharedName)
            : null;

    public void SetQuietProfilePercent(int percent) =>
        Preferences.Default.Set(QuietProfilePercentKey, percent, SharedName);
}
