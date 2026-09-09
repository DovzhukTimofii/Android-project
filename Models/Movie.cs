using CommunityToolkit.Mvvm.ComponentModel;

namespace MauiStartup.Models;

public partial class Movie : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ReleaseDate { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isFavorite;

    [ObservableProperty]
    private bool isUserCreated;
}