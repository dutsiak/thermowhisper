using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Settings;

namespace ThermoWhisper.Application.UseCases;

/// <summary>Reads the stored settings. A missing or unknown quiet profile falls back to the default.</summary>
public sealed class LoadSettings(ISettingsStore settings)
{
    public AppSettings Execute() => new(QuietProfile.FromStored(settings.GetQuietProfilePercent()));
}
