using System.Globalization;

namespace MauiStartup.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    event EventHandler? CultureChanged;
    string GetString(string key, params object[] args);
    void SetCulture(string languageCode, string? userId = null);
    void RestoreCulture(string? userId = null);
}
