using QuizzWare.Core.Content;
using QuizzWare.Core.Leveling;
using QuizzWare.Core.Models;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Progress;

/// <param name="Before">Niveau de la catégorie avant le quizz ; null pour une révision (plusieurs catégories).</param>
public sealed record ProgressUpdate(
    QuizResult Result,
    bool IsChallenge,
    LevelInfo? Before,
    LevelInfo? After,
    bool UnitPassed,
    bool UnitNewlyCompleted,
    int Streak,
    DailyGoalStatus DailyGoal,
    bool DailyGoalJustReached)
{
    public bool LeveledUp => After is not null && Before is not null && After.Level > Before.Level;
}

public sealed record DailyGoalStatus(int XpToday, int GoalXp)
{
    public bool IsReached => XpToday >= GoalXp;
    public double Progress => GoalXp <= 0 ? 1 : Math.Min(1, (double)XpToday / GoalXp);
}

/// <summary><see cref="Planned"/> : contenu pas encore écrit, visible dans le parcours mais non jouable.</summary>
public enum UnitState { Locked, Available, Completed, Planned }

/// <summary>
/// Applique les résultats de quizz à la progression : XP, niveaux, leçons et défis réussis, série de jours,
/// objectif quotidien et révision espacée.
/// </summary>
public sealed class ProgressService(IProgressStore store, ContentRepository content, TimeProvider time)
{
    /// <summary>Score minimum (en %) pour valider une leçon ou un défi.</summary>
    public const int PassPercent = 70;

    /// <summary>Nombre maximal de questions dans une session de révision.</summary>
    public const int ReviewSessionSize = 10;

    /// <summary>Nombre de jours d'historique d'XP conservés.</summary>
    public const int DailyHistoryDays = 60;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private UserProgress? _progress;

    /// <summary>Levé après chaque sauvegarde (quizz, réglages…) : l'app s'en sert pour reprogrammer les rappels.</summary>
    public event EventHandler<UserProgress>? Changed;

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
        ProgressUpdate update;
        UserProgress progress;

        await _lock.WaitAsync();
        try
        {
            progress = _progress ??= await store.LoadAsync();
            var today = Today();
            var goalBefore = GetDailyGoal(progress);

            LevelInfo? before = null, after = null;
            var isChallenge = false;
            var passed = false;
            var newlyCompleted = false;

            if (result.CategoryId is { } categoryId)
            {
                var category = progress.GetOrCreate(categoryId);
                before = LevelSystem.GetLevelInfo(category.TotalXp);

                category.TotalXp += result.XpEarned;
                category.QuizzesCompleted++;
                category.QuestionsAnswered += result.TotalQuestions;
                category.CorrectAnswers += result.CorrectAnswers;

                if (result.UnitId is { } unitId)
                {
                    isChallenge = content.GetUnit(categoryId, unitId) is Challenge;
                    category.BestScores[unitId] = Math.Max(category.BestScores.GetValueOrDefault(unitId), result.ScorePercent);
                    passed = result.ScorePercent >= PassPercent;
                    newlyCompleted = passed && category.CompletedLessons.Add(unitId);
                }

                after = LevelSystem.GetLevelInfo(category.TotalXp);
            }
            else
            {
                // Révision : chaque question rapporte son XP à sa propre catégorie.
                foreach (var outcome in result.Outcomes ?? [])
                {
                    if (content.FindQuestion(outcome.QuestionId) is not { } location)
                        continue;
                    var category = progress.GetOrCreate(location.Category.Id);
                    category.TotalXp += outcome.XpEarned;
                    category.QuestionsAnswered++;
                    if (outcome.IsCorrect)
                        category.CorrectAnswers++;
                }
            }

            foreach (var outcome in result.Outcomes ?? [])
                progress.Reviews[outcome.QuestionId] =
                    SpacedRepetition.Update(progress.Reviews.GetValueOrDefault(outcome.QuestionId), outcome.IsCorrect, today);

            AddDailyXp(progress, today, result.XpEarned);
            UpdateStreak(progress, today);
            await SaveAsync(progress);

            var goalAfter = GetDailyGoal(progress);
            update = new ProgressUpdate(result, isChallenge, before, after, passed, newlyCompleted, progress.CurrentStreak,
                goalAfter, goalAfter.IsReached && !goalBefore.IsReached);
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke(this, progress);
        return update;
    }

    /// <summary>Modifie les réglages (objectif quotidien, rappels) et les sauvegarde.</summary>
    public async Task UpdateSettingsAsync(Action<UserSettings> change)
    {
        UserProgress progress;
        await _lock.WaitAsync();
        try
        {
            progress = _progress ??= await store.LoadAsync();
            change(progress.Settings);
            await SaveAsync(progress);
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke(this, progress);
    }

    /// <summary>Efface toute la progression. Les réglages sont conservés.</summary>
    public async Task ResetAsync()
    {
        UserProgress progress;
        await _lock.WaitAsync();
        try
        {
            var settings = (_progress ??= await store.LoadAsync()).Settings;
            progress = _progress = new UserProgress { Settings = settings };
            await SaveAsync(progress);
        }
        finally
        {
            _lock.Release();
        }

        Changed?.Invoke(this, progress);
    }

    public DailyGoalStatus GetDailyGoal(UserProgress progress) =>
        new(progress.DailyXp.GetValueOrDefault(DayKey(Today())), progress.Settings.DailyGoalXp);

    /// <summary>Questions à réviser aujourd'hui : les plus en retard d'abord, puis les moins bien maîtrisées.</summary>
    public IReadOnlyList<Question> GetDueReviews(UserProgress progress, int max = ReviewSessionSize)
    {
        var today = Today();
        return progress.Reviews
            .Where(r => r.Value.Due <= today)
            .OrderBy(r => r.Value.Due).ThenBy(r => r.Value.Box)
            .Select(r => content.FindQuestion(r.Key)?.Question)
            .OfType<Question>() // une question retirée du contenu est ignorée
            .Take(max)
            .ToList();
    }

    public int CountDueReviews(UserProgress progress)
    {
        var today = Today();
        return progress.Reviews.Count(r => r.Value.Due <= today && content.FindQuestion(r.Key) is not null);
    }

    public int CountMasteredQuestions(UserProgress progress) =>
        progress.Reviews.Values.Count(SpacedRepetition.IsMastered);

    private async Task SaveAsync(UserProgress progress)
    {
        progress.SchemaVersion = UserProgress.CurrentSchemaVersion;
        progress.UpdatedAt = time.GetUtcNow();
        await store.SaveAsync(progress);
    }

    private static void AddDailyXp(UserProgress progress, DateOnly today, int xp)
    {
        var key = DayKey(today);
        progress.DailyXp[key] = progress.DailyXp.GetValueOrDefault(key) + xp;

        var oldest = DayKey(today.AddDays(-DailyHistoryDays));
        foreach (var old in progress.DailyXp.Keys.Where(k => string.CompareOrdinal(k, oldest) < 0).ToList())
            progress.DailyXp.Remove(old);
    }

    private static string DayKey(DateOnly day) => day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

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

    private static void UpdateStreak(UserProgress progress, DateOnly today)
    {
        if (progress.LastActivityDate == today)
            return;

        progress.CurrentStreak = progress.LastActivityDate == today.AddDays(-1) ? progress.CurrentStreak + 1 : 1;
        progress.BestStreak = Math.Max(progress.BestStreak, progress.CurrentStreak);
        progress.LastActivityDate = today;
    }

    public DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
