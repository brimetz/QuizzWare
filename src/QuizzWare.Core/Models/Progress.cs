namespace QuizzWare.Core.Models;

/// <summary>Progression globale de l'utilisateur, persistée localement.</summary>
public sealed class UserProgress
{
    public Dictionary<string, CategoryProgress> Categories { get; set; } = [];
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateOnly? LastActivityDate { get; set; }

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
