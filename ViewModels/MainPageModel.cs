using MauiStartup.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace MauiStartup.ViewModels;

public partial class MainPageModel : ObservableObject
{
    private readonly AppStateService _appStateService;

    public MainPageModel(AppStateService appStateService)
    {
        _appStateService = appStateService;
    }
    private readonly Dictionary<string, string> _users = new()
    {
        { "student@gmail.com", "123456" },
        { "admin@gmail.com", "admin123" },
        { "test@gmail.com", "password" }
    };

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string message = string.Empty;

    [RelayCommand]
    private void CheckRegistration()
    {
        Message = string.Empty;

        if (string.IsNullOrWhiteSpace(Email))
        {
            Message = "Введіть електронну пошту.";
            return;
        }

        if (!Email.Contains("@") || !Email.Contains("."))
        {
            Message = "Некоректний формат електронної пошти.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            Message = "Введіть пароль.";
            return;
        }

        if (_users.TryGetValue(Email, out string? savedPassword))
        {
            if (savedPassword == Password)
            {
                _appStateService.IsRegistered = true;
                _appStateService.UserEmail = Email;

                Message = "Користувач зареєстрований.";
            }
            else
            {
                Message = "Неправильний пароль.";
            }
        }
        else
        {
            Message = "Користувача не знайдено.";
        }
    }
}