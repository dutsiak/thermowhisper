namespace ThermoWhisper.Application.Abstractions;

public interface IDialogService
{
    /// <summary>Shows one modal message with an OK button, also when no app window is visible. Completes when closed. Safe from any thread.</summary>
    Task ShowMessageAsync(string title, string message);
}
