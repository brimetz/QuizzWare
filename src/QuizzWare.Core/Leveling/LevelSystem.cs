namespace QuizzWare.Core.Leveling;

public sealed record LevelInfo(int Level, int XpIntoLevel, int XpForNextLevel, string Title, bool IsMaxLevel)
{
    public double Progress => IsMaxLevel ? 1 : (double)XpIntoLevel / XpForNextLevel;
}

/// <summary>
/// Règles de progression : chaque catégorie a son propre niveau (1 à 10), calculé à partir de l'XP cumulée.
/// Passer du niveau N au niveau N+1 coûte 100 + 50 × (N − 1) XP.
/// </summary>
public static class LevelSystem
{
    public const int MaxLevel = 10;
    public const int XpPerDifficultyPoint = 10;
    public const int PerfectQuizBonus = 20;

    public static int XpToReachNext(int level) => 100 + (level - 1) * 50;

    public static int TotalXpForLevel(int level)
    {
        var total = 0;
        for (var k = 1; k < level; k++)
            total += XpToReachNext(k);
        return total;
    }

    public static LevelInfo GetLevelInfo(int totalXp)
    {
        totalXp = Math.Max(0, totalXp);

        var level = 1;
        while (level < MaxLevel && totalXp >= TotalXpForLevel(level + 1))
            level++;

        if (level == MaxLevel)
            return new LevelInfo(level, 0, 0, GetTitle(level), IsMaxLevel: true);

        return new LevelInfo(level, totalXp - TotalXpForLevel(level), XpToReachNext(level), GetTitle(level), IsMaxLevel: false);
    }

    public static string GetTitle(int level) => level switch
    {
        <= 2 => "Débutant",
        <= 4 => "Junior",
        <= 6 => "Confirmé",
        <= 8 => "Senior",
        9 => "Expert",
        _ => "Maître",
    };

    /// <summary>Difficulté maximale des questions proposées en entraînement selon le niveau.</summary>
    public static int MaxQuestionDifficulty(int level) => level switch
    {
        <= 3 => 1,
        <= 6 => 2,
        _ => 3,
    };

    public static int XpForCorrectAnswer(int difficulty) => XpPerDifficultyPoint * difficulty;
}
