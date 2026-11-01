using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ThermoWhisper.Application;
using ThermoWhisper.Application.Abstractions;
using ThermoWhisper.Application.Power;
using ThermoWhisper.Application.Settings;
using ThermoWhisper.Application.UseCases;
using ThermoWhisper.Domain.ValueObjects;

namespace ThermoWhisper.ViewModels;

/// <summary>Settings window: the quiet profile (UC-3) and the real current limit (UC-4).</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly PowerStatus _status;
    private readonly LoadSettings _loadSettings;
    private readonly ReadPowerState _readPowerState;
    private readonly ChangeQuietProfile _changeQuietProfile;
    private readonly PowerActionGate _gate;
    private readonly IWindowService _windowService;
    private readonly ILocalizer _localizer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    public partial CpuLimit? SelectedQuietProfile { get; set; }

    [ObservableProperty]
    public partial string CurrentLimitText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    public SettingsViewModel(
        PowerStatus status,
        LoadSettings loadSettings,
        ReadPowerState readPowerState,
        ChangeQuietProfile changeQuietProfile,
        PowerActionGate gate,
        IWindowService windowService,
        ILocalizer localizer)
    {
        _status = status;
        _loadSettings = loadSettings;
        _readPowerState = readPowerState;
        _changeQuietProfile = changeQuietProfile;
        _gate = gate;
        _windowService = windowService;
        _localizer = localizer;

        RefreshCurrentLimit();

        _status.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshCurrentLimit);
        _gate.BusyChanged += (_, _) => MainThread.BeginInvokeOnMainThread(ApplyCommand.NotifyCanExecuteChanged);
        _windowService.SettingsVisibilityChanged += OnSettingsVisibilityChanged;
    }

    public IReadOnlyList<CpuLimit> QuietProfiles { get; } = QuietProfile.Options;

    /// <summary>Runs on every show: the first display (NavigatedTo) and every later one (SettingsVisibilityChanged).</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        SelectedQuietProfile = _loadSettings.Execute().QuietProfile;
        StatusText = string.Empty;

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

    private bool CanApply() => SelectedQuietProfile is not null && !_gate.IsBusy;

    private void OnSettingsVisibilityChanged(object? sender, EventArgs e)
    {
        if (_windowService.IsSettingsVisible)
        {
            LoadCommand.Execute(null);
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
