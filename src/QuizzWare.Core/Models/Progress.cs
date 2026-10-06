namespace QuizzWare.Core.Models;

/// <summary>
/// Progression et préférences de l'utilisateur. Aujourd'hui sauvegardée localement ;
/// conçue pour devenir le profil synchronisé d'un compte (voir <see cref="SchemaVersion"/> et <see cref="UpdatedAt"/>).
/// </summary>
public sealed class UserProgress
{
    /// <summary>
    /// Version 1 : catégories et série de jours. Version 2 : révisions espacées, objectif quotidien, réglages.
    /// Les ajouts sont rétrocompatibles : une ancienne sauvegarde se charge avec des valeurs par défaut.
    /// </summary>
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Dernière modification : servira à fusionner les progressions de plusieurs appareils.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    public Dictionary<string, CategoryProgress> Categories { get; set; } = [];
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateOnly? LastActivityDate { get; set; }

    public UserSettings Settings { get; set; } = new();

    /// <summary>XP gagnée par jour (clé « aaaa-MM-jj »), limitée aux derniers jours.</summary>
    public Dictionary<string, int> DailyXp { get; set; } = [];

    /// <summary>Fiches de révision espacée, par identifiant de question.</summary>
    public Dictionary<string, ReviewCard> Reviews { get; set; } = [];

    public CategoryProgress GetOrCreate(string categoryId)
    {
        if (!Categories.TryGetValue(categoryId, out var progress))
        {
            progress = new CategoryProgress();
            Categories[categoryId] = progress;
        }
        return progress;
    }
}

public sealed class CategoryProgress
{
    public int TotalXp { get; set; }
    public int QuizzesCompleted { get; set; }
    public int QuestionsAnswered { get; set; }
    public int CorrectAnswers { get; set; }
    /// <summary>Leçons et défis validés (le nom est conservé pour rester compatible avec les sauvegardes existantes).</summary>
    public HashSet<string> CompletedLessons { get; set; } = [];

    /// <summary>Meilleur score (en %) obtenu par leçon ou défi.</summary>
    public Dictionary<string, int> BestScores { get; set; } = [];
}

public sealed class UserSettings
{
    public const int DefaultDailyGoalXp = 30;

    public int DailyGoalXp { get; set; } = DefaultDailyGoalXp;
    public bool RemindersEnabled { get; set; }
    public TimeOnly ReminderTime { get; set; } = new(19, 0);
    public bool SoundEnabled { get; set; } = true;
    public bool HapticsEnabled { get; set; } = true;
}

/// <summary>Fiche de révision (système de Leitner) : plus la boîte est haute, plus la révision suivante est lointaine.</summary>
public sealed class ReviewCard
{
    public int Box { get; set; } = 1;
    public DateOnly Due { get; set; }
    public DateOnly LastSeen { get; set; }
    public int Lapses { get; set; }
}
