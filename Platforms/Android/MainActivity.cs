using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using MauiStartup.Services;

namespace MauiStartup;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = AuthService.RedirectScheme,
    DataPath = "/oauth2redirect")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleOAuthIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleOAuthIntent(intent);
    }

    private static void HandleOAuthIntent(Intent? intent)
    {
        var data = intent?.DataString;
        if (string.IsNullOrWhiteSpace(data) || !Uri.TryCreate(data, UriKind.Absolute, out var uri))
        {
            return;
        }

        var authService = IPlatformApplication.Current?.Services.GetService<AuthService>();
        authService?.HandleOAuthCallback(uri);
    }
}
