using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;
using System.Collections.ObjectModel;

namespace MauiStartup.ViewModels;

public partial class MoviesPageModel : ObservableObject
{
    private readonly MovieService _movieService;
    private readonly NavigationService _navigationService;
    private readonly List<Movie> _loadedMovies = new();

    private const int VisibleBatchSize = 10;
    private int _currentPage = 1;
    private int _visibleCount;
    private bool _initialized;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private Movie? selectedMovie;

    [ObservableProperty]
    private bool isFavoritesMode;

    [ObservableProperty]
    private string favoritesButtonText = "Показати улюблені";

    [ObservableProperty]
    private string swipeActionText = "До улюблених";

    public ObservableCollection<Movie> Movies { get; } = new();

    public MoviesPageModel(
        MovieService movieService,
        NavigationService navigationService)
    {
        _movieService = movieService;
        _navigationService = navigationService;

        _movieService.MovieAdded += OnMovieAdded;
        _movieService.MovieRemoved += OnMovieRemoved;
        _movieService.MovieChanged += OnMovieChanged;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _movieService.InitializeAsync();

        foreach (var movie in _movieService.UserMovies)
        {
            if (_loadedMovies.All(existing => existing.Id != movie.Id))
            {
                _loadedMovies.Add(movie);
            }
        }

        _initialized = true;
        await LoadMoviesAsync();
    }

    [RelayCommand]
    public async Task LoadMoviesAsync()
    {
        if (IsLoading || IsFavoritesMode)
        {
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Завантаження...";

            var movies = await _movieService.GetMoviesAsync(_currentPage);

            foreach (var movie in movies)
            {
                if (_loadedMovies.All(existing => existing.Id != movie.Id))
                {
                    _loadedMovies.Add(movie);
                }
            }

            AddNextVisibleBatch();

            StatusMessage = movies.Count == 0 && Movies.Count == 0
                ? "Дані не знайдено."
                : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка завантаження: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddNextVisibleBatch()
    {
        var remaining = _loadedMovies.Count - _visibleCount;

        if (remaining <= 0)
        {
            return;
        }

        var count = Math.Min(VisibleBatchSize, remaining);

        for (var i = 0; i < count; i++)
        {
            Movies.Add(_loadedMovies[_visibleCount]);
            _visibleCount++;
        }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsLoading || IsFavoritesMode)
        {
            return;
        }

        if (_visibleCount < _loadedMovies.Count)
        {
            AddNextVisibleBatch();
            return;
        }

        _currentPage++;
        await LoadMoviesAsync();
    }

    [RelayCommand]
    private async Task ManageFavoriteAsync(Movie? movie)
    {
        if (movie == null)
        {
            return;
        }

        if (IsFavoritesMode)
        {
            await _movieService.RemoveFavoriteAsync(movie);
            Movies.Remove(movie);
            return;
        }

        if (!_movieService.IsFavorite(movie))
        {
            await _movieService.AddFavoriteAsync(movie);
        }
    }

    [RelayCommand]
    private async Task AddMovieAsync()
    {
        await _navigationService.OpenAddMovieAsync();
    }

    [RelayCommand]
    private void ToggleFavorites()
    {
        IsFavoritesMode = !IsFavoritesMode;
        SelectedMovie = null;
        Movies.Clear();

        if (IsFavoritesMode)
        {
            FavoritesButtonText = "Показати всі";
            SwipeActionText = "Видалити";

            foreach (var movie in _movieService.Favorites)
            {
                Movies.Add(movie);
            }

            StatusMessage = Movies.Count == 0
                ? "Список улюблених порожній."
                : string.Empty;
        }
        else
        {
            FavoritesButtonText = "Показати улюблені";
            SwipeActionText = "До улюблених";
            _visibleCount = 0;
            AddNextVisibleBatch();
            StatusMessage = string.Empty;
        }
    }

    [RelayCommand]
    private async Task MovieSelectedAsync(Movie? movie)
    {
        if (movie == null)
        {
            return;
        }

        await _navigationService.OpenMovieDetailsAsync(movie);
        SelectedMovie = null;
    }

    private void OnMovieAdded(object? sender, Movie movie)
    {
        if (_loadedMovies.All(existing => existing.Id != movie.Id))
        {
            _loadedMovies.Insert(0, movie);
        }

        if (!IsFavoritesMode && Movies.All(existing => existing.Id != movie.Id))
        {
            Movies.Insert(0, movie);
            _visibleCount++;
        }
    }

    private void OnMovieRemoved(object? sender, Movie movie)
    {
        var removedIndex = _loadedMovies.FindIndex(existing => existing.Id == movie.Id);
        if (removedIndex >= 0)
        {
            _loadedMovies.RemoveAt(removedIndex);

            if (removedIndex < _visibleCount && _visibleCount > 0)
            {
                _visibleCount--;
            }
        }

        Movies.Remove(movie);
    }

    private void OnMovieChanged(object? sender, Movie movie)
    {
        if (IsFavoritesMode && !_movieService.IsFavorite(movie))
        {
            Movies.Remove(movie);
        }
    }
}
