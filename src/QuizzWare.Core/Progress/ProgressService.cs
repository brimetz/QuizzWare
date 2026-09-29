using QuizzWare.Core.Content;
using QuizzWare.Core.Leveling;
using QuizzWare.Core.Models;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Progress;

public sealed record ProgressUpdate(
    QuizResult Result,
    bool IsChallenge,
    LevelInfo Before,
    LevelInfo After,
    bool UnitPassed,
    bool UnitNewlyCompleted,
    int Streak)
{
    public bool LeveledUp => After.Level > Before.Level;
}

/// <summary><see cref="Planned"/> : contenu pas encore écrit, visible dans le parcours mais non jouable.</summary>
public enum UnitState { Locked, Available, Completed, Planned }

/// <summary>Applique les résultats de quizz à la progression : XP, niveaux, leçons et défis réussis, série de jours.</summary>
public sealed class ProgressService(IProgressStore store, ContentRepository content, TimeProvider time)
{
    /// <summary>Score minimum (en %) pour valider une leçon ou un défi.</summary>
    public const int PassPercent = 70;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private UserProgress? _progress;

    public async Task<UserProgress> GetProgressAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _progress ??= await store.LoadAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProgressUpdate> ApplyResultAsync(QuizResult result)
    {
        await _lock.WaitAsync();
        try
        {
            var progress = _progress ??= await store.LoadAsync();
            var category = progress.GetOrCreate(result.CategoryId);
            var before = LevelSystem.GetLevelInfo(category.TotalXp);

            category.TotalXp += result.XpEarned;
            category.QuizzesCompleted++;
            category.QuestionsAnswered += result.TotalQuestions;
            category.CorrectAnswers += result.CorrectAnswers;

            var isChallenge = false;
            var passed = false;
            var newlyCompleted = false;
            if (result.UnitId is { } unitId)
            {
                isChallenge = content.GetUnit(result.CategoryId, unitId) is Challenge;
                category.BestScores[unitId] = Math.Max(category.BestScores.GetValueOrDefault(unitId), result.ScorePercent);
                passed = result.ScorePercent >= PassPercent;
                newlyCompleted = passed && category.CompletedLessons.Add(unitId);
            }

            UpdateStreak(progress);
            await store.SaveAsync(progress);

            return new ProgressUpdate(result, isChallenge, before, LevelSystem.GetLevelInfo(category.TotalXp), passed, newlyCompleted, progress.CurrentStreak);
        }
        finally
        {
            _lock.Release();
        }
    }

    public LevelInfo GetLevel(UserProgress progress, string categoryId) =>
        LevelSystem.GetLevelInfo(progress.Categories.GetValueOrDefault(categoryId)?.TotalXp ?? 0);

    /// <summary>Série affichée : retombe à 0 si l'utilisateur a manqué un jour.</summary>
    public int GetCurrentStreak(UserProgress progress) =>
        progress.LastActivityDate is { } last && last >= Today().AddDays(-1) ? progress.CurrentStreak : 0;

    /// <summary>
    /// Les leçons se débloquent dans l'ordre de leur parcours (tronc commun ou spécialisation), chapitre après chapitre :
    /// il faut valider la leçon précédente. Les parcours de spécialisation attendent en plus l'ouverture de l'arbre.
    /// </summary>
    public UnitState GetLessonState(UserProgress progress, Category category, string lessonId)
    {
        var completed = Completed(progress, category);
        if (completed.Contains(lessonId))
            return UnitState.Completed;

        foreach (var path in category.Paths)
        {
            // Les leçons « à venir » sont sautées : elles ne bloquent pas la suite du parcours.
            string? previous = null;
            foreach (var lesson in path.SelectMany(c => c.Lessons))
            {
                if (lesson.Id == lessonId)
                {
                    if (lesson.IsPlanned)
                        return UnitState.Planned;

                    var isSpecialization = !ReferenceEquals(path, category.Chapters);
                    if (isSpecialization && !AreSpecializationsUnlocked(progress, category))
                        return UnitState.Locked;
                    return previous is null || completed.Contains(previous) ? UnitState.Available : UnitState.Locked;
                }

                if (!lesson.IsPlanned)
                    previous = lesson.Id;
            }
        }

        throw new KeyNotFoundException($"Leçon inconnue : {category.Id}/{lessonId}");
    }

    /// <summary>L'arbre des spécialisations s'ouvre quand le chapitre <see cref="SpecializationTree.UnlockedBy"/> est terminé.</summary>
    public bool AreSpecializationsUnlocked(UserProgress progress, Category category)
    {
        if (category.Specializations is not { } tree)
            return false;
        if (tree.UnlockedBy is null)
            return true;

        var completed = Completed(progress, category);
        return category.Chapters.First(c => c.Id == tree.UnlockedBy).Lessons
            .Where(l => !l.IsPlanned)
            .All(l => completed.Contains(l.Id));
    }

    /// <summary>Leçons jouables validées / leçons jouables au total, dans le parcours d'un nœud et de ses descendants.</summary>
    public (int Completed, int Total) GetNodeProgress(UserProgress progress, Category category, SpecializationNode node)
    {
        var completed = Completed(progress, category);
        var lessons = node.SelfAndDescendants.SelectMany(n => n.Track).SelectMany(c => c.Lessons).Where(l => !l.IsPlanned).ToList();
        return (lessons.Count(l => completed.Contains(l.Id)), lessons.Count);
    }

    /// <summary>Le défi d'un chapitre s'ouvre quand toutes ses leçons sont validées.</summary>
    public UnitState GetChallengeState(UserProgress progress, Category category, Chapter chapter)
    {
        if (chapter.Challenge is null)
            throw new ArgumentException($"Le chapitre {chapter.Id} n'a pas de défi.", nameof(chapter));

        if (chapter.Challenge.IsPlanned)
            return UnitState.Planned;

        var completed = Completed(progress, category);
        if (completed.Contains(chapter.Challenge.Id))
            return UnitState.Completed;
        return chapter.Lessons.Where(l => !l.IsPlanned).All(l => completed.Contains(l.Id)) ? UnitState.Available : UnitState.Locked;
    }

    /// <summary>Leçons déjà accessibles : l'entraînement ne pioche que dans ce que l'utilisateur a vu ou peut voir.</summary>
    public IReadOnlyList<Lesson> GetUnlockedLessons(UserProgress progress, Category category) =>
        category.AllLessons
            .Where(l => GetLessonState(progress, category, l.Id) is UnitState.Available or UnitState.Completed)
            .ToList();

    private static HashSet<string> Completed(UserProgress progress, Category category) =>
        progress.Categories.GetValueOrDefault(category.Id)?.CompletedLessons ?? [];

    private void UpdateStreak(UserProgress progress)
    {
        var today = Today();
        if (progress.LastActivityDate == today)
            return;

        progress.CurrentStreak = progress.LastActivityDate == today.AddDays(-1) ? progress.CurrentStreak + 1 : 1;
        progress.BestStreak = Math.Max(progress.BestStreak, progress.CurrentStreak);
        progress.LastActivityDate = today;
    }

    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
