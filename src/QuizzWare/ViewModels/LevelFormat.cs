using QuizzWare.Core.Leveling;

namespace QuizzWare.ViewModels;

public static class LevelFormat
{
    public static string Level(LevelInfo info) => $"Niveau {info.Level} · {info.Title}";

    public static string Xp(LevelInfo info) =>
        info.IsMaxLevel ? "Niveau maximum atteint 🏆" : $"{info.XpIntoLevel} / {info.XpForNextLevel} XP";
}
