using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;
using QuizzWare.Services;

namespace QuizzWare.ViewModels;

public partial class ResultViewModel(ProgressService progressService, FeedbackService feedback) : ObservableObject, IQueryAttributable
{
    /// <summary>Leçon validée, sans faute ou objectif du jour atteint : la page lance les confettis.</summary>
    public bool ShouldCelebrate { get; private set; }

    /// <summary>Le son de fin, joué à l'apparition de la page.</summary>
    public async Task PlayEndSoundAsync()
    {
        var settings = (await progressService.GetProgressAsync()).Settings;
        await feedback.PlayAsync(ShouldCelebrate ? FeedbackKind.Complete : FeedbackKind.Correct, settings);
    }

    [ObservableProperty]
    private string headline = string.Empty;

    [ObservableProperty]
    private string scoreText = string.Empty;

    [ObservableProperty]
    private string xpText = string.Empty;

    [ObservableProperty]
    private bool isLevelUp;

    [ObservableProperty]
    private string levelUpText = string.Empty;

    [ObservableProperty]
    private string levelText = string.Empty;

    [ObservableProperty]
    private string levelXpText = string.Empty;

    [ObservableProperty]
    private double levelProgress;

    [ObservableProperty]
    private string streakText = string.Empty;

    [ObservableProperty]
    private string? lessonText;

    [ObservableProperty]
    private Color lessonColor = Palette.Success;

    [ObservableProperty]
    private bool hasLevel;

    [ObservableProperty]
    private string dailyGoalText = string.Empty;

    [ObservableProperty]
    private double dailyGoalProgress;

    [ObservableProperty]
    private Color dailyGoalColor = Palette.Warning;

    [ObservableProperty]
    private bool hasReport;

    [ObservableProperty]
    private string reportTitle = string.Empty;

    [ObservableProperty]
    private string reportDetails = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> weakTopics = [];

