using MauiStartup.Models;

namespace MauiStartup.Services;

public class FavoritesService
{
    private readonly Dictionary<string, Movie> _favorites = new();

    public IReadOnlyCollection<Movie> Favorites =>
        _favorites.Values;

    public bool IsFavorite(Movie movie)
    {
        return _favorites.ContainsKey(movie.Id);
    }

    public void AddFavorite(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Id))
            return;

        if (_favorites.ContainsKey(movie.Id))
            return;

        movie.IsFavorite = true;
        _favorites[movie.Id] = movie;
    }

    public void RemoveFavorite(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Id))
            return;

        movie.IsFavorite = false;
        _favorites.Remove(movie.Id);
    }

    public void ToggleFavorite(Movie movie)
    {
        if (IsFavorite(movie))
        {
            RemoveFavorite(movie);
        }
        else
        {
            AddFavorite(movie);
        }
    }
}