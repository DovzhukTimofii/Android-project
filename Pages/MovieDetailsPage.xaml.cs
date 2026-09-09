using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.Pages;

public partial class MovieDetailsPage : ContentPage
{
    private readonly Movie _movie;
    private readonly FavoritesService _favoritesService;

    public MovieDetailsPage(
        Movie movie,
        FavoritesService favoritesService)
    {
        InitializeComponent();

        _movie = movie;
        _favoritesService = favoritesService;

        BindingContext = movie;

        UpdateFavoriteButton();
    }

    private void OnFavoriteClicked(
        object sender,
        EventArgs e)
    {
        if (_favoritesService.IsFavorite(_movie))
        {
            _favoritesService.RemoveFavorite(_movie);
        }
        else
        {
            _favoritesService.AddFavorite(_movie);
        }

        UpdateFavoriteButton();
    }

    private void UpdateFavoriteButton()
    {
        if (_favoritesService.IsFavorite(_movie))
        {
            FavoriteButton.Text =
                "Видалити з улюблених";
        }
        else
        {
            FavoriteButton.Text =
                "Додати до улюблених";
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}