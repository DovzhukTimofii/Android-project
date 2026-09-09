using MauiStartup.Pages;
using MauiStartup.Services;
using MauiStartup.ViewModels;
using Microsoft.Extensions.Logging;

namespace MauiStartup;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://api.imdbapi.dev/") });
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<StorageService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<MovieService>();
        builder.Services.AddSingleton<NavigationService>();
        builder.Services.AddSingleton<AppStateService>();
        builder.Services.AddTransient<MoviesPageModel>();
        builder.Services.AddTransient<MoviesPage>();
        builder.Services.AddTransient<AddMoviePageModel>();
        builder.Services.AddTransient<AddMoviePage>();
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<ProfilePageModel>();
        builder.Services.AddTransient<ProfilePage>();
        return builder.Build();
    }
}
