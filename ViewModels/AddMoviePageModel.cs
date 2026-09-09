using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStartup.Models;
using MauiStartup.Services;

namespace MauiStartup.ViewModels;

public partial class AddMoviePageModel : ObservableObject
{
    private readonly MovieService _movieService;
    private FileResult? _selectedPoster;

    public NewMovieDto Movie { get; } = new();

    [ObservableProperty]
    private string message = string.Empty;

    public event EventHandler<Movie>? Saved;
    public event EventHandler? Cancelled;

    public AddMoviePageModel(MovieService movieService)
    {
        _movieService = movieService;
    }

    [RelayCommand]
    private async Task SelectPosterAsync()
    {
        try
        {
            Message = string.Empty;

            var result = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Оберіть постер фільму",
                    FileTypes = FilePickerFileType.Images
                });

            if (result == null)
            {
                return;
            }

            _selectedPoster = result;
            Movie.PosterPath = result.FullPath;
            Message = "Постер обрано.";
        }
        catch (Exception ex)
        {
            Message = $"Помилка вибору зображення: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Message = string.Empty;

        if (string.IsNullOrWhiteSpace(Movie.Title))
        {
            Message = "Введіть назву фільму.";
            return;
        }

        if (!int.TryParse(Movie.ReleaseYear, out var year))
        {
            Message = "Рік повинен бути числом.";
            return;
        }

        if (year < 1888 || year > DateTime.Now.Year + 5)
        {
            Message = "Вкажіть коректний рік випуску.";
            return;
        }

        if (_selectedPoster == null)
        {
            Message = "Оберіть постер.";
            return;
        }

        try
        {
            await using var stream = await _selectedPoster.OpenReadAsync();

            var createdMovie = await _movieService.AddUserMovieAsync(
                Movie,
                stream,
                _selectedPoster.FileName);

            Saved?.Invoke(this, createdMovie);
        }
        catch (Exception ex)
        {
            Message = $"Помилка збереження: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