    [ObservableProperty]
    private bool hasWeakTopics;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("update", out var value) && value is ProgressUpdate update)
            Populate(update);
        if (query.TryGetValue("interview", out var r) && r is InterviewReport report)
            PopulateInterview(report);
        if (query.TryGetValue("placement", out var p) && p is PlacementUpdate placement)
            PopulatePlacement(placement);

        // Évite de ré-appliquer l'objet si Shell rappelle cette méthode lors d'une navigation retour.
        query.Clear();
    }

    private void Populate(ProgressUpdate update)
    {
        var result = update.Result;
        ShouldCelebrate = result.IsPerfect || update.UnitNewlyCompleted || update.LeveledUp || update.DailyGoalJustReached;

        Headline = result.IsPerfect ? "Parfait ! 🎯"
            : result.ScorePercent >= ProgressService.PassPercent ? "Bien joué ! 💪"
            : "Continue, tu progresses 📚";
        ScoreText = $"{result.CorrectAnswers} / {result.TotalQuestions}";
        XpText = result.IsPerfect && !result.IsReview ? $"+{result.XpEarned} XP (bonus sans faute inclus)" : $"+{result.XpEarned} XP";

        // Une révision touche plusieurs catégories : pas de carte de niveau.
        HasLevel = update.After is not null;
        IsLevelUp = update.LeveledUp;
        if (update.After is { } after)
        {
            LevelUpText = $"⬆️ Niveau {after.Level} atteint : {after.Title} !";
            LevelText = LevelFormat.Level(after);
            LevelXpText = LevelFormat.Xp(after);
            LevelProgress = after.Progress;
        }

        StreakText = update.Streak > 1 ? $"🔥 Série de {update.Streak} jours" : "🔥 Série démarrée : reviens demain !";

        var goal = update.DailyGoal;
        DailyGoalProgress = goal.Progress;
        DailyGoalColor = goal.IsReached ? Palette.Success : Palette.Warning;
        DailyGoalText = update.DailyGoalJustReached ? $"🎉 Objectif du jour atteint : {goal.XpToday} / {goal.GoalXp} XP !"
            : goal.IsReached ? $"✅ Objectif du jour : {goal.XpToday} / {goal.GoalXp} XP"
            : $"🎯 Objectif du jour : {goal.XpToday} / {goal.GoalXp} XP";

        var unit = update.IsChallenge ? "le défi" : "la leçon";
        if (result.IsReview)
        {
            var failed = result.TotalQuestions - result.CorrectAnswers;
            LessonText = failed == 0
                ? "🧠 Révisions réussies : ces questions reviendront plus tard, de plus en plus espacées."
                : $"🧠 {failed} question{(failed > 1 ? "s" : "")} à revoir demain.";
            LessonColor = failed == 0 ? Palette.Success : Palette.Warning;
        }
        else if (result.UnitId is null)
        {
            LessonText = null;
        }
        else if (update.UnitPassed)
        {
            LessonText = (update.IsChallenge, update.UnitNewlyCompleted) switch
            {
                (true, true) => "🏆 Défi entretien réussi !",
                (false, true) => "✅ Leçon validée ! La suite est débloquée.",
                _ => $"✅ {char.ToUpper(unit[0])}{unit[1..]} déjà validé{(update.IsChallenge ? "" : "e")}",
            };
            LessonColor = Palette.Success;
        }
        else
        {
            LessonText = $"Il faut {ProgressService.PassPercent} % de bonnes réponses pour valider {unit}.";
            LessonColor = Palette.Warning;
        }
    }

    /// <summary>Simulation d'entretien : verdict, temps et sujets à retravailler, en plus du résultat habituel.</summary>
    private void PopulateInterview(InterviewReport report)
    {
        Headline = report.Percent >= InterviewReport.PassPercent ? "Entretien réussi ! 🎉"
            : report.Percent >= InterviewReport.AlmostPercent ? "Presque ! 💪"
            : "Pas encore… 📚";
        ShouldCelebrate = report.Percent >= InterviewReport.PassPercent;
        HasReport = true;
        ReportTitle = $"🎤 Entretien {report.Level.Label} · {report.Percent} %";
        ReportDetails = report.Verdict + "\n" + (report.TimeExpired
            ? $"⏱ Temps écoulé ({report.Level.Duration.TotalMinutes:0} min) : les questions sans réponse comptent comme fausses."
            : $"⏱ Terminé en {report.Elapsed:m\\:ss} sur {report.Level.Duration.TotalMinutes:0} min.");
        WeakTopics = report.WeakTopics.Select(t => $"• {t.Title} : {t.Correct}/{t.Total}").ToList();
        HasWeakTopics = WeakTopics.Count > 0;

        var failed = report.Total - report.Correct;
        LessonText = failed == 0 ? null : $"🧠 {failed} question{(failed > 1 ? "s" : "")} ajoutée{(failed > 1 ? "s" : "")} à tes révisions.";
        LessonColor = Palette.Warning;
    }

    /// <summary>Test de niveau : chapitres acquis, leçons validées et prochaine étape.</summary>
    private void PopulatePlacement(PlacementUpdate update)
    {
        Headline = update.PassedChapters == update.TotalChapters ? "Tout le tronc commun est acquis ! 🏆"
            : update.PassedChapters > 0 ? "Bien placé ! 🎯"
            : "On commence par les bases 📚";
        ScoreText = $"{update.PassedChapters} / {update.TotalChapters}";
        XpText = $"chapitre{(update.TotalChapters > 1 ? "s" : "")} acquis · +{update.XpEarned} XP";
        ShouldCelebrate = update.PassedChapters > 0;

        HasLevel = true;
        IsLevelUp = false;
        LevelText = LevelFormat.Level(update.After);
        LevelXpText = LevelFormat.Xp(update.After);
        LevelProgress = update.After.Progress;

        HasReport = true;
        ReportTitle = "🎯 Test de niveau";
        ReportDetails = update.LessonsValidated > 0
            ? $"{update.LessonsValidated} leçon{(update.LessonsValidated > 1 ? "s" : "")} validée{(update.LessonsValidated > 1 ? "s" : "")} d'un coup. Les défis entretien de ces chapitres restent à relever."
            : "Aucun chapitre validé : le parcours commence au début, c'est le bon endroit pour toi.";
        LessonText = update.NextLesson is { } next ? $"➡️ Prochaine leçon : {next.Title}" : "Il te reste les défis entretien et les spécialisations.";
        LessonColor = Palette.Success;

        StreakText = update.Streak > 1 ? $"🔥 Série de {update.Streak} jours" : "🔥 Série démarrée : reviens demain !";
        var goal = update.DailyGoal;
        DailyGoalProgress = goal.Progress;
        DailyGoalColor = goal.IsReached ? Palette.Success : Palette.Warning;
        DailyGoalText = update.DailyGoalJustReached ? $"🎉 Objectif du jour atteint : {goal.XpToday} / {goal.GoalXp} XP !"
            : $"🎯 Objectif du jour : {goal.XpToday} / {goal.GoalXp} XP";
    }

    [RelayCommand]
    private Task ContinueAsync() => Shell.Current.GoToAsync("..");
}
