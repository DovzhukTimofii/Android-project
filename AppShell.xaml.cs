using MauiStartup.Services;

namespace MauiStartup;

public partial class AppShell : Shell
{
    private ILocalizationService? _localization;

    public AppShell()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _localization ??= Handler?.MauiContext?.Services.GetService<ILocalizationService>();
        if (_localization == null) return;

        _localization.CultureChanged -= OnCultureChanged;
        _localization.CultureChanged += OnCultureChanged;
        ApplyLocalization();
    }

    private void OnCultureChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(ApplyLocalization);

    private void ApplyLocalization()
    {
        if (_localization == null) return;
        HomeItem.Title = _localization.GetString("Home");
        ProfileItem.Title = _localization.GetString("Profile");
        MoviesItem.Title = _localization.GetString("Movies");
    }
}
