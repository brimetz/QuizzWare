using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class QuizPage : ContentPage
{
    private readonly QuizViewModel _viewModel;

    public QuizPage(QuizViewModel viewModel)
    {
        InitializeComponent();
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
        await FeedbackPanel.TranslateTo(0, 0, 220, Easing.CubicOut);

        if (isCorrect)
        {
            await Bar.ScaleYTo(2.2, 120, Easing.CubicOut);
            await Bar.ScaleYTo(1, 160, Easing.CubicIn);
            return;
        }

        foreach (var x in new[] { -10.0, 10, -7, 7, -3, 0 })
            await FeedbackPanel.TranslateTo(x, 0, 45);
    }
}
