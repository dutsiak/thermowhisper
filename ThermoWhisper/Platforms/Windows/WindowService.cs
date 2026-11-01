using H.NotifyIcon;

using Microsoft.UI.Windowing;

using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Platforms.Windows;

public sealed class WindowService : IWindowService
{
    private TaskbarIcon? _trayIcon;
    private volatile bool _quitting;

    public void ShowSettings() => MainThread.BeginInvokeOnMainThread(ShowOnUiThread);

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
    }

    private void QuitOnUiThread()
    {
        _quitting = true;
        _trayIcon?.Dispose();
        Microsoft.Maui.Controls.Application.Current?.Quit();
    }

    private static void ShowOnUiThread()
    {
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();

        if (window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        if (nativeWindow.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
        {
            presenter.Restore();
        }

        nativeWindow.AppWindow.Show();
        nativeWindow.Activate();
    }
}
