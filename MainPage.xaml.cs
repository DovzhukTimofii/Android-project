using MauiStartup.ViewModels;

namespace MauiStartup;

public partial class MainPage : ContentPage
{
    private readonly MainPageModel _viewModel;

    public MainPage(MainPageModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
