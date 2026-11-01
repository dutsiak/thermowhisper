using H.NotifyIcon;

using Microsoft.UI.Windowing;

using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Platforms.Windows;

public sealed class WindowService : IWindowService
{
    private TaskbarIcon? _trayIcon;
    private volatile bool _quitting;

    public bool IsSettingsVisible { get; private set; } = true;

    public event EventHandler? SettingsVisibilityChanged;

    public void ShowSettings() => MainThread.BeginInvokeOnMainThread(ShowOnUiThread);

    public void Hide() => MainThread.BeginInvokeOnMainThread(HideOnUiThread);

    /// <summary>Disposes the tray icon first so no ghost icon stays in the tray, then ends the app.</summary>
    public void Quit() => MainThread.BeginInvokeOnMainThread(QuitOnUiThread);

    public void RegisterTrayIcon(TaskbarIcon trayIcon) => _trayIcon = trayIcon;

    /// <summary>The close button (X) only hides the window; Quit is not blocked.</summary>
    public void HideInsteadOfClose(Microsoft.UI.Xaml.Window nativeWindow) =>
        nativeWindow.AppWindow.Closing += OnClosing;

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_quitting)
        {
            return;
        }

        args.Cancel = true;
        sender.Hide();
        SetVisible(false);
    }

    private void QuitOnUiThread()
    {
        _quitting = true;
        _trayIcon?.Dispose();
        Microsoft.Maui.Controls.Application.Current?.Quit();
    }

    private void ShowOnUiThread()
    {
        if (FindNativeWindow() is not { } nativeWindow)
        {
            return;
        }

        if (nativeWindow.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
        {
            presenter.Restore();
        }

        nativeWindow.AppWindow.Show();
        nativeWindow.Activate();
        SetVisible(true);
    }

    private void HideOnUiThread()
    {
        if (FindNativeWindow() is not { } nativeWindow)
        {
            return;
        }

        nativeWindow.AppWindow.Hide();
        SetVisible(false);
    }

    private static Microsoft.UI.Xaml.Window? FindNativeWindow() =>
        Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

    private void SetVisible(bool visible)
    {
        IsSettingsVisible = visible;
        SettingsVisibilityChanged?.Invoke(this, EventArgs.Empty);
    }
}
