using MauiStartup.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MauiStartup.Services;

public class AuthService
{
    // Replace this value with the OAuth client ID created for this app in Google Cloud.
    public const string GoogleClientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";

    public const string RedirectScheme = "com.companyname.mauistartup";
    public const string RedirectUri = RedirectScheme + ":/oauth2redirect";

    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    private const string RefreshTokenKey = "google_refresh_token";
    private const string AccessTokenKey = "google_access_token";
    private const string IdTokenKey = "google_id_token";

    private const string UserIdKey = "auth_user_id";
    private const string UserNameKey = "auth_user_name";
    private const string UserEmailKey = "auth_user_email";
    private const string UserPictureKey = "auth_user_picture";

    private readonly HttpClient _httpClient;
    private TaskCompletionSource<Uri?>? _callbackSource;
    private string? _pendingState;
    private string? _pendingCodeVerifier;

    public AuthUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;
    public bool IsConfigured => !GoogleClientId.StartsWith("YOUR_GOOGLE_CLIENT_ID", StringComparison.Ordinal);

    public event EventHandler? AuthenticationChanged;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task InitializeAsync()
    {
        LoadPublicProfile();

        var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            if (CurrentUser != null)
            {
                ClearPublicProfile();
                CurrentUser = null;
            }

            AuthenticationChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!IsConfigured)
        {
            // Keep the UI signed out until a real Google OAuth client ID is configured.
            CurrentUser = null;
            AuthenticationChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        try
        {
            await RefreshTokensAsync(refreshToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Не вдалося оновити Google token: {ex.Message}");
            await LogoutAsync();
        }
    }

    public async Task LoginAsync()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Спочатку вкажіть Google OAuth Client ID у Services/AuthService.cs.");
        }

        _pendingState = CreateRandomUrlSafeString(32);
        _pendingCodeVerifier = CreateRandomUrlSafeString(64);
        var challenge = CreateCodeChallenge(_pendingCodeVerifier);

        var query = new Dictionary<string, string>
        {
            ["client_id"] = GoogleClientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["state"] = _pendingState,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256"
        };

        var authorizationUrl = AuthorizationEndpoint + "?" + string.Join(
            "&",
            query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        _callbackSource = new TaskCompletionSource<Uri?>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await Browser.Default.OpenAsync(
            new Uri(authorizationUrl),
            BrowserLaunchMode.SystemPreferred);

        var callbackUri = await _callbackSource.Task;
        if (callbackUri == null)
        {
            throw new InvalidOperationException("Google не повернув результат авторизації.");
        }

        var parameters = ParseQuery(callbackUri.Query);

        if (parameters.TryGetValue("error", out var error))
        {
            throw new InvalidOperationException($"Google OAuth: {error}");
        }

        if (!parameters.TryGetValue("state", out var returnedState) ||
            returnedState != _pendingState)
        {
            throw new InvalidOperationException("Некоректний OAuth state.");
        }

        if (!parameters.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Код авторизації не отримано.");
        }

        await ExchangeCodeAsync(code, _pendingCodeVerifier!);
    }

    public void HandleOAuthCallback(Uri uri)
    {
        if (!string.Equals(uri.Scheme, RedirectScheme, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _callbackSource?.TrySetResult(uri);
    }

    public async Task LogoutAsync()
    {
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(IdTokenKey);

        ClearPublicProfile();
        CurrentUser = null;

        await Task.CompletedTask;
        AuthenticationChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ExchangeCodeAsync(string code, string codeVerifier)
    {
        var body = new Dictionary<string, string>
        {
            ["client_id"] = GoogleClientId,
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["redirect_uri"] = RedirectUri,
            ["grant_type"] = "authorization_code"
        };

        using var response = await _httpClient.PostAsync(
            TokenEndpoint,
            new FormUrlEncodedContent(body));

        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        await StoreTokenResponseAsync(json, preserveExistingRefreshToken: false);
    }

    private async Task RefreshTokensAsync(string refreshToken)
    {
        var body = new Dictionary<string, string>
        {
            ["client_id"] = GoogleClientId,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        };

        using var response = await _httpClient.PostAsync(
            TokenEndpoint,
            new FormUrlEncodedContent(body));

        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        await StoreTokenResponseAsync(json, preserveExistingRefreshToken: true);
    }

    private async Task StoreTokenResponseAsync(
        string json,
        bool preserveExistingRefreshToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var accessToken = GetString(root, "access_token");
        var idToken = GetString(root, "id_token");
        var refreshToken = GetString(root, "refresh_token");

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        }

        if (!string.IsNullOrWhiteSpace(idToken))
        {
            await SecureStorage.Default.SetAsync(IdTokenKey, idToken);
            CurrentUser = ParseIdToken(idToken);
            SavePublicProfile(CurrentUser);
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }
        else if (!preserveExistingRefreshToken)
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
        }

        if (CurrentUser == null)
        {
            throw new InvalidOperationException("Не вдалося отримати профіль Google.");
        }

        AuthenticationChanged?.Invoke(this, EventArgs.Empty);
    }

    private static AuthUser ParseIdToken(string idToken)
    {
        var parts = idToken.Split('.');
        if (parts.Length < 2)
        {
            throw new InvalidOperationException("Google повернув некоректний ID token.");
        }

        var payloadBytes = Base64UrlDecode(parts[1]);
        using var document = JsonDocument.Parse(payloadBytes);
        var payload = document.RootElement;

        return new AuthUser
        {
            Id = GetString(payload, "sub"),
            Name = GetString(payload, "name"),
            Email = GetString(payload, "email"),
            PictureUrl = GetString(payload, "picture")
        };
    }

    private void LoadPublicProfile()
    {
        var id = Preferences.Default.Get(UserIdKey, string.Empty);
        if (string.IsNullOrWhiteSpace(id))
        {
            CurrentUser = null;
            return;
        }

        CurrentUser = new AuthUser
        {
            Id = id,
            Name = Preferences.Default.Get(UserNameKey, string.Empty),
            Email = Preferences.Default.Get(UserEmailKey, string.Empty),
            PictureUrl = Preferences.Default.Get(UserPictureKey, string.Empty)
        };
    }

    private static void SavePublicProfile(AuthUser user)
    {
        Preferences.Default.Set(UserIdKey, user.Id);
        Preferences.Default.Set(UserNameKey, user.Name);
        Preferences.Default.Set(UserEmailKey, user.Email);
        Preferences.Default.Set(UserPictureKey, user.PictureUrl);
    }

    private static void ClearPublicProfile()
    {
        Preferences.Default.Remove(UserIdKey);
        Preferences.Default.Remove(UserNameKey);
        Preferences.Default.Remove(UserEmailKey);
        Preferences.Default.Remove(UserPictureKey);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        return query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1].Replace('+', ' ')));
    }

    private static string CreateRandomUrlSafeString(int byteCount)
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));
    }

    private static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += normalized.Length % 4 switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };

        return Convert.FromBase64String(normalized);
    }
}
