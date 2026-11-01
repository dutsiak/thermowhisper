using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ThermoWhisper.Application;
using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.UseCases;

namespace ThermoWhisper.ViewModels;

/// <summary>Tray menu: one toggle item (Slow Down / Restore Speed by the real limit) and Settings.</summary>
public sealed partial class TrayViewModel : ObservableObject
{
    // The menu is built right after the open command returns, so the fresh read must finish inside it.
    private static readonly TimeSpan MenuReadTimeout = TimeSpan.FromSeconds(2);

    private readonly PowerStatus _status;
    private readonly SystemSupport _systemSupport;
    private readonly ReadPowerState _readPowerState;
    private readonly ToggleSlowDown _toggleSlowDown;
    private readonly RestoreSpeedOnExit _restoreSpeedOnExit;
    private readonly PowerActionGate _gate;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizer _localizer;

    public TrayViewModel(
        PowerStatus status,
        SystemSupport systemSupport,
        ReadPowerState readPowerState,
        ToggleSlowDown toggleSlowDown,
        RestoreSpeedOnExit restoreSpeedOnExit,
        PowerActionGate gate,
        AppStartup appStartup,
        IWindowService windowService,
        IDialogService dialogService,
        ILocalizer localizer)
    {
        _status = status;
        _systemSupport = systemSupport;
        _readPowerState = readPowerState;
        _toggleSlowDown = toggleSlowDown;
        _restoreSpeedOnExit = restoreSpeedOnExit;
        _gate = gate;
        _windowService = windowService;
        _dialogService = dialogService;
        _localizer = localizer;

        _status.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshItems);
        _gate.BusyChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshItems);
        appStartup.Completed += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshItems);
    }

    public string ToggleText => _localizer.Get(_status.Current?.IsSlowed == true ? TextKeys.TrayRestoreSpeed : TextKeys.TraySlowDown);

    public string SettingsText => _localizer.Get(TextKeys.TraySettings);

    public string ExitText => _localizer.Get(TextKeys.TrayExit);

    /// <summary>The tray icon kind; the page maps it to the .ico file (Platforms/Windows/TrayIconConverter).</summary>
    public TrayIconKind IconKind => Appearance.Icon;

    public string TooltipText
    {
        get
        {
            var appearance = Appearance;

            return appearance.Percent is { } percent
                ? _localizer.Format(appearance.TooltipKey, percent)
                : _localizer.Get(appearance.TooltipKey);
        }
    }

    private TrayAppearance Appearance =>
        TrayAppearance.From(_status.Current, isLimitedMode: _systemSupport.Result is { IsSupported: false });

    /// <summary>Runs on the UI thread on right mouse down, before the tray menu is built on mouse up (TrayMenuOpenBehavior; the library has no menu-open event).</summary>
    [RelayCommand]
    private void PrepareMenu()
    {
        // Task.Run: the use case must not resume on the UI thread that waits here.
        Task.Run(() => _readPowerState.ExecuteAsync()).Wait(MenuReadTimeout);
        RefreshItems();
    }

    [RelayCommand(CanExecute = nameof(CanToggle))]
    private Task ToggleAsync() => RunPowerActionAsync(() => _toggleSlowDown.ExecuteAsync(CancellationToken.None));

    /// <summary>Restore (through the gate), one popup on error, then quit; the quit also happens when the restore or the popup throws.</summary>
    [RelayCommand(CanExecute = nameof(CanRunPowerAction))]
    private async Task ExitAsync()
    {
        var started = false;

        try
        {
            await RunPowerActionAsync(() =>
            {
                started = true;
                return _restoreSpeedOnExit.ExecuteAsync(CancellationToken.None);
            });
        }
        finally
        {
            // Not started = another power action runs (the gate said no): do not quit under it.
            if (started)
            {
                _windowService.Quit();
            }
        }
    }

    private async Task RunPowerActionAsync(Func<Task<PowerActionResult>> action)
    {
        var result = await _gate.TryRunAsync(action);

        if (result is { IsSuccess: false })
        {
            await ShowErrorAsync(result);
        }
    }

    [RelayCommand]
    private void OpenSettings() => _windowService.ShowSettings();

    private bool CanRunPowerAction() => !_gate.IsBusy;

    // Only Slow Down / Restore Speed need a supported system; Exit stays enabled in limited mode and still quits.
    private bool CanToggle() => _systemSupport.IsSupported && CanRunPowerAction();

    private void RefreshItems()
    {
        OnPropertyChanged(nameof(ToggleText));
        OnPropertyChanged(nameof(IconKind));
        OnPropertyChanged(nameof(TooltipText));
        ToggleCommand.NotifyCanExecuteChanged();
        ExitCommand.NotifyCanExecuteChanged();
    }

    private Task ShowErrorAsync(PowerActionResult result) =>
        _dialogService.ShowMessageAsync(_localizer.Get(TextKeys.AppTitle), ErrorMessage.Build(result, _localizer));
}
