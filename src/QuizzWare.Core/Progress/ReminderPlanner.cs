using QuizzWare.Core.Models;

namespace QuizzWare.Core.Progress;

public sealed record PlannedReminder(int Id, DateTime When, string Title, string Message);

/// <summary>
/// Décide quels rappels programmer, sans dépendre du système de notifications :
/// un rappel par jour à l'heure choisie, sur quelques jours d'avance (au cas où l'app ne serait pas rouverte),
/// et aucun rappel aujourd'hui si l'objectif du jour est déjà atteint.
/// </summary>
public static class ReminderPlanner
{
    public const int FirstId = 1000;
    public const int DaysAhead = 3;

    /// <summary>Identifiants de tous les rappels que QuizzWare peut programmer (pour les annuler avant de reprogrammer).</summary>
    public static IEnumerable<int> AllIds => Enumerable.Range(FirstId, DaysAhead);

    /// <param name="now">Heure locale actuelle.</param>
    public static IReadOnlyList<PlannedReminder> Plan(UserSettings settings, DailyGoalStatus goal, int streak, int dueReviews, DateTime now)
    {
        if (!settings.RemindersEnabled)
            return [];

        var today = DateOnly.FromDateTime(now);
        var reminders = new List<PlannedReminder>();
        for (var d = 0; d < DaysAhead; d++)
        {
            var when = today.AddDays(d).ToDateTime(settings.ReminderTime);
            if (when <= now || (d == 0 && goal.IsReached))
                continue;

            var (title, message) = d == 0 ? Today(goal, streak, dueReviews) : Later();
            reminders.Add(new PlannedReminder(FirstId + d, when, title, message));
        }

        return reminders;
    }

    private static (string Title, string Message) Today(DailyGoalStatus goal, int streak, int dueReviews)
    {
        var title = streak > 0
            ? $"🔥 Ta série de {streak} jour{(streak > 1 ? "s" : "")} est en jeu"
            : "📚 C'est l'heure de ta session QuizzWare";

        var remaining = Math.Max(0, goal.GoalXp - goal.XpToday);
        var message = dueReviews > 0
            ? $"{dueReviews} question{(dueReviews > 1 ? "s" : "")} à réviser et encore {remaining} XP pour ton objectif du jour."
            : $"Encore {remaining} XP pour atteindre ton objectif du jour.";
        return (title, message);
    }

    private static (string Title, string Message) Later() =>
        ("📚 Une leçon de 5 minutes ?", "Tes révisions t'attendent : quelques questions suffisent à entretenir tes acquis.");
}
