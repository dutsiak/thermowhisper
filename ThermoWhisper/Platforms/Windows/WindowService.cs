using Microsoft.UI.Windowing;

using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Platforms.Windows;

public sealed class WindowService : IWindowService
{
    public void ShowSettings() => MainThread.BeginInvokeOnMainThread(ShowOnUiThread);

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
