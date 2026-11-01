using System.Windows.Input;

using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace ThermoWhisper.Platforms.Windows;

/// <summary>
/// H.NotifyIcon.Maui 2.4.1 has no menu-open event and never runs RightClickCommand. The menu is built on right mouse up
/// from the items' current state, so this runs <see cref="Command"/> on right mouse down (UI thread, WndProc).
/// A MAUI behavior gets no BindingContext by itself, so it copies the one of the icon and follows its changes.
/// It also hands the icon to <see cref="WindowService"/>, which disposes it on Quit.
/// </summary>
public sealed class TrayMenuOpenBehavior : Behavior<TaskbarIcon>
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(TrayMenuOpenBehavior));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttachedTo(TaskbarIcon bindable)
    {
        base.OnAttachedTo(bindable);
        BindingContext = bindable.BindingContext;
        bindable.BindingContextChanged += OnIconBindingContextChanged;
        bindable.TrayIcon.MessageWindow.MouseEventReceived += OnMouseEventReceived;
        IPlatformApplication.Current?.Services.GetService<WindowService>()?.RegisterTrayIcon(bindable);
    }

    protected override void OnDetachingFrom(TaskbarIcon bindable)
    {
        bindable.BindingContextChanged -= OnIconBindingContextChanged;
        bindable.TrayIcon.MessageWindow.MouseEventReceived -= OnMouseEventReceived;
        BindingContext = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnIconBindingContextChanged(object? sender, EventArgs args) =>
        BindingContext = ((TaskbarIcon)sender!).BindingContext;

    private void OnMouseEventReceived(object? sender, MessageWindow.MouseEventReceivedEventArgs args)
    {
        if (args.MouseEvent == MouseEvent.IconRightMouseDown && Command?.CanExecute(null) == true)
        {
            Command.Execute(null);
        }
    }
}
