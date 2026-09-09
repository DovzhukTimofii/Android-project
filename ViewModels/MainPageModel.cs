using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class MainPageModel : ObservableObject
{
    private readonly AuthService _authService;
    private bool _initialized;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isAuthenticated;

    [ObservableProperty]
    private bool isAuthConfigured;

    [ObservableProperty]
    private string googleUserName = string.Empty;

    [ObservableProperty]
    private string googleUserEmail = string.Empty;

    [ObservableProperty]
    private string googleUserPicture = string.Empty;

    [ObservableProperty]
    private string authStatusText = "Вхід через Google не виконано";

    public MainPageModel(AuthService authService)
    {
        _authService = authService;
        _authService.AuthenticationChanged += OnAuthenticationChanged;
        UpdateAuthState();
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await _authService.InitializeAsync();
        UpdateAuthState();
    }

    [RelayCommand]
    private async Task GoogleLoginAsync()
    {
        try
        {
            Message = string.Empty;
            await _authService.LoginAsync();
            UpdateAuthState();
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
    }

    [RelayCommand]
    private async Task GoogleLogoutAsync()
    {
        await _authService.LogoutAsync();
        Message = "Вихід з Google-профілю виконано.";
        UpdateAuthState();
    }

    private void OnAuthenticationChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(UpdateAuthState);
    }

    private void UpdateAuthState()
    {
        IsAuthConfigured = _authService.IsConfigured;
        IsAuthenticated = _authService.IsAuthenticated;

        var user = _authService.CurrentUser;
        GoogleUserName = user?.Name ?? string.Empty;
        GoogleUserEmail = user?.Email ?? string.Empty;
        GoogleUserPicture = user?.PictureUrl ?? string.Empty;

        AuthStatusText = IsAuthenticated
            ? $"Виконано вхід: {GoogleUserName}"
            : IsAuthConfigured
                ? "Вхід через Google не виконано"
                : "Google OAuth потребує Client ID";
    }
}
