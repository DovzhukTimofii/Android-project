using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;
using System.Collections.ObjectModel;

namespace MauiStartup.ViewModels;

public partial class MoviesPageModel : ObservableObject
{
    private readonly UserMoviesService _userMoviesService;
    private readonly MovieService _movieService;
    private readonly NavigationService _navigationService;
    private readonly FavoritesService _favoritesService;

    private readonly List<Movie> _loadedMovies = new();

    private const int VisibleBatchSize = 10;

    private int _currentPage = 1;
    private int _visibleCount;

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
        NavigationService navigationService,
        FavoritesService favoritesService,
        UserMoviesService userMoviesService)
    {
        _movieService = movieService;
        _navigationService = navigationService;
        _favoritesService = favoritesService;
        _userMoviesService = userMoviesService;

        _userMoviesService.MovieAdded +=
            OnUserMovieAdded;
    }

    [RelayCommand]
    public async Task LoadMoviesAsync()
    {
        if (IsLoading || IsFavoritesMode)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Завантаження...";

            var movies =
                await _movieService.GetMoviesAsync(_currentPage);

            if (movies.Count == 0)
            {
                StatusMessage = "Більше даних немає.";
                return;
            }

            foreach (var movie in movies)
            {
                if (_loadedMovies.All(x => x.Id != movie.Id))
                {
                    movie.IsFavorite =
                        _favoritesService.IsFavorite(movie);

                    _loadedMovies.Add(movie);
                }
            }

            AddNextVisibleBatch();

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Помилка завантаження: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnUserMovieAdded(
    object? sender,
    Movie movie)
    {   
        
        if (_loadedMovies.All(x => x.Id != movie.Id))
        {
            _loadedMovies.Insert(0, movie);
            _visibleCount++;
        }

        if (!IsFavoritesMode)
        {
            Movies.Insert(0, movie);
        }
    }

    private void AddNextVisibleBatch()
    {
        var remaining =
            _loadedMovies.Count - _visibleCount;

        if (remaining <= 0)
            return;

        var count =
            Math.Min(VisibleBatchSize, remaining);

        for (int i = 0; i < count; i++)
        {
            Movies.Add(_loadedMovies[_visibleCount]);
            _visibleCount++;
        }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsLoading || IsFavoritesMode)
            return;

        if (_visibleCount < _loadedMovies.Count)
        {
            AddNextVisibleBatch();
            return;
        }

        _currentPage++;

        await LoadMoviesAsync();
    }

    [RelayCommand]
    private void ManageFavorite(Movie? movie)
    {
        if (movie == null)
            return;

      
        if (IsFavoritesMode)
        {
            _favoritesService.RemoveFavorite(movie);
            Movies.Remove(movie);
            return;
        }

        if (!_favoritesService.IsFavorite(movie))
        {
            _favoritesService.AddFavorite(movie);
        }
    }

    [RelayCommand]
    private async Task AddMovieAsync()
    {
        await _navigationService
            .OpenAddMovieAsync();
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

            foreach (var movie in _favoritesService.Favorites)
            {
                Movies.Add(movie);
            }

            if (Movies.Count == 0)
            {
                StatusMessage =
                    "Список улюблених порожній.";
            }
            else
            {
                StatusMessage = string.Empty;
            }
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
            return;

        await _navigationService
            .OpenMovieDetailsAsync(movie);

        SelectedMovie = null;
    }
}