using MauiStartup.Models;

namespace MauiStartup.Services;

public class UserMoviesService
{
    private readonly List<Movie> _movies = new();

    public IReadOnlyList<Movie> Movies => _movies;

    public event EventHandler<Movie>? MovieAdded;

    public void AddMovie(Movie movie)
    {
        _movies.Add(movie);

        MovieAdded?.Invoke(this, movie);
    }
}