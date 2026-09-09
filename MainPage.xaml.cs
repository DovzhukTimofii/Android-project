using MauiStartup.ViewModels;

namespace MauiStartup;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}