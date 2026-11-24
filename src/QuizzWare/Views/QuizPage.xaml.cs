using QuizzWare.Services;
using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _viewModel;
    private readonly AppearanceService _appearance;

    public QuizPage(QuizViewModel viewModel, AppearanceService appearance)
    {
        InitializeComponent();
        _appearance = appearance;
        BindingContext = _viewModel = viewModel;
        _viewModel.AnswerChecked += OnAnswerChecked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Pause();
    }

    /// <summary>La correction monte depuis le bas ; en cas d'erreur, elle tremble.</summary>
    private async void OnAnswerChecked(object? sender, bool isCorrect)
    {
        FeedbackPanel.AbortAnimation("slide");
        FeedbackPanel.TranslationY = 240;
        FeedbackPanel.TranslationX = 0;
        if (_appearance.Current.ReduceMotion)
        {
            FeedbackPanel.TranslationY = 0; // Accessibilité : la correction apparaît sans glisser ni trembler.
            return;
        }
        await FeedbackPanel.TranslateToAsync(0, 0, 220, Easing.CubicOut);

        if (isCorrect)
        {
            await Bar.ScaleYToAsync(2.2, 120, Easing.CubicOut);
            await Bar.ScaleYToAsync(1, 160, Easing.CubicIn);
            return;
        }

        foreach (var x in new[] { -10.0, 10, -7, 7, -3, 0 })
            await FeedbackPanel.TranslateToAsync(x, 0, 45);
    }
}
