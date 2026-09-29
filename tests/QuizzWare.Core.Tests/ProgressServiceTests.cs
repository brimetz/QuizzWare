using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

public class ProgressServiceTests
{
    private readonly InMemoryProgressStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly QuizzWare.Core.Content.ContentRepository _content = Sample.Repository();
    private readonly ProgressService _service;

    public ProgressServiceTests()
    {
        _service = new ProgressService(_store, _content, _time);
    }

    private QuizzWare.Core.Models.Category Cat => _content.GetCategory("cat");

    private Task Pass(string unitId) => _service.ApplyResultAsync(new QuizResult("cat", unitId, 4, 4, 40));

    [Fact]
    public async Task ApplyResult_AddsXp_AndDetectsLevelUp()
    {
        var update = await _service.ApplyResultAsync(new QuizResult("cat", null, 5, 5, 120));

        Assert.Equal(1, update.Before.Level);
        Assert.Equal(2, update.After.Level);
        Assert.True(update.LeveledUp);
        Assert.Equal(120, _store.Stored.Categories["cat"].TotalXp);
        Assert.Equal(1, _store.SaveCount);
    }

    [Fact]
    public async Task Lessons_UnlockInOrder_AcrossChapters()
    {
        var progress = await _service.GetProgressAsync();
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "l1"));
        Assert.Equal(UnitState.Locked, _service.GetLessonState(progress, Cat, "l2"));
        Assert.Equal(UnitState.Locked, _service.GetLessonState(progress, Cat, "l3"));

        var update = await _service.ApplyResultAsync(new QuizResult("cat", "l1", 4, 3, 30));
        Assert.True(update.UnitPassed);
        Assert.True(update.UnitNewlyCompleted);
        Assert.False(update.IsChallenge);
        Assert.Equal(UnitState.Completed, _service.GetLessonState(progress, Cat, "l1"));
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "l2"));

        await Pass("l2");
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "l3")); // 1re leçon du chapitre suivant
    }

    [Fact]
    public async Task Challenge_OpensWhenAllChapterLessonsAreDone_AndIsOptional()
    {
        var progress = await _service.GetProgressAsync();
        var chapter = Cat.Chapters[0];

        await Pass("l1");
        Assert.Equal(UnitState.Locked, _service.GetChallengeState(progress, Cat, chapter));

        await Pass("l2");
        Assert.Equal(UnitState.Available, _service.GetChallengeState(progress, Cat, chapter));
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "l3"));

        var update = await _service.ApplyResultAsync(new QuizResult("cat", "d1", 2, 2, 70));
        Assert.True(update.IsChallenge);
        Assert.True(update.UnitNewlyCompleted);
        Assert.Equal(UnitState.Completed, _service.GetChallengeState(progress, Cat, chapter));
    }

    [Fact]
    public async Task UnlockedLessons_FeedPractice()
    {
        var progress = await _service.GetProgressAsync();
        Assert.Equal(["l1"], _service.GetUnlockedLessons(progress, Cat).Select(l => l.Id));

        await Pass("l1");
        Assert.Equal(["l1", "l2"], _service.GetUnlockedLessons(progress, Cat).Select(l => l.Id));
    }

    [Fact]
    public async Task ApplyResult_FailingScore_KeepsLessonOpen_ButRecordsBestScore()
    {
        var update = await _service.ApplyResultAsync(new QuizResult("cat", "l1", 4, 2, 20));

        Assert.False(update.UnitPassed);
        Assert.DoesNotContain("l1", _store.Stored.Categories["cat"].CompletedLessons);
        Assert.Equal(50, _store.Stored.Categories["cat"].BestScores["l1"]);
    }

    [Fact]
    public async Task BestScore_IsNotLoweredByWorseAttempt()
    {
        await Pass("l1");
        await _service.ApplyResultAsync(new QuizResult("cat", "l1", 4, 1, 10));

        Assert.Equal(100, _store.Stored.Categories["cat"].BestScores["l1"]);
    }

    [Fact]
    public async Task Streak_IncrementsOnConsecutiveDays_AndResetsAfterGap()
    {
        await _service.ApplyResultAsync(new QuizResult("cat", null, 1, 1, 10));
        await _service.ApplyResultAsync(new QuizResult("cat", null, 1, 1, 10)); // même jour
        Assert.Equal(1, _store.Stored.CurrentStreak);

        _time.AdvanceDays(1);
        var update = await _service.ApplyResultAsync(new QuizResult("cat", null, 1, 1, 10));
        Assert.Equal(2, update.Streak);

        _time.AdvanceDays(3);
        Assert.Equal(0, _service.GetCurrentStreak(_store.Stored)); // série perdue à l'affichage
        update = await _service.ApplyResultAsync(new QuizResult("cat", null, 1, 1, 10));
        Assert.Equal(1, update.Streak);
        Assert.Equal(2, _store.Stored.BestStreak);
    }

    [Fact]
    public async Task PlannedLessons_AreSkipped_AndNeverPlayable()
    {
        var planned = new QuizzWare.Core.Models.Lesson("p", "À venir", "", 1, null, null);
        var category = Sample.Category("cat",
            Sample.Chapter("ch", new QuizzWare.Core.Models.Challenge("d", "Défi", "", [Sample.Question("dq")]),
                Sample.Lesson("l1", Sample.Question("q1")), planned, Sample.Lesson("l2", Sample.Question("q2"))),
            Sample.Chapter("ch2", new QuizzWare.Core.Models.Challenge("d2", "Défi à venir", "", null), planned with { Id = "p2" }));
        var content = new QuizzWare.Core.Content.ContentRepository([category]);
        var service = new ProgressService(_store, content, _time);
        var progress = await service.GetProgressAsync();

        Assert.Equal(UnitState.Planned, service.GetLessonState(progress, category, "p"));

        await service.ApplyResultAsync(new QuizResult("cat", "l1", 1, 1, 10));
        Assert.Equal(UnitState.Available, service.GetLessonState(progress, category, "l2")); // p ne bloque pas
        Assert.DoesNotContain(service.GetUnlockedLessons(progress, category), l => l.IsPlanned);

        await service.ApplyResultAsync(new QuizResult("cat", "l2", 1, 1, 10));
        Assert.Equal(UnitState.Available, service.GetChallengeState(progress, category, category.Chapters[0]));
        Assert.Equal(UnitState.Planned, service.GetChallengeState(progress, category, category.Chapters[1]));
        Assert.True(category.HasContent);
    }

    [Fact]
    public async Task ApplyResult_UnknownUnit_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.ApplyResultAsync(new QuizResult("cat", "inconnue", 1, 1, 10)));
    }
}
