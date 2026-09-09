using System.Globalization;
using System.Resources;

namespace MauiStartup.Services;

public class LocalizationService : ILocalizationService
{
    private const string DefaultLanguage = "en";
    private const string PreferencePrefix = "language_";
    private readonly ResourceManager _resourceManager =
        new("MauiStartup.Resources.Localization.AppStrings", typeof(LocalizationService).Assembly);

    public string CurrentLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    public event EventHandler? CultureChanged;

    public LocalizationService()
    {
        RestoreCulture();
    }

    public string GetString(string key, params object[] args)
    {
        var value = _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }

    public void SetCulture(string languageCode, string? userId = null)
    {
        var normalized = languageCode.Equals("uk", StringComparison.OrdinalIgnoreCase)
            ? "uk"
            : DefaultLanguage;

        var culture = new CultureInfo(normalized);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        Preferences.Default.Set(GetPreferenceKey(userId), normalized);
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RestoreCulture(string? userId = null)
    {
        var key = GetPreferenceKey(userId);
        var language = Preferences.Default.Get(key, string.Empty);

        if (string.IsNullOrWhiteSpace(language) && !string.IsNullOrWhiteSpace(userId))
        {
            language = Preferences.Default.Get(GetPreferenceKey(null), DefaultLanguage);
        }

        if (string.IsNullOrWhiteSpace(language))
        {
            language = DefaultLanguage;
        }

        var culture = new CultureInfo(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private static string GetPreferenceKey(string? userId) =>
        string.IsNullOrWhiteSpace(userId)
            ? PreferencePrefix + "default"
            : PreferencePrefix + userId;
}
