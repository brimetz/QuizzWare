using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Premium;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;
using QuizzWare.Services;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

public partial class CategoryViewModel(ContentRepository content, ProgressService progressService, ISubscriptionService subscription)
    : ObservableObject, IQueryAttributable
{
    private string? _categoryId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string icon = string.Empty;

    [ObservableProperty]
    private Color color = Palette.Stroke;

    [ObservableProperty]
    private string levelText = string.Empty;

    [ObservableProperty]
    private string xpText = string.Empty;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private string statsText = string.Empty;

    [ObservableProperty]
    private string practiceText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChapterViewModel> chapters = [];

    [ObservableProperty]
    private bool canPractice;

    [ObservableProperty]
    private bool hasSpecializations;

    [ObservableProperty]
    private string specializationsText = string.Empty;

    [ObservableProperty]
    private bool canTakePlacement;

    [ObservableProperty]
    private string placementText = string.Empty;

    [ObservableProperty]
    private bool canInterview;

    [ObservableProperty]
    private string interviewText = "🎤 Mode entretien";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("categoryId", out var id))
            _categoryId = id as string;
    }

    [RelayCommand]
    private async Task StartPlacementAsync()
    {
        if (_categoryId is null)
            return;
        var category = content.GetCategory(_categoryId);
        var chapters = category.Chapters.Count(c => c.Lessons.Any(l => !l.IsPlanned));
        var ok = await Shell.Current.DisplayAlertAsync("🎯 Test de niveau",
            $"Jusqu'à {chapters * PlacementTest.QuestionsPerChapter} questions, {PlacementTest.QuestionsPerChapter} par chapitre, sans correction en cours de route. "
            + "Chaque chapitre réussi sans faute est validé, et le test s'arrête au premier chapitre raté.",
            "C'est parti", "Plus tard");
        if (ok)
            await Shell.Current.GoToAsync($"{nameof(QuizPage)}?categoryId={_categoryId}&mode=placement");
    }

    [RelayCommand]
    private async Task StartInterviewAsync()
    {
        if (_categoryId is null)
            return;

        var progress = await progressService.GetProgressAsync();
        var now = progressService.Now();
        if (!PremiumPolicy.CanStartInterview(progress, now, subscription.IsPremium))
        {
            var next = PremiumPolicy.NextFreeInterviewAt(progress, now);
            var when = next is { } n ? $" Le prochain entretien gratuit sera disponible le {n.ToLocalTime():dddd d MMMM à HH:mm}." : string.Empty;
            var discover = await Shell.Current.DisplayAlertAsync("🎤 Entretiens gratuits épuisés",
                $"La version gratuite comprend {PremiumPolicy.FreeInterviewsPerWeek} entretiens par semaine.{when} "
                + "Avec Premium, les entretiens sont illimités.", "Découvrir Premium", "Plus tard");
            if (discover)
                await Shell.Current.GoToAsync($"{nameof(PremiumPage)}?reason=interview");
            return;
        }

        var category = content.GetCategory(_categoryId);
        var levels = InterviewLevel.All.Where(l => QuizBuilder.CanInterview(category, l)).ToList();
        var choice = await Shell.Current.DisplayActionSheetAsync("🎤 Niveau de l'entretien", "Annuler", null,
            levels.Select(l => l.Description).ToArray());
        var level = levels.FirstOrDefault(l => l.Description == choice);
        if (level is null)
            return;

        var ok = await Shell.Current.DisplayAlertAsync($"Entretien {level.Label}",
            $"{level.QuestionCount} questions en {level.Duration.TotalMinutes:0} minutes, sans correction avant la fin, comme en vrai. "
            + "Tu obtiendras un verdict et les sujets à retravailler.", "Commencer", "Annuler");
        if (ok)
            await Shell.Current.GoToAsync($"{nameof(QuizPage)}?categoryId={_categoryId}&mode=interview&level={level.Id}");
    }

    public async Task LoadAsync()
    {
        if (_categoryId is null)
            return;

        var category = content.GetCategory(_categoryId);
        var userProgress = await progressService.GetProgressAsync();
        var level = progressService.GetLevel(userProgress, category.Id);
        var stats = userProgress.Categories.GetValueOrDefault(category.Id);

        Name = category.Name;
        Description = category.Description;
        Icon = category.Icon;
        Color = Color.FromArgb(category.Color);
        LevelText = LevelFormat.Level(level);
        XpText = LevelFormat.Xp(level);
        Progress = level.Progress;
        StatsText = stats is null || stats.QuestionsAnswered == 0
            ? "Aucun quizz pour l'instant"
            : $"{stats.QuizzesCompleted} quizz · {stats.CorrectAnswers * 100 / stats.QuestionsAnswered} % de bonnes réponses";
        PracticeText = $"⚡ Entraînement rapide ({QuizBuilder.PracticeQuestionCount} questions)";

        Chapters = PathBuilder.Build(category, category.Chapters, userProgress, progressService);
        CanPractice = progressService.GetUnlockedLessons(userProgress, category).Count > 0;
        CanTakePlacement = progressService.CanTakePlacement(userProgress, category);
        PlacementText = $"Tu connais déjà le {category.Name} ? Passe le test de niveau pour sauter les chapitres que tu maîtrises.";
        CanInterview = QuizBuilder.CanInterview(category, InterviewLevel.Junior);
        var freeLeft = PremiumPolicy.FreeInterviewsLeft(userProgress, progressService.Now());
        InterviewText = subscription.IsPremium
            ? "🎤 Mode entretien"
            : $"🎤 Mode entretien · {freeLeft} gratuit{(freeLeft > 1 ? "s" : "")} cette semaine";

        HasSpecializations = category.Specializations is not null;
        if (category.Specializations is { } tree)
        {
            var leaves = tree.AllNodes.Where(n => n.ChildNodes.Count == 0 && !n.IsDoor).ToList();
            var doors = tree.AllNodes.Count(n => n.IsDoor);
            var available = leaves.Count(n => content.Resolve(category, n).Node.HasTrack);
            var summary = $"{available} parcours disponibles · {leaves.Count - available} à venir"
                + (doors > 0 ? $" · {doors} portes" : string.Empty);
            SpecializationsText = progressService.AreSpecializationsUnlocked(userProgress, category)
                ? summary
                : $"🔒 Termine le chapitre « {category.Chapters.First(c => c.Id == tree.UnlockedBy).Title} » pour débloquer";
        }
    }

    [RelayCommand]
    private Task PracticeAsync() =>
        _categoryId is null ? Task.CompletedTask : Shell.Current.GoToAsync($"{nameof(QuizPage)}?categoryId={_categoryId}");

    [RelayCommand]
    private Task OpenSpecializationsAsync() =>
        _categoryId is null ? Task.CompletedTask : Shell.Current.GoToAsync($"{nameof(SpecializationsPage)}?categoryId={_categoryId}");
}

