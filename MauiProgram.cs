using MauiStartup.Pages;
using MauiStartup.Services;
using Microsoft.Extensions.Logging;
using MauiStartup.ViewModels;

namespace MauiStartup;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
    builder.Services.AddSingleton<UserMoviesService>();

    builder.Services.AddTransient<AddMoviePageModel>();
    builder.Services.AddTransient<AddMoviePage>();
    builder.Services.AddSingleton<FavoritesService>();
    builder.Services.AddTransient<MoviesPageModel>();
    builder.Services.AddTransient<MoviesPage>();
    builder.Services.AddSingleton<NavigationService>();
    builder.Services.AddSingleton(new HttpClient
    {
        BaseAddress = new Uri("https://api.imdbapi.dev/")
    });

    builder.Services.AddSingleton<MovieService>();
    builder.Services.AddSingleton<AppStateService>();

    builder.Services.AddSingleton<MainPageModel>();
    builder.Services.AddSingleton<MainPage>();

    builder.Services.AddTransient<ProfilePage>();

    return builder.Build();
    }
}