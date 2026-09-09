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

        if (_viewModel.Movies.Count == 0 &&
            !_viewModel.IsFavoritesMode)
        {
            await _viewModel.LoadMoviesAsync();
        }
    }

    private void OnSwipeStarted(
        object sender,
        SwipeStartedEventArgs e)
    {
        /*
         * Во время свайпа запрещаем CollectionView
         * трактовать жест как выбор фильма.
         */
        MoviesCollection.SelectedItem = null;
        MoviesCollection.SelectionMode =
            SelectionMode.None;
    }

    private void OnSwipeEnded(
        object sender,
        SwipeEndedEventArgs e)
    {
        MoviesCollection.SelectedItem = null;

        MoviesCollection.SelectionMode =
            SelectionMode.Single;
    }
}