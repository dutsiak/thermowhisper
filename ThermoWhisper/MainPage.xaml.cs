using ThermoWhisper.ViewModels;

namespace ThermoWhisper;

public partial class MainPage : ContentPage
{
    public MainPage(TrayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
