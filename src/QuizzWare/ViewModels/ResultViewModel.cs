using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Progress;

namespace QuizzWare.ViewModels;

public partial class ResultViewModel : ObservableObject, IQueryAttributable
{
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("update", out var value) && value is ProgressUpdate update)
            Populate(update);

        // Évite de ré-appliquer l'objet si Shell rappelle cette méthode lors d'une navigation retour.
        query.Clear();
    }

    private void Populate(ProgressUpdate update)
    {
        var result = update.Result;

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

    [RelayCommand]
    private Task ContinueAsync() => Shell.Current.GoToAsync("..");
}
