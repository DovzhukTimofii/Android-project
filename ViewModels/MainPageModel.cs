using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MainPageModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly ILocalizationService _localization;
    private bool _initialized;

    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private bool isAuthenticated;
    [ObservableProperty] private bool isSignedOut = true;
    [ObservableProperty] private bool isAuthConfigured;
    [ObservableProperty] private string googleUserName = string.Empty;
    [ObservableProperty] private string googleUserEmail = string.Empty;
    [ObservableProperty] private string googleUserPicture = string.Empty;
    [ObservableProperty] private string authStatusText = string.Empty;
    [ObservableProperty] private string welcomeText = string.Empty;
    [ObservableProperty] private string authHintText = string.Empty;
    [ObservableProperty] private string signInText = string.Empty;
    [ObservableProperty] private string signOutText = string.Empty;

    public MainPageModel(AuthService authService, ILocalizationService localization)
    {
        _authService = authService;
        _localization = localization;
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        _localization.CultureChanged += (_, _) => MainThread.BeginInvokeOnMainThread(UpdateAuthState);
        UpdateAuthState();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;
        await _authService.InitializeAsync();
        _localization.RestoreCulture(_authService.CurrentUser?.Id);
        UpdateAuthState();
    }

    [RelayCommand]
    private async Task GoogleLoginAsync()
    {
        try
        {
            Message = string.Empty;
            await _authService.LoginAsync();
            _localization.RestoreCulture(_authService.CurrentUser?.Id);
            UpdateAuthState();
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    [RelayCommand]
    private async Task GoogleLogoutAsync()
    {
        await _authService.LogoutAsync();
        Message = _localization.GetString("LogoutDone");
        UpdateAuthState();
    }

    [RelayCommand]
    private async Task OpenProfileAsync()
    {
        await Shell.Current.GoToAsync("//ProfilePage");
    }

    private void OnAuthenticationChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(UpdateAuthState);

    private void UpdateAuthState()
    {
        IsAuthConfigured = _authService.IsConfigured;
        IsAuthenticated = _authService.IsAuthenticated;
        IsSignedOut = !IsAuthenticated;
        var user = _authService.CurrentUser;
        GoogleUserName = user?.Name ?? string.Empty;
        GoogleUserEmail = user?.Email ?? string.Empty;
        GoogleUserPicture = user?.PictureUrl ?? string.Empty;
        WelcomeText = _localization.GetString("Welcome");
        AuthHintText = _localization.GetString("AuthHint");
        SignInText = _localization.GetString("GoogleSignIn");
        SignOutText = _localization.GetString("SignOut");
        AuthStatusText = IsAuthenticated
            ? _localization.GetString("SignedInFormat", GoogleUserName)
            : IsAuthConfigured ? _localization.GetString("SignedOut") : _localization.GetString("OAuthNotConfigured");
    }
}
