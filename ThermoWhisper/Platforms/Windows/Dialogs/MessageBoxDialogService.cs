using System.Runtime.InteropServices;

using ThermoWhisper.Application.Abstractions;

namespace ThermoWhisper.Platforms.Windows.Dialogs;

/// <summary>Win32 MessageBoxW: needs no app window, so it also works when the app lives only in the tray.</summary>
public sealed class MessageBoxDialogService : IDialogService
{
    private const uint MbOk = 0x0;
    private const uint MbSetForeground = 0x10000;
    private const uint MbTopMost = 0x40000;

    // The call blocks until the user closes the box, so it runs on a pool thread: safe from any thread, the UI thread stays free.
    public Task ShowMessageAsync(string title, string message) =>
        Task.Run(() => MessageBoxW(IntPtr.Zero, message, title, MbOk | MbSetForeground | MbTopMost));

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
