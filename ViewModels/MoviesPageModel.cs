using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;
using System.Collections.ObjectModel;

namespace MauiStartup.ViewModels;

public partial class MoviesPageModel : ObservableObject
{
    private readonly MovieService _movieService; private readonly NavigationService _navigationService; private readonly AuthService _authService; private readonly ILocalizationService _localization; private readonly List<Movie> _loadedMovies = new();
    private const int VisibleBatchSize = 10; private int _currentPage = 1; private int _visibleCount; private bool _initialized;
    [ObservableProperty] private bool isLoading; [ObservableProperty] private string statusMessage = string.Empty; [ObservableProperty] private Movie? selectedMovie; [ObservableProperty] private bool isFavoritesMode; [ObservableProperty] private string favoritesButtonText = string.Empty; [ObservableProperty] private string swipeActionText = string.Empty; [ObservableProperty] private bool canUseProtectedFeatures; [ObservableProperty] private string accountStatusText = string.Empty; [ObservableProperty] private string pageTitle = string.Empty; [ObservableProperty] private string catalogTitle = string.Empty; [ObservableProperty] private string addMovieButtonText = string.Empty;
    public ObservableCollection<Movie> Movies { get; } = new();

    public MoviesPageModel(MovieService movieService, NavigationService navigationService, AuthService authService, ILocalizationService localization)
    { _movieService=movieService; _navigationService=navigationService; _authService=authService; _localization=localization; _movieService.MovieAdded+=OnMovieAdded; _movieService.MovieRemoved+=OnMovieRemoved; _movieService.MovieChanged+=OnMovieChanged; _authService.AuthenticationChanged+=OnAuthenticationChanged; _localization.CultureChanged += (_,_) => MainThread.BeginInvokeOnMainThread(RefreshLocalizedText); UpdateAuthState(); RefreshLocalizedText(); }

    public async Task InitializeAsync(){ if(!_initialized){ await _authService.InitializeAsync(); await _movieService.InitializeAsync(); foreach(var movie in _movieService.UserMovies) if(_loadedMovies.All(x=>x.Id!=movie.Id)) _loadedMovies.Add(movie); _initialized=true; await LoadMoviesAsync(); } UpdateAuthState(); }
    [RelayCommand] public async Task LoadMoviesAsync(){ if(IsLoading||IsFavoritesMode)return; try{IsLoading=true; StatusMessage=_localization.GetString("Loading"); var movies=await _movieService.GetMoviesAsync(_currentPage); foreach(var movie in movies) if(_loadedMovies.All(x=>x.Id!=movie.Id)) _loadedMovies.Add(movie); AddNextVisibleBatch(); StatusMessage=movies.Count==0&&Movies.Count==0?_localization.GetString("NoData"):string.Empty;}catch(Exception ex){StatusMessage=ex.Message;}finally{IsLoading=false;} }
    private void AddNextVisibleBatch(){var remaining=_loadedMovies.Count-_visibleCount;if(remaining<=0)return;var count=Math.Min(VisibleBatchSize,remaining);for(var i=0;i<count;i++){Movies.Add(_loadedMovies[_visibleCount]);_visibleCount++;}}
    [RelayCommand] public async Task LoadMoreAsync(){if(IsLoading||IsFavoritesMode)return;if(_visibleCount<_loadedMovies.Count){AddNextVisibleBatch();return;}_currentPage++;await LoadMoviesAsync();}
    [RelayCommand] private async Task ManageFavoriteAsync(Movie? movie){if(movie==null)return;if(!CanUseProtectedFeatures){StatusMessage=_localization.GetString("AuthenticationRequired");return;}if(IsFavoritesMode){await _movieService.RemoveFavoriteAsync(movie);Movies.Remove(movie);return;}if(!_movieService.IsFavorite(movie))await _movieService.AddFavoriteAsync(movie);}
    [RelayCommand] private async Task AddMovieAsync(){if(!CanUseProtectedFeatures){StatusMessage=_localization.GetString("AuthenticationRequired");return;}await _navigationService.OpenAddMovieAsync();}
    [RelayCommand] private void ToggleFavorites(){if(!CanUseProtectedFeatures){StatusMessage=_localization.GetString("AuthenticationRequired");return;}IsFavoritesMode=!IsFavoritesMode;SelectedMovie=null;Movies.Clear();if(IsFavoritesMode){foreach(var movie in _movieService.Favorites)Movies.Add(movie);StatusMessage=Movies.Count==0?_localization.GetString("FavoritesEmpty"):string.Empty;}else{_visibleCount=0;AddNextVisibleBatch();StatusMessage=string.Empty;}RefreshLocalizedText();}
    [RelayCommand] private async Task MovieSelectedAsync(Movie? movie){if(movie==null)return;await _navigationService.OpenMovieDetailsAsync(movie);SelectedMovie=null;}
    private void OnAuthenticationChanged(object? s,EventArgs e)=>MainThread.BeginInvokeOnMainThread(UpdateAuthState);
    private void UpdateAuthState(){CanUseProtectedFeatures=_authService.IsAuthenticated;AccountStatusText=CanUseProtectedFeatures?$"Google: {_authService.CurrentUser?.Name}":_localization.GetString("AuthHint");if(!CanUseProtectedFeatures&&IsFavoritesMode){IsFavoritesMode=false;Movies.Clear();_visibleCount=0;AddNextVisibleBatch();}RefreshLocalizedText();}
    private void RefreshLocalizedText(){PageTitle=_localization.GetString("Movies");CatalogTitle=_localization.GetString("MovieCatalog");AddMovieButtonText=_localization.GetString("AddMovie");FavoritesButtonText=_localization.GetString(IsFavoritesMode?"ShowAll":"ShowFavorites");SwipeActionText=_localization.GetString(IsFavoritesMode?"Remove":"AddToFavorites");if(!CanUseProtectedFeatures)AccountStatusText=_localization.GetString("AuthHint");}
    private void OnMovieAdded(object? s,Movie movie){if(_loadedMovies.All(x=>x.Id!=movie.Id))_loadedMovies.Insert(0,movie);if(!IsFavoritesMode&&Movies.All(x=>x.Id!=movie.Id)){Movies.Insert(0,movie);_visibleCount++;}}
    private void OnMovieRemoved(object? s,Movie movie){var i=_loadedMovies.FindIndex(x=>x.Id==movie.Id);if(i>=0){_loadedMovies.RemoveAt(i);if(i<_visibleCount&&_visibleCount>0)_visibleCount--;}Movies.Remove(movie);}
    private void OnMovieChanged(object? s,Movie movie){if(IsFavoritesMode&&!_movieService.IsFavorite(movie))Movies.Remove(movie);}
}
