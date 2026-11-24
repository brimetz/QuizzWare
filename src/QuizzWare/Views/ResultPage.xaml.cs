using QuizzWare.Services;
using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class ResultPage : ContentPage
{
    private static readonly string[] ConfettiEmojis = ["🎉", "✨", "⭐", "🎊", "💜", "🟨", "🟪"];

    private readonly ResultViewModel _viewModel;
    private readonly AppearanceService _appearance;
    private bool _played;

    public ResultPage(ResultViewModel viewModel, AppearanceService appearance)
    {
        InitializeComponent();
        _appearance = appearance;
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_played)
            return;
        _played = true;

        _ = _viewModel.PlayEndSoundAsync();
        if (_appearance.Current.ReduceMotion)
            return; // Accessibilité : ni rebond du score, ni confettis.

        // Le score « saute » à l'arrivée.
        ScoreBlock.Scale = 0.6;
        ScoreBlock.Opacity = 0;
        await Task.WhenAll(ScoreBlock.FadeToAsync(1, 250), ScoreBlock.ScaleToAsync(1.08, 260, Easing.CubicOut));
        await ScoreBlock.ScaleToAsync(1, 160, Easing.CubicIn);

        if (_viewModel.ShouldCelebrate)
            await RainConfettiAsync();
    }

    private async Task RainConfettiAsync()
    {
        var width = Width > 0 ? Width : 400;
        var height = Height > 0 ? Height : 800;
        var random = Random.Shared;
        var falls = new List<Task>();

        for (var i = 0; i < 28; i++)
        {
            var label = new Label
            {
                Text = ConfettiEmojis[random.Next(ConfettiEmojis.Length)],
                FontSize = random.Next(16, 28),
                Rotation = random.Next(-40, 40),
            };
            AbsoluteLayout.SetLayoutBounds(label, new Rect(random.NextDouble() * (width - 30), -40, -1, -1));
            Confetti.Children.Add(label);
            falls.Add(FallAsync(label, height, random.Next(0, 500), (uint)random.Next(1400, 2400),random.Next(-60, 60)));
        }

        await Task.WhenAll(falls);
        Confetti.Children.Clear();
    }

    private static async Task FallAsync(Label label, double height, int delay, uint duration, int drift)
    {
        await Task.Delay(delay);
        await Task.WhenAll(
            label.TranslateToAsync(drift, height + 80, duration, Easing.CubicIn),
            label.RotateToAsync(label.Rotation + drift * 4, duration),
            label.FadeToAsync(0.2, duration, Easing.CubicIn));
    }
}
