using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ThermoWhisper.Application;
using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper.ViewModels;

/// <summary>Tray menu: one toggle item (Slow Down / Restore Speed by the real limit) and Settings.</summary>
public sealed partial class TrayViewModel : ObservableObject
{
    // The menu is built right after the open command returns, so the fresh read must finish inside it.
    private static readonly TimeSpan MenuReadTimeout = TimeSpan.FromSeconds(2);

    private readonly PowerStatus _status;
    private readonly ReadPowerState _readPowerState;
    private readonly ToggleSlowDown _toggleSlowDown;
    private readonly PowerActionGate _gate;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizer _localizer;

    public TrayViewModel(
        PowerStatus status,
        ReadPowerState readPowerState,
        ToggleSlowDown toggleSlowDown,
        PowerActionGate gate,
        IWindowService windowService,
        IDialogService dialogService,
        ILocalizer localizer)
    {
        _status = status;
        _readPowerState = readPowerState;
        _toggleSlowDown = toggleSlowDown;
        _gate = gate;
        _windowService = windowService;
        _dialogService = dialogService;
        _localizer = localizer;

        _status.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshItems);
        _gate.BusyChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshItems);
    }

    public string ToggleText => _localizer.Get(_status.Current?.IsSlowed == true ? TextKeys.TrayRestoreSpeed : TextKeys.TraySlowDown);

    public string SettingsText => _localizer.Get(TextKeys.TraySettings);

    /// <summary>Runs on the UI thread just before the tray menu is built (the library has no menu-open event; this is bound to RightClickCommand).</summary>
    [RelayCommand]
    private void PrepareMenu()
    {
        // Task.Run: the use case must not resume on the UI thread that waits here.
        Task.Run(() => _readPowerState.ExecuteAsync()).Wait(MenuReadTimeout);
        RefreshItems();
    }

    [RelayCommand(CanExecute = nameof(CanToggle))]
    private async Task ToggleAsync()
    {
        var result = await _gate.TryRunAsync(() => _toggleSlowDown.ExecuteAsync(CancellationToken.None));

        if (result is { IsSuccess: false })
        {
            await ShowErrorAsync(result);
        }
    }

    [RelayCommand]
    private void OpenSettings() => _windowService.ShowSettings();

    private bool CanToggle() => !_gate.IsBusy;

    private void RefreshItems()
    {
        OnPropertyChanged(nameof(ToggleText));
        ToggleCommand.NotifyCanExecuteChanged();
    }

    private Task ShowErrorAsync(PowerActionResult result)
    {
        var message = _localizer.Get(result.ErrorKey!);

        if (result.ErrorDetail is not null)
        {
            message = $"{message}{Environment.NewLine}{Environment.NewLine}{result.ErrorDetail}";
        }

        return _dialogService.ShowMessageAsync(_localizer.Get(TextKeys.AppTitle), message);
    }
}
