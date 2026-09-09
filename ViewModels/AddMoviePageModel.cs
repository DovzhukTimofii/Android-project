using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class AddMoviePageModel : ObservableObject
{
    private readonly UserMoviesService _userMoviesService;

    public NewMovieDto Movie { get; } = new();

    [ObservableProperty]
    private string message = string.Empty;

    public event EventHandler<Movie>? Saved;

    public event EventHandler? Cancelled;

    public AddMoviePageModel(
        UserMoviesService userMoviesService)
    {
        _userMoviesService = userMoviesService;
    }

    [RelayCommand]
    private async Task SelectPosterAsync()
    {
        try
        {
            Message = string.Empty;

            var permission =
                await Permissions.RequestAsync<
                    Permissions.StorageRead>();

            if (permission != PermissionStatus.Granted)
            {
                Message =
                    "Доступ до файлів не надано.";
                return;
            }

            var result =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "Оберіть постер фільму",

                        FileTypes =
                            FilePickerFileType.Images
                    });

            if (result == null)
                return;


            var extension =
                Path.GetExtension(result.FileName);

            var fileName =
                $"poster_{Guid.NewGuid():N}{extension}";

            var localPath =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    fileName);

            await using var source =
                await result.OpenReadAsync();

            await using var destination =
                File.Create(localPath);

            await source.CopyToAsync(destination);

            Movie.PosterPath = localPath;

            Message = "Постер обрано.";
        }
        catch (Exception ex)
        {
            Message =
                $"Помилка вибору зображення: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Save()
    {
        Message = string.Empty;

        if (string.IsNullOrWhiteSpace(Movie.Title))
        {
            Message = "Введіть назву фільму.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Movie.ReleaseYear))
        {
            Message = "Введіть рік випуску.";
            return;
        }

        if (!int.TryParse(
                Movie.ReleaseYear,
                out var year))
        {
            Message =
                "Рік повинен бути числом.";
            return;
        }

        if (year < 1888 ||
            year > DateTime.Now.Year + 5)
        {
            Message =
                "Вкажіть коректний рік випуску.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
                Movie.PosterPath))
        {
            Message = "Оберіть постер.";
            return;
        }

        var createdMovie = new Movie
        {
            Id =
                $"user-{Guid.NewGuid():N}",

            Title =
                Movie.Title.Trim(),

            Description =
                string.IsNullOrWhiteSpace(
                    Movie.Description)
                    ? "Опис відсутній."
                    : Movie.Description.Trim(),

            ReleaseDate =
                year.ToString(),

            ImageUrl =
                Movie.PosterPath,

            IsUserCreated = true
        };

        _userMoviesService.AddMovie(createdMovie);

        Saved?.Invoke(this, createdMovie);
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}