using MauiStartup.Models;
using System.Net.Http.Json;

namespace MauiStartup.Services;

public class MovieService
{
    private readonly HttpClient _httpClient;

    public MovieService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Movie>> GetMoviesAsync(int page = 1)
    {
        try
        {
            var response = await _httpClient
                .GetFromJsonAsync<ImdbApiResponse>(
                    $"titles?page={page}"
                );

            if (response == null)
                return new List<Movie>();

            return response.Titles.Select(title => new Movie
            {
                Id = title.Id,

                Title = string.IsNullOrWhiteSpace(title.PrimaryTitle)
                    ? "Без назви"
                    : title.PrimaryTitle,

                Description = string.IsNullOrWhiteSpace(title.Plot)
                    ? "Опис відсутній."
                    : title.Plot,

                ReleaseDate = title.StartYear?.ToString()
                              ?? "Рік невідомий",

                ImageUrl = title.PrimaryImage?.Url
                           ?? string.Empty

            }).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Помилка API: {ex.Message}"
            );

            return new List<Movie>();
        }
    }
}