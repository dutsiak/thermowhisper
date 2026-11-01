namespace ThermoWhisper.Application.Abstractions;

public interface IWindowService
{
    /// <summary>Shows the Settings window and brings it to the front. Safe to call from any thread.</summary>
    void ShowSettings();
}