public sealed record ChapterViewModel(string Number, string Title, string Summary, string ProgressText, IReadOnlyList<UnitItemViewModel> Items);

/// <summary>Une ligne du parcours : leçon ou défi entretien.</summary>
public sealed partial class UnitItemViewModel
{
    private readonly string _categoryId;
    private readonly string _unitId;
    private readonly bool _isChallenge;
    private readonly UnitState _state;

    private UnitItemViewModel(string categoryId, string unitId, bool isChallenge, UnitState state)
    {
        _categoryId = categoryId;
        _unitId = unitId;
        _isChallenge = isChallenge;
        _state = state;
    }

    public required string Badge { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required string StatusText { get; init; }
    public string DifficultyText { get; init; } = string.Empty;

    public double Opacity => _state is UnitState.Locked or UnitState.Planned ? 0.45 : 1;
    public Color BadgeColor => _isChallenge ? Palette.Warning : Palette.SurfaceRaised;
    public Color BadgeTextColor => _isChallenge ? Palette.Surface : Palette.TextPrimary;
    public Color StrokeColor => _isChallenge && _state is UnitState.Available or UnitState.Completed ? Palette.Warning : Palette.Stroke;

    public Color StatusColor => _state switch
    {
        UnitState.Completed => Palette.Success,
        UnitState.Available => Palette.Warning,
        _ => Palette.TextMuted,
    };

    public static UnitItemViewModel ForLesson(string categoryId, Lesson lesson, string number, UnitState state, int? best) =>
        new(categoryId, lesson.Id, isChallenge: false, state)
        {
            Badge = number,
            Title = lesson.Title,
            Summary = lesson.Summary,
            DifficultyText = new string('●', lesson.Difficulty) + new string('○', 3 - lesson.Difficulty),
            StatusText = Status(state, best, "🔒 Valide la leçon précédente pour débloquer"),
        };

    public static UnitItemViewModel ForChallenge(string categoryId, Challenge challenge, UnitState state, int? best) =>
        new(categoryId, challenge.Id, isChallenge: true, state)
        {
            Badge = "🏆",
            Title = challenge.Title,
            Summary = challenge.Summary,
            StatusText = Status(state, best, "🔒 Termine les leçons du chapitre pour débloquer"),
        };

    private static string Status(UnitState state, int? best, string lockedText) => state switch
    {
        UnitState.Planned => "🕓 À venir",
        UnitState.Locked => lockedText,
        UnitState.Completed when best is null => "✅ Validée par le test de niveau",
        UnitState.Completed => $"✅ Réussi · meilleur score {best} %",
        _ when best is { } s => $"Meilleur score {s} % · {ProgressService.PassPercent} % requis",
        _ => "À découvrir",
    };

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (_state == UnitState.Planned)
        {
            await Shell.Current.DisplayAlertAsync(Title, "Ce contenu est prévu dans le parcours, mais pas encore écrit. Il arrive bientôt !", "OK");
            return;
        }

        if (_state == UnitState.Locked)
        {
            var message = _isChallenge
                ? "Valide toutes les leçons du chapitre pour accéder au défi."
                : $"Obtiens au moins {ProgressService.PassPercent} % au quizz de la leçon précédente pour débloquer celle-ci.";
            await Shell.Current.DisplayAlertAsync("Pas encore disponible", message, "OK");
            return;
        }

        var route = _isChallenge ? nameof(QuizPage) : nameof(LessonPage);
        await Shell.Current.GoToAsync($"{route}?categoryId={_categoryId}&unitId={_unitId}");
    }
}
