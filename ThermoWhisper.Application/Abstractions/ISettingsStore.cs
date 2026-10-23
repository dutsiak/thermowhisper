namespace ThermoWhisper.Application.Abstractions;

public interface ISettingsStore
{
    /// <summary>Null when never saved.</summary>
    int? GetQuietProfilePercent();

    void SetQuietProfilePercent(int percent);
}
