using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Services;
using System.Collections.ObjectModel;

namespace MauiStartup.ViewModels;

public partial class ProfilePageModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly MovieService _movieService;
    private readonly ILocalizationService _localization;
    private bool _changingLanguage;

    public ObservableCollection<string> Languages { get; } = new();

    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private string userName = string.Empty;
    [ObservableProperty] private string userEmail = string.Empty;
    [ObservableProperty] private string pictureUrl = string.Empty;
    [ObservableProperty] private string selectedLanguage = string.Empty;
    [ObservableProperty] private string pageTitle = string.Empty;
    [ObservableProperty] private string languageLabel = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private string favoritesText = string.Empty;
    [ObservableProperty] private string createdMoviesText = string.Empty;
    [ObservableProperty] private string signOutText = string.Empty;

    public ProfilePageModel(AuthService authService, MovieService movieService, ILocalizationService localization)
    {
        _authService = authService;
        _movieService = movieService;
        _localization = localization;
        _localization.CultureChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        _authService.AuthenticationChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Refresh);
        Refresh();
    }

    public void Refresh()
    {
        var user = _authService.CurrentUser;
        IsAuthenticated = user != null;
        UserName = user?.Name ?? string.Empty;
        UserEmail = user?.Email ?? string.Empty;
        PictureUrl = user?.PictureUrl ?? string.Empty;

        PageTitle = _localization.GetString("UserProfile");
        LanguageLabel = _localization.GetString("Language");
        StatusText = IsAuthenticated ? _localization.GetString("SignedInFormat", UserName) : _localization.GetString("NotSignedIn");
        FavoritesText = _localization.GetString("FavoritesCountFormat", _movieService.Favorites.Count);
        CreatedMoviesText = _localization.GetString("CreatedCountFormat", _movieService.UserMovies.Count);
        SignOutText = _localization.GetString("SignOut");

        _changingLanguage = true;
        Languages.Clear();
        Languages.Add(_localization.GetString("English"));
        Languages.Add(_localization.GetString("Ukrainian"));
        SelectedLanguage = _localization.CurrentLanguage == "uk" ? Languages[1] : Languages[0];
        _changingLanguage = false;
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        if (_changingLanguage || string.IsNullOrWhiteSpace(value)) return;

        var ukrainianLabel = _localization.GetString("Ukrainian");
        var code = value == ukrainianLabel ? "uk" : "en";
        _localization.SetCulture(code, _authService.CurrentUser?.Id);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        Refresh();
    }
}
