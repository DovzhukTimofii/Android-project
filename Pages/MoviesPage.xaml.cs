using MauiStartup.ViewModels;

namespace MauiStartup.Pages;

public partial class MoviesPage : ContentPage
{
    private readonly MoviesPageModel _viewModel;

    public MoviesPage(MoviesPageModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        MoviesCollection.SelectedItem = null;
        await _viewModel.InitializeAsync();
    }

    private void OnSwipeStarted(
        object sender,
        SwipeStartedEventArgs e)
    {
        MoviesCollection.SelectedItem = null;
        MoviesCollection.SelectionMode = SelectionMode.None;
    }

    private void OnSwipeEnded(
        object sender,
        SwipeEndedEventArgs e)
    {
        MoviesCollection.SelectedItem = null;
        MoviesCollection.SelectionMode = SelectionMode.Single;
    }
}
