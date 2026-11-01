using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using ThermoWhisper.Application;
using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Compatibility;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Settings;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.ViewModels;

/// <summary>Settings window: the quiet profile (UC-3) and the real current limit (UC-4).</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly PowerStatus _status;
    private readonly SystemSupport _support;
    private readonly LoadSettings _loadSettings;
    private readonly ReadPowerState _readPowerState;
    private readonly ChangeQuietProfile _changeQuietProfile;
    private readonly PowerActionGate _gate;
    private readonly IWindowService _windowService;
    private readonly ILocalizer _localizer;
    private readonly StatusRefreshLoop _refreshLoop;
    private readonly ILogger<SettingsViewModel> _logger;
    private CancellationTokenSource? _refreshCts;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial CpuLimit? SelectedQuietProfile { get; set; }

    [ObservableProperty]
    public partial string CurrentLimitText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    public SettingsViewModel(
        PowerStatus status,
        SystemSupport support,
        LoadSettings loadSettings,
        ReadPowerState readPowerState,
        ChangeQuietProfile changeQuietProfile,
        PowerActionGate gate,
        IWindowService windowService,
        ILocalizer localizer,
        StatusRefreshLoop refreshLoop,
        AppStartup appStartup,
        ILogger<SettingsViewModel> logger)
    {
        _status = status;
        _support = support;
        _loadSettings = loadSettings;
        _readPowerState = readPowerState;
        _changeQuietProfile = changeQuietProfile;
        _gate = gate;
        _windowService = windowService;
        _localizer = localizer;
        _refreshLoop = refreshLoop;
        _logger = logger;

        RefreshCurrentLimit();

        _status.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshCurrentLimit);
        _gate.BusyChanged += (_, _) => MainThread.BeginInvokeOnMainThread(ApplyCommand.NotifyCanExecuteChanged);
        _windowService.SettingsVisibilityChanged += OnSettingsVisibilityChanged;
        // Singleton view model: it lives as long as the app, so the subscriptions are never removed.
        appStartup.Completed += (_, _) => MainThread.BeginInvokeOnMainThread(OnStartupCompleted);
    }

    public IReadOnlyList<CpuLimit> QuietProfiles { get; } = QuietProfile.Options;

    /// <summary>False until the startup check passed: in limited mode (and before the check ends) the dropdown and Apply are greyed.</summary>
    public bool IsEditable => _support.IsSupported;

    /// <summary>The check finished and failed. A missing result (check not finished) is not limited mode.</summary>
    private bool IsLimitedMode => _support.Result is { IsSupported: false };

    /// <summary>Runs on every show: the first display (NavigatedTo) and every later one (SettingsVisibilityChanged).</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        SelectedQuietProfile = _loadSettings.Execute().QuietProfile;
        StatusText = string.Empty;

        if (IsLimitedMode)
        {
            ShowLimitedMode();
            return;
        }

        RestartRefresh();

        // Before the check ends there is nothing to read yet: AppStartup does the first read.
        if (!IsEditable)
        {
            return;
        }

        var result = await _readPowerState.ExecuteAsync();

        RefreshCurrentLimit();

        if (!result.IsSuccess)
        {
            StatusText = ErrorMessage.Build(result, _localizer);
        }
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var quietProfile = SelectedQuietProfile!;
        var result = await _gate.TryRunAsync(() => _changeQuietProfile.ExecuteAsync(quietProfile));

        if (result is not null)
        {
            StatusText = result.IsSuccess ? _localizer.Get(TextKeys.SettingsSaved) : ErrorMessage.Build(result, _localizer);
        }
    }

    [RelayCommand]
    private void Close() => _windowService.Hide();

    private bool CanApply() => IsEditable && SelectedQuietProfile is not null && !_gate.IsBusy;

    /// <summary>Limited mode: the problems in the status line, no refresh loop, no read; the label keeps the last known value.</summary>
    private void ShowLimitedMode()
    {
        StopRefresh();
        StatusText = LimitedModeMessage.Build(_support.Result!, _localizer);
        RefreshCurrentLimit();
    }

    private void OnStartupCompleted()
    {
        OnPropertyChanged(nameof(IsEditable));
        ApplyCommand.NotifyCanExecuteChanged();

        if (IsLimitedMode)
        {
            ShowLimitedMode();
        }
        else
        {
            RefreshCurrentLimit();
        }
    }

    private void OnSettingsVisibilityChanged(object? sender, EventArgs e)
    {
        if (_windowService.IsSettingsVisible)
        {
            LoadCommand.Execute(null);
        }
        else
        {
            StopRefresh();
        }
    }

    /// <summary>Always at most one loop: the old one is cancelled first, which also restarts the 3-minute period.</summary>
    private void RestartRefresh()
    {
        StopRefresh();
        _refreshCts = new CancellationTokenSource();
        _ = RunRefreshAsync(_refreshCts.Token);
    }

    private void StopRefresh()
    {
        _refreshCts?.Cancel();
        _refreshCts?.Dispose();
        _refreshCts = null;
    }

    private async Task RunRefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _refreshLoop.RunAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The Settings refresh loop stopped unexpectedly.");
        }
    }

    private void RefreshCurrentLimit()
    {
        var display = CurrentLimitDisplay.From(_status.Current);

        CurrentLimitText = display switch
        {
            { AcPercent: { } ac, DcPercent: { } dc } => _localizer.Format(display.TextKey, ac, dc),
            { AcPercent: { } ac } => _localizer.Format(display.TextKey, ac),
            _ => _localizer.Get(display.TextKey),
        };
    }
}
