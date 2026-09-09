using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiStartup.Models;

public partial class NewMovieDto : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string releaseYear = string.Empty;

    [ObservableProperty]
    private string posterPath = string.Empty;
}