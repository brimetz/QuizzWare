using QuizzWare.Core.Models;

namespace QuizzWare.Core.Premium;

/// <summary>
/// Ce qui est gratuit et ce qui demande l'abonnement Premium.
/// Gratuit : les catégories de langages et les catégories de base, et quelques entretiens par semaine.
/// Premium : toutes les autres catégories (et les parcours qui en dépendent) et les entretiens illimités.
/// </summary>
public static class PremiumPolicy
{
    /// <summary>Identifiant de l'abonnement créé dans la Play Console (et App Store Connect).</summary>
    public const string SubscriptionProductId = "quizzware_premium";

    public const int FreeInterviewsPerWeek = 3;
    public static readonly TimeSpan InterviewWindow = TimeSpan.FromDays(7);

    /// <summary>Langages de programmation.</summary>
    public static readonly IReadOnlySet<string> LanguageCategories = new HashSet<string>
    {
        "c", "cpp", "csharp", "java", "javascript", "python", "go", "rust",
    };

    /// <summary>Fondamentaux que tout développeur doit maîtriser.</summary>
    public static readonly IReadOnlySet<string> BasicCategories = new HashSet<string>
    {
        "algorithmes", "bases-de-donnees", "git", "tests",
    };

    public static bool IsCategoryFree(string categoryId) =>
        LanguageCategories.Contains(categoryId) || BasicCategories.Contains(categoryId);

    public static bool CanAccessCategory(string categoryId, bool isPremium) =>
        isPremium || IsCategoryFree(categoryId);

    /// <summary>Entretiens gratuits restants sur les 7 derniers jours (fenêtre glissante).</summary>
    public static int FreeInterviewsLeft(UserProgress progress, DateTimeOffset now) =>
        Math.Max(0, FreeInterviewsPerWeek - RecentInterviews(progress, now).Count());

    public static bool CanStartInterview(UserProgress progress, DateTimeOffset now, bool isPremium) =>
        isPremium || FreeInterviewsLeft(progress, now) > 0;

    /// <summary>Date à laquelle un entretien gratuit redevient disponible, null s'il en reste déjà.</summary>
    public static DateTimeOffset? NextFreeInterviewAt(UserProgress progress, DateTimeOffset now)
    {
        var recent = RecentInterviews(progress, now).OrderBy(d => d).ToList();
        return recent.Count < FreeInterviewsPerWeek ? null : recent[recent.Count - FreeInterviewsPerWeek] + InterviewWindow;
    }

    private static IEnumerable<DateTimeOffset> RecentInterviews(UserProgress progress, DateTimeOffset now) =>
        progress.InterviewStarts.Where(d => d <= now && now - d < InterviewWindow);
}
