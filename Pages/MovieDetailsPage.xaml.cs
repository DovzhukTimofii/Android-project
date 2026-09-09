using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.Pages;

public partial class MovieDetailsPage : ContentPage
{
    private readonly Movie _movie;
    private readonly MovieService _movieService;

    public MovieDetailsPage(
        Movie movie,
        MovieService movieService)
    {
        InitializeComponent();

        _movie = movie;
        _movieService = movieService;
        BindingContext = movie;

        UpdateFavoriteButton();
    }

    private async void OnFavoriteClicked(
        object sender,
        EventArgs e)
    {
        if (_movieService.IsFavorite(_movie))
        {
            await _movieService.RemoveFavoriteAsync(_movie);
        }
        else
        {
            await _movieService.AddFavoriteAsync(_movie);
        }

        UpdateFavoriteButton();
    }

    private void UpdateFavoriteButton()
    {
        FavoriteButton.Text = _movieService.IsFavorite(_movie)
            ? "Видалити з улюблених"
            : "Додати до улюблених";
    }

    private async void OnDeleteMovieClicked(
        object sender,
        EventArgs e)
    {
        if (!_movie.IsUserCreated)
        {
            return;
        }

        var confirmed = await DisplayAlert(
            "Видалення",
            "Видалити цей фільм зі сховища?",
            "Так",
            "Ні");

        if (!confirmed)
        {
            return;
        }

        await _movieService.DeleteUserMovieAsync(_movie);
        await Navigation.PopModalAsync();
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
