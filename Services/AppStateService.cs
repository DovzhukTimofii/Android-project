namespace MauiStartup.Services;

public class AppStateService
{
    public bool IsRegistered { get; set; } = false;

    public string UserEmail { get; set; } = string.Empty;
}