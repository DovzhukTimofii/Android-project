using MauiStartup.Services;

namespace MauiStartup.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly AppStateService _appStateService;

    public ProfilePage(AppStateService appStateService)
    {
        InitializeComponent();

        _appStateService = appStateService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_appStateService.IsRegistered)
        {
            StatusLabel.Text = "Вітаємо! Користувач успішно зареєстрований.";

            EmailLabel.Text = $"Електронна пошта: {_appStateService.UserEmail}";
            EmailLabel.IsVisible = true;
        }
        else
        {
            StatusLabel.Text =
                "Для перегляду контенту необхідно спочатку зареєструватися.";

            EmailLabel.IsVisible = false;
        }
    }
}