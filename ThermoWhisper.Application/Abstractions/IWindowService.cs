namespace ThermoWhisper.Application.Abstractions;

public interface IWindowService
{
    /// <summary>Shows the Settings window and brings it to the front. Safe to call from any thread.</summary>
    void ShowSettings();

    /// <summary>Hides the Settings window; the app keeps running in the tray. Safe to call from any thread.</summary>
    void Hide();

    /// <summary>Ends the app for real: closes the window and exits the process. Safe to call from any thread.</summary>
    void Quit();

    /// <summary>True while the Settings window is shown (read on the UI thread). Starts true: MAUI shows the window at start.</summary>
    bool IsSettingsVisible { get; }

    /// <summary>Raised on the UI thread after every ShowSettings (also when already visible) and after the window was hidden (Hide or the X button).</summary>
    event EventHandler? SettingsVisibilityChanged;
}
