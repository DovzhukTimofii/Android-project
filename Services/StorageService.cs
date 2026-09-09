using MauiStartup.Models;
using System.Text.Json;

namespace MauiStartup.Services;

public class StorageService
{
    private readonly string _dataDirectory;
    private readonly string _postersDirectory;
    private readonly string _moviesFile;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public StorageService()
    {
        _dataDirectory = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "MovieCatalog");

        _postersDirectory = Path.Combine(
            _dataDirectory,
            "Posters");

        _moviesFile = Path.Combine(
            _dataDirectory,
            "movies.json");

        Directory.CreateDirectory(_dataDirectory);
        Directory.CreateDirectory(_postersDirectory);
    }

    public async Task<string> SavePosterAsync(
        Stream sourceStream,
        string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var newFileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_postersDirectory, newFileName);

        await using var destination = File.Create(destinationPath);
        await sourceStream.CopyToAsync(destination);

        return destinationPath;
    }

    public async Task SaveMoviesAsync(IEnumerable<Movie> movies)
    {
        var userMovies = movies
            .Where(movie => movie.IsUserCreated)
            .ToList();

        var json = JsonSerializer.Serialize(userMovies, _jsonOptions);
        await File.WriteAllTextAsync(_moviesFile, json);
    }

    public async Task<List<Movie>> LoadMoviesAsync()
    {
        if (!File.Exists(_moviesFile))
        {
            return new List<Movie>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_moviesFile);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<Movie>();
            }

            return JsonSerializer.Deserialize<List<Movie>>(json, _jsonOptions)
                   ?? new List<Movie>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Помилка читання локального сховища: {ex.Message}");

            return new List<Movie>();
        }
    }

    public Task DeletePosterAsync(string posterPath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(posterPath) && File.Exists(posterPath))
            {
                File.Delete(posterPath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Помилка видалення постера: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
