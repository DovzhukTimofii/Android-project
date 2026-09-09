using MauiStartup.Models;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MauiStartup.Services;

public class AuthService
{
    // Replace with the OAuth client ID created for this app in Google Cloud.
    public const string GoogleClientId = "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com";

    public const string RedirectScheme = "com.companyname.mauistartup";
    public const string RedirectUri = RedirectScheme + ":/oauth2redirect";

    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";

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
    private bool _initialized;

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
        if (_initialized) return;
        _initialized = true;

        LoadPublicProfile();
        var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);

        if (string.IsNullOrWhiteSpace(refreshToken) || !IsConfigured)
        {
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
        var parameters = ParseQuery(callbackUri?.Query ?? string.Empty);

        if (parameters.TryGetValue("error", out var error))
        {
            throw new InvalidOperationException($"Google OAuth: {error}");
        }

        if (!parameters.TryGetValue("state", out var returnedState) || returnedState != _pendingState)
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
        if (string.Equals(uri.Scheme, RedirectScheme, StringComparison.OrdinalIgnoreCase))
        {
            _callbackSource?.TrySetResult(uri);
        }
    }

    public Task LogoutAsync()
    {
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(IdTokenKey);
        ClearPublicProfile();
        CurrentUser = null;
        AuthenticationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
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

        using var response = await _httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(body));
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

        using var response = await _httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(body));
        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        await StoreTokenResponseAsync(json, preserveExistingRefreshToken: true);
    }

    private async Task StoreTokenResponseAsync(string json, bool preserveExistingRefreshToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var accessToken = GetString(root, "access_token");
        var idToken = GetString(root, "id_token");
        var refreshToken = GetString(root, "refresh_token");

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Google не повернув access token.");
        }

        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);

        if (!string.IsNullOrWhiteSpace(idToken))
        {
            await SecureStorage.Default.SetAsync(IdTokenKey, idToken);
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        }
        else if (!preserveExistingRefreshToken)
        {
            SecureStorage.Default.Remove(RefreshTokenKey);
        }

        CurrentUser = await LoadUserProfileAsync(accessToken);
        SavePublicProfile(CurrentUser);
        AuthenticationChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task<AuthUser> LoadUserProfileAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(json);
        var profile = document.RootElement;

        return new AuthUser
        {
            Id = GetString(profile, "sub"),
            Name = GetString(profile, "name"),
            Email = GetString(profile, "email"),
            PictureUrl = GetString(profile, "picture")
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

    private static string GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static Dictionary<string, string> ParseQuery(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => Uri.UnescapeDataString(parts[1].Replace('+', ' ')));

    private static string CreateRandomUrlSafeString(int byteCount) =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));

    private static string CreateCodeChallenge(string verifier) =>
        Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
