using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;
using QuizzWare.Services;
using QuizzWare.ViewModels.Questions;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

public partial class QuizViewModel(ContentRepository content, ProgressService progressService, QuizBuilder builder, FeedbackService feedback)
    : ObservableObject, IQueryAttributable
{
    private string? _categoryId;
    private string? _unitId;
    private bool _isReview;
    private bool _startedFromLesson;
    private QuizSession? _session;
    private UserSettings _settings = new();

    /// <summary>Levé après chaque vérification (true = bonne réponse) : la page anime la correction.</summary>
    public event EventHandler<bool>? AnswerChecked;

    [ObservableProperty]
    private bool isRetry;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string kind = string.Empty;

    [ObservableProperty]
    private string prompt = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAnswered))]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
    private bool isAnswered;

    [ObservableProperty]
    private string feedbackTitle = string.Empty;

    [ObservableProperty]
    private Color feedbackColor = Palette.Success;

    [ObservableProperty]
    private string explanation = string.Empty;

    [ObservableProperty]
    private string continueText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
    private QuestionViewModel? current;

    public bool IsNotAnswered => !IsAnswered;

    /// <summary>Contient uniquement la question courante : permet de choisir son template via BindableLayout.</summary>
    public ObservableCollection<QuestionViewModel> CurrentQuestion { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _categoryId = query.TryGetValue("categoryId", out var c) ? c as string : null;
        _unitId = query.TryGetValue("unitId", out var u) ? u as string : null;
        _isReview = query.TryGetValue("mode", out var m) && m as string == "review";
    }

    public async Task LoadAsync()
    {
        if (_session is not null)
            return;

        _settings = (await progressService.GetProgressAsync()).Settings;

        if (_isReview)
        {
            await LoadReviewAsync();
            return;
        }

        if (_categoryId is null)
            return;

        var category = content.GetCategory(_categoryId);

        switch (_unitId is null ? null : content.GetUnit(_categoryId, _unitId))
        {
            // Titres courts : la barre de navigation tronque les titres longs sur mobile.
            case Lesson lesson:
                _session = builder.ForLesson(category, lesson);
                _startedFromLesson = true;
                Title = content.FindSpecialization(category.Id, lesson.Id)?.Title ?? category.Name;
                break;
            case Challenge challenge:
                _session = builder.ForChallenge(category, challenge);
                Title = "🏆 Défi entretien";
                break;
            default:
                // L'entraînement pioche dans les leçons débloquées et s'adapte au niveau.
                var userProgress = await progressService.GetProgressAsync();
                var level = progressService.GetLevel(userProgress, category.Id).Level;
                _session = builder.ForPractice(category, level, progressService.GetUnlockedLessons(userProgress, category));
                Title = "⚡ Entraînement";
                break;
        }

        ShowCurrentQuestion();
    }

    /// <summary>Révision espacée : les questions dues aujourd'hui, toutes catégories confondues.</summary>
    private async Task LoadReviewAsync()
    {
        var userProgress = await progressService.GetProgressAsync();
        var due = progressService.GetDueReviews(userProgress);
        if (due.Count == 0)
        {
            await Shell.Current.DisplayAlert("Rien à réviser", "Toutes tes révisions du jour sont faites. Reviens demain !", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _session = builder.ForReview(due);
        Title = "🧠 Révisions";
        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        var question = _session!.Current;

        // Le langage (et, en révision, la catégorie) dépend de la question : une révision mélange les catégories.
        var location = content.FindQuestion(question.Source.Id);
        var language = location is null ? null : content.GetCodeLanguage(location.Category.Id, location.Unit.Id);

        if (Current is not null)
            Current.PropertyChanged -= OnCurrentChanged;
        Current = QuestionViewModel.Create(question, language);
        Current.PropertyChanged += OnCurrentChanged;
        CurrentQuestion.Clear();
        CurrentQuestion.Add(Current);

        IsRetry = _session.IsRetry;
        Kind = IsRetry ? "🔁 Erreur à revoir"
            : _isReview && location is not null ? $"{location.Category.Name} · {Current.Kind}"
            : Current.Kind;
        Prompt = question.Source.Prompt;
        ProgressText = IsRetry
            ? $"{_session.PendingRetries} à revoir"
            : $"{_session.CurrentIndex + 1} / {_session.Questions.Count}";
        UpdateProgress();
        IsAnswered = false;
    }

    /// <summary>La barre n'avance que pour les questions terminées : une erreur se rattrape avant la fin.</summary>
    private void UpdateProgress() => Progress = (double)_session!.SettledCount / _session.Questions.Count;

    private void OnCurrentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestionViewModel.CanSubmit))
            CheckCommand.NotifyCanExecuteChanged();
    }

    private bool CanCheck() => Current?.CanSubmit == true && !IsAnswered;

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private void Check()
    {
        if (_session is null || Current is null || IsAnswered)
            return;

        var wasRetry = _session.IsRetry;
        var retriesBefore = _session.PendingRetries;
        var result = _session.Submit(Current.BuildAnswer());
        var willComeBack = _session.PendingRetries > retriesBefore;
        Current.ShowCorrection(result.IsCorrect);

        FeedbackTitle = (result.IsCorrect, wasRetry) switch
        {
            (true, true) => "Bien rattrapé ! 💪",
            (true, false) => $"Bonne réponse !  +{result.XpEarned} XP",
            _ when willComeBack => "Pas tout à fait… elle reviendra à la fin",
            _ => "Pas tout à fait…",
        };
        FeedbackColor = result.IsCorrect ? Palette.Success : Palette.Error;
        Explanation = result.Explanation;
        ContinueText = !_session.HasNext ? "Voir les résultats"
            : !wasRetry && _session.CurrentIndex == _session.Questions.Count - 1 ? "Revoir mes erreurs"
            : "Continuer";
        UpdateProgress();
        IsAnswered = true;

        AnswerChecked?.Invoke(this, result.IsCorrect);
        _ = feedback.PlayAsync(result.IsCorrect ? FeedbackKind.Correct : FeedbackKind.Wrong, _settings);
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (_session is null || !IsAnswered)
            return;

        if (_session.MoveNext())
        {
            ShowCurrentQuestion();
            return;
        }

        var update = await progressService.ApplyResultAsync(_session.BuildResult());

        // On remplace le quizz (et la leçon) par l'écran de résultat : « retour » ramène à la catégorie.
        var back = _startedFromLesson ? "../.." : "..";
        await Shell.Current.GoToAsync($"{back}/{nameof(ResultPage)}", new Dictionary<string, object> { ["update"] = update });
    }
}
