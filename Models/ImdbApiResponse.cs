using System.Text.Json.Serialization;

namespace MauiStartup.Models;

public class ImdbApiResponse
{
    [JsonPropertyName("titles")]
    public List<ImdbTitle> Titles { get; set; } = new();
}

public class ImdbTitle
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("primaryTitle")]
    public string PrimaryTitle { get; set; } = string.Empty;

    [JsonPropertyName("plot")]
    public string Plot { get; set; } = string.Empty;

    [JsonPropertyName("startYear")]
    public int? StartYear { get; set; }

    [JsonPropertyName("primaryImage")]
    public ImdbImage? PrimaryImage { get; set; }
}

public class ImdbImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}