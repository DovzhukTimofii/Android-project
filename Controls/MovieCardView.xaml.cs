using MauiStartup.Models;

namespace MauiStartup.Controls;

public partial class MovieCardView : ContentView
{
    private const uint AnimationDuration = 400;

    private static readonly HashSet<string> AnimatedMovieIds = new();

    public static readonly BindableProperty ModelProperty =
        BindableProperty.Create(
            nameof(Model),
            typeof(Movie),
            typeof(MovieCardView),
            default(Movie),
            BindingMode.OneWay,
            propertyChanged: OnModelChanged);

    public Movie? Model
    {
        get => (Movie?)GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public MovieCardView()
    {
        InitializeComponent();

        Opacity = 0;
        TranslationY = 40;
    }

    private static void OnModelChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        if (bindable is not MovieCardView view)
            return;

        if (newValue is not Movie movie)
            return;

        view.StartAppearanceAnimation(movie);
    }

    private void StartAppearanceAnimation(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.Id))
        {
            Opacity = 1;
            TranslationY = 0;
            return;
        }

        if (AnimatedMovieIds.Contains(movie.Id))
        {
            Opacity = 1;
            TranslationY = 0;
            return;
        }

        AnimatedMovieIds.Add(movie.Id);

        Opacity = 0;
        TranslationY = 40;

        _ = AnimateAppearingAsync();
    }

    private async Task AnimateAppearingAsync()
    {
        await Task.Delay(50);

        await Task.WhenAll(
            FadeTo(
                1,
                AnimationDuration,
                Easing.CubicOut),

            TranslateTo(
                0,
                0,
                AnimationDuration,
                Easing.CubicOut)
        );
    }
}