using MauiStartup.Models;
using System.Net.Http.Json;

namespace MauiStartup.Services;

public class MovieService
{
    private readonly HttpClient _httpClient;
    private readonly StorageService _storageService;

    private readonly List<Movie> _apiMovies = new();
    private readonly List<Movie> _userMovies = new();
    private readonly HashSet<string> _favoriteIds = new();

    private bool _initialized;

    public event EventHandler<Movie>? MovieAdded;
    public event EventHandler<Movie>? MovieRemoved;
    public event EventHandler<Movie>? MovieChanged;

    public MovieService(HttpClient httpClient, StorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public IReadOnlyList<Movie> UserMovies => _userMovies;

    public IReadOnlyList<Movie> AllLoadedMovies =>
        _userMovies.Concat(_apiMovies).ToList();

    public IReadOnlyList<Movie> Favorites =>
        AllLoadedMovies.Where(IsFavorite).ToList();

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        var storedMovies = await _storageService.LoadMoviesAsync();

        foreach (var movie in storedMovies)
        {
            movie.IsUserCreated = true;

            if (movie.IsFavorite)
            {
                _favoriteIds.Add(movie.Id);
            }

            if (_userMovies.All(existing => existing.Id != movie.Id))
            {
                _userMovies.Add(movie);
            }
        }

        _initialized = true;
    }

    public async Task<List<Movie>> GetMoviesAsync(int page = 1)
    {
        await InitializeAsync();

        try
        {
            var response = await _httpClient
                .GetFromJsonAsync<ImdbApiResponse>($"titles?page={page}");

            if (response == null)
            {
                return new List<Movie>();
            }

            var movies = response.Titles.Select(title => new Movie
            {
                Id = title.Id,
                Title = string.IsNullOrWhiteSpace(title.PrimaryTitle)
                    ? "Без назви"
                    : title.PrimaryTitle,
                Description = string.IsNullOrWhiteSpace(title.Plot)
                    ? "Опис відсутній."
                    : title.Plot,
                ReleaseDate = title.StartYear?.ToString() ?? "Рік невідомий",
                ImageUrl = title.PrimaryImage?.Url ?? string.Empty
            }).ToList();

            foreach (var movie in movies)
            {
                movie.IsFavorite = _favoriteIds.Contains(movie.Id);

                if (_apiMovies.All(existing => existing.Id != movie.Id))
                {
                    _apiMovies.Add(movie);
                }
            }

            return movies;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Помилка API: {ex.Message}");

            return new List<Movie>();
        }
    }

    public async Task<Movie> AddUserMovieAsync(
        NewMovieDto dto,
        Stream posterStream,
        string posterFileName)
    {
        await InitializeAsync();

        var posterPath = await _storageService.SavePosterAsync(
            posterStream,
            posterFileName);

        var movie = new Movie
        {
            Id = $"user-{Guid.NewGuid():N}",
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? "Опис відсутній."
                : dto.Description.Trim(),
            ReleaseDate = dto.ReleaseYear,
            ImageUrl = posterPath,
            IsUserCreated = true
        };

        _userMovies.Insert(0, movie);
        await SaveUserMoviesAsync();

        MovieAdded?.Invoke(this, movie);
        return movie;
    }

    public async Task DeleteUserMovieAsync(Movie movie)
    {
        if (!movie.IsUserCreated)
        {
            return;
        }

        _userMovies.RemoveAll(existing => existing.Id == movie.Id);
        _favoriteIds.Remove(movie.Id);

        await _storageService.DeletePosterAsync(movie.ImageUrl);
        await SaveUserMoviesAsync();

        MovieRemoved?.Invoke(this, movie);
    }

    public bool IsFavorite(Movie movie) =>
        _favoriteIds.Contains(movie.Id);

    public async Task AddFavoriteAsync(Movie movie)
    {
        _favoriteIds.Add(movie.Id);
        movie.IsFavorite = true;

        if (movie.IsUserCreated)
        {
            await SaveUserMoviesAsync();
        }

        MovieChanged?.Invoke(this, movie);
    }

    public async Task RemoveFavoriteAsync(Movie movie)
    {
        _favoriteIds.Remove(movie.Id);
        movie.IsFavorite = false;

        if (movie.IsUserCreated)
        {
            await SaveUserMoviesAsync();
        }

        MovieChanged?.Invoke(this, movie);
    }

    private Task SaveUserMoviesAsync() =>
        _storageService.SaveMoviesAsync(_userMovies);
}
