using ThermoWhisper.ViewModels;

namespace ThermoWhisper.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel settingsViewModel, TrayViewModel trayViewModel)
    {
        InitializeComponent();
        TrayIcon.BindingContext = trayViewModel;
        BindingContext = settingsViewModel;
    }
}
