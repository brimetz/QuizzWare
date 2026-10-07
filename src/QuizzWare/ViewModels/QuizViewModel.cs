using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

/// <summary>
/// Normal : leçon, défi ou entraînement. Review : révision espacée.
/// Placement et Interview sont des examens : pas de correction question par question.
/// </summary>
public enum QuizMode { Normal, Review, Placement, Interview }

public partial class QuizViewModel(
    ContentRepository content, ProgressService progressService, QuizBuilder builder, FeedbackService feedback, ContentReporter reporter)
    : ObservableObject, IQueryAttributable
{
    private string? _categoryId;
    private string? _unitId;
    private QuizMode _mode;
    private bool _startedFromLesson;
    private QuizSession? _session;
    private PlacementTest? _placement;
    private InterviewLevel? _interviewLevel;
    private IDispatcherTimer? _timer;
    private readonly Stopwatch _chrono = new();
    private bool _finishing;
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
    private string checkText = "Vérifier";

    [ObservableProperty]
    private bool hasTimer;

    [ObservableProperty]
    private string timerText = string.Empty;

    [ObservableProperty]
    private Color timerColor = Palette.TextMuted;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
    private QuestionViewModel? current;

    public bool IsNotAnswered => !IsAnswered;

    private bool IsExam => _mode is QuizMode.Placement or QuizMode.Interview;

    /// <summary>Contient uniquement la question courante : permet de choisir son template via BindableLayout.</summary>
    public ObservableCollection<QuestionViewModel> CurrentQuestion { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _categoryId = query.TryGetValue("categoryId", out var c) ? c as string : null;
        _unitId = query.TryGetValue("unitId", out var u) ? u as string : null;
        _mode = (query.TryGetValue("mode", out var m) ? m as string : null) switch
        {
            "review" => QuizMode.Review,
            "placement" => QuizMode.Placement,
            "interview" => QuizMode.Interview,
            _ => QuizMode.Normal,
        };
        if (query.TryGetValue("level", out var l) && l is string levelId)
            _interviewLevel = InterviewLevel.Get(levelId);
    }

    public async Task LoadAsync()
    {
        if (_session is not null)
        {
            ResumeTimer();
            return;
        }

        _settings = (await progressService.GetProgressAsync()).Settings;

        if (_mode == QuizMode.Review)
        {
            await LoadReviewAsync();
            return;
        }

        if (_categoryId is null)
            return;

        var category = content.GetCategory(_categoryId);

        if (_mode == QuizMode.Placement)
        {
            _placement = builder.ForPlacement(category);
            _session = _placement.Session;
            Title = $"🎯 Test de niveau";
            CheckText = "Valider";
            ShowCurrentQuestion();
            return;
        }

        if (_mode == QuizMode.Interview)
        {
            _interviewLevel ??= InterviewLevel.Junior;
            _session = builder.ForInterview(category, _interviewLevel);
            await progressService.RecordInterviewStartAsync(); // compte dans le quota gratuit, même s'il est abandonné
            Title = $"🎤 Entretien {_interviewLevel.Label}";
            CheckText = "Valider";
            HasTimer = true;
            _chrono.Start();
            StartTimer();
            ShowCurrentQuestion();
            return;
        }

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
        Prompt = question.Source.Prompt;

        switch (_mode)
        {
            case QuizMode.Placement:
                Kind = $"{_placement!.CurrentChapterNumber}. {_placement.CurrentChapter.Title}";
                ProgressText = $"Chapitre {_placement.CurrentChapterNumber} / {_placement.Chapters.Count}";
                Progress = (double)(_placement.CurrentChapterNumber - 1) / _placement.Chapters.Count;
                break;
            case QuizMode.Interview:
                Kind = Current.Kind;
                ProgressText = $"{_session.CurrentIndex + 1} / {_session.Questions.Count}";
                Progress = (double)_session.CurrentIndex / _session.Questions.Count;
                break;
            default:
                Kind = IsRetry ? "🔁 Erreur à revoir"
                    : _mode == QuizMode.Review && location is not null ? $"{location.Category.Name} · {Current.Kind}"
                    : Current.Kind;
                ProgressText = IsRetry
                    ? $"{_session.PendingRetries} à revoir"
                    : $"{_session.CurrentIndex + 1} / {_session.Questions.Count}";
                UpdateProgress();
                break;
        }

        IsAnswered = false;
    }

    /// <summary>La barre n'avance que pour les questions terminées : une erreur se rattrape avant la fin.</summary>
    private void UpdateProgress() => Progress = (double)_session!.SettledCount / _session.Questions.Count;

    private void OnCurrentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(QuestionViewModel.CanSubmit))
            CheckCommand.NotifyCanExecuteChanged();
    }

    private bool CanCheck() => Current?.CanSubmit == true && !IsAnswered && !_finishing;

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task CheckAsync()
    {
        if (_session is null || Current is null || IsAnswered || _finishing)
            return;

        if (IsExam)
        {
            await SubmitExamAnswerAsync();
            return;
        }

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

    /// <summary>Examen : on enregistre la réponse sans la corriger, puis on enchaîne.</summary>
    private async Task SubmitExamAnswerAsync()
    {
        _session!.Submit(Current!.BuildAnswer());

        var finished = _mode == QuizMode.Placement ? _placement!.IsFinished : !_session.HasNext;
        if (finished)
        {
            await FinishExamAsync(timeExpired: false);
            return;
        }

        _session.MoveNext();
        ShowCurrentQuestion();
    }

    [RelayCommand]
    private Task ReportAsync()
    {
        if (_session is null)
            return Task.CompletedTask;

        var question = _session.Current.Source;
        var location = content.FindQuestion(question.Id);
        return reporter.ReportAsync($"question {question.Id}",
        [
            $"- Question : `{question.Id}`",
            $"- Catégorie : {location?.Category.Name ?? "?"} · {location?.Unit.Title ?? "?"}",
            $"- Énoncé : {question.Prompt}",
        ]);
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

    private async Task FinishExamAsync(bool timeExpired)
    {
        if (_finishing)
            return;
        _finishing = true;
        CheckCommand.NotifyCanExecuteChanged();
        StopTimer();
        _chrono.Stop();

        Dictionary<string, object> parameters;
        if (_mode == QuizMode.Placement)
        {
            parameters = new() { ["placement"] = await progressService.ApplyPlacementAsync(_placement!) };
        }
        else
        {
            var session = _session!;
            var unanswered = Enumerable.Range(0, session.Questions.Count)
                .Where(i => session.IsCorrectAt(i) is null)
                .Select(i => session.Questions[i].Source.Id)
                .ToHashSet();
            if (timeExpired)
                session.Expire();
            var result = session.BuildResult();
            var update = await progressService.ApplyResultAsync(result);
            var report = InterviewReport.Evaluate(result, _interviewLevel!, _chrono.Elapsed, timeExpired, content, unanswered);
            parameters = new() { ["update"] = update, ["interview"] = report };
        }

        await Shell.Current.GoToAsync($"../{nameof(ResultPage)}", parameters);
    }

    // ---- Chrono du mode entretien ----------------------------------------------------------

    private void StartTimer()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        OnTick();
    }

    private void OnTick()
    {
        var remaining = _interviewLevel!.Duration - _chrono.Elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            TimerText = "⏱ 00:00";
            _ = TimeUpAsync();
            return;
        }

        TimerText = $"⏱ {remaining:mm\\:ss}";
        TimerColor = remaining < TimeSpan.FromMinutes(1) ? Palette.Error : Palette.TextMuted;
    }

    private async Task TimeUpAsync()
    {
        if (_finishing)
            return;
        StopTimer();
        await Shell.Current.DisplayAlert("⏱ Temps écoulé", "Les questions sans réponse comptent comme fausses.", "Voir le bilan");
        await FinishExamAsync(timeExpired: true);
    }

    private void ResumeTimer()
    {
        if (_mode == QuizMode.Interview && !_finishing && _timer is { IsRunning: false })
            _timer.Start();
    }

    /// <summary>La page quitte l'écran (retour, autre page) : le chrono visuel s'arrête, le temps continue de compter.</summary>
    public void Pause() => _timer?.Stop();

    private void StopTimer()
    {
        _timer?.Stop();
        _timer = null;
    }
}
