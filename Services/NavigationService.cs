using MauiStartup.Models;
using MauiStartup.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace MauiStartup.Services;

public class NavigationService
{
    private readonly MovieService _movieService;
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(
        MovieService movieService,
        IServiceProvider serviceProvider)
    {
        _movieService = movieService;
        _serviceProvider = serviceProvider;
    }

    private Page? GetRootPage()
    {
        if (Application.Current?.Windows.Count == 0)
        {
            return null;
        }

        return Application.Current.Windows[0].Page;
    }

    public async Task OpenMovieDetailsAsync(Movie movie)
    {
        var rootPage = GetRootPage();

        if (rootPage == null)
        {
            return;
        }

        var page = new MovieDetailsPage(movie, _movieService);
        await rootPage.Navigation.PushModalAsync(page);
    }

    public async Task OpenAddMovieAsync()
    {
        var rootPage = GetRootPage();

        if (rootPage == null)
        {
            return;
        }

        var page = _serviceProvider.GetRequiredService<AddMoviePage>();
        await rootPage.Navigation.PushModalAsync(page);
    }
}
