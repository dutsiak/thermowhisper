#if DISABLE_XAML_GENERATED_MAIN
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;

using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.WinUI;

/// <summary>
/// Custom entry point (DISABLE_XAML_GENERATED_MAIN): the single-instance check runs before the MAUI app is built.
/// Pattern: https://learn.microsoft.com/windows/apps/windows-app-sdk/applifecycle/applifecycle-single-instance
/// </summary>
public static class Program
{
    private const string InstanceKey = "ThermoWhisper.Main";
    private const uint RedirectTimeoutMs = 3000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

    [DllImport("kernel32.dll")]
    private static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("ole32.dll")]
    private static extern uint CoWaitForMultipleObjects(uint dwFlags, uint dwMilliseconds, uint nHandles, IntPtr[] pHandles, out uint dwIndex);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [STAThread]
    private static int Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        if (IsRedirectedToRunningInstance())
        {
            return 0;
        }

        Microsoft.UI.Xaml.Application.Start(callbackParams =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _ = new App();
        });

        return 0;
    }

    private static bool IsRedirectedToRunningInstance()
    {
        var keyInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += (_, _) => ShowSettingsOfRunningApp();
            return false;
        }

        RedirectActivationTo(AppInstance.GetCurrent().GetActivatedEventArgs(), keyInstance);
        return true;
    }

    // Runs on a thread-pool thread, never on the UI thread: WindowService marshals itself.
    // Before the app is built (very early start) there is no service yet; the window shows on start anyway.
    private static void ShowSettingsOfRunningApp() =>
        IPlatformApplication.Current?.Services.GetService<IWindowService>()?.ShowSettings();

    // The redirect runs on another thread; the STA thread waits with a non-blocking COM wait.
    // SetEvent is in finally: a failed redirect must not leave this process waiting forever.
    private static void RedirectActivationTo(AppActivationArguments arguments, AppInstance keyInstance)
    {
        var redirectDone = CreateEvent(IntPtr.Zero, true, false, null);

        Task.Run(() =>
        {
            try
            {
                keyInstance.RedirectActivationToAsync(arguments).AsTask().Wait();
            }
            finally
            {
                SetEvent(redirectDone);
            }
        });

        var waitResult = CoWaitForMultipleObjects(0, RedirectTimeoutMs, 1, [redirectDone], out _);
        // On timeout the redirect task may still use the handle: leave it to the OS (the process exits next).
        if (waitResult == 0)
        {
            CloseHandle(redirectDone);
        }

        BringRunningWindowToFront(keyInstance);
    }

    // The running copy also raises its own window from the Activated event; this is only a help
    // and does nothing when that window is hidden (MainWindowHandle is zero) or the process is gone.
    private static void BringRunningWindowToFront(AppInstance keyInstance)
    {
        try
        {
            using var process = Process.GetProcessById((int)keyInstance.ProcessId);
            SetForegroundWindow(process.MainWindowHandle);
        }
        // The running copy exited during the redirect: nothing to bring to the front.
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
        }
    }
}
#endif
