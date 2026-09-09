using MauiStartup.Models;
using MauiStartup.ViewModels;

namespace MauiStartup.Pages;

public partial class AddMoviePage : ContentPage
{
    private readonly AddMoviePageModel _viewModel;

    public AddMoviePage(
        AddMoviePageModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.Saved += OnSaved;
        _viewModel.Cancelled += OnCancelled;
    }

    private async void OnSaved(
        object? sender,
        Movie movie)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCancelled(
        object? sender,
        EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _viewModel.Saved -= OnSaved;
        _viewModel.Cancelled -= OnCancelled;
    }
}