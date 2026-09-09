using MauiStartup.ViewModels;

namespace MauiStartup.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfilePageModel _viewModel;

    public ProfilePage(ProfilePageModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }
}
