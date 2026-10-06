using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

/// <summary>Révision espacée, objectif quotidien et rappels : ce qui fait revenir l'utilisateur.</summary>
public class RetentionTests
{
    private static readonly DateOnly Today = new(2026, 9, 29);

    private readonly InMemoryProgressStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
    private readonly ContentRepository _content;
    private readonly ProgressService _service;

    public RetentionTests()
    {
        // Deux catégories, pour vérifier la répartition de l'XP d'une révision.
        _content = new ContentRepository([
            Sample.Category("cat", Sample.Chapter("ch", null, Sample.Lesson("l1", Sample.Question("q1"), Sample.Question("q2", 2)))),
            Sample.Category("autre", Sample.Chapter("ch-autre", null, Sample.Lesson("l-autre", Sample.Question("q3", 3)))) with { Order = 2 },
        ]);
        _service = new ProgressService(_store, _content, _time);
    }

    private static QuizResult Lesson(string category, string unit, params (string Id, bool Ok, int Xp)[] answers) =>
        new(category, unit, answers.Length, answers.Count(a => a.Ok), answers.Sum(a => a.Xp),
            answers.Select(a => new QuestionOutcome(a.Id, a.Ok, a.Xp)).ToList());

    // ---- Boîtes de Leitner ---------------------------------------------------------------

    [Fact]
    public void WrongAnswer_GoesBackToBoxOne_DueTomorrow()
    {
        var card = SpacedRepetition.Update(new ReviewCard { Box = 4, Due = Today }, isCorrect: false, Today);

        Assert.Equal(1, card.Box);
        Assert.Equal(Today.AddDays(1), card.Due);
        Assert.Equal(1, card.Lapses);
    }

    [Fact]
    public void FirstSuccess_StartsInBoxTwo()
    {
        var card = SpacedRepetition.Update(null, isCorrect: true, Today);

        Assert.Equal(SpacedRepetition.FirstSuccessBox, card.Box);
        Assert.Equal(Today.AddDays(3), card.Due);
    }

    [Fact]
    public void SuccessBeforeDueDate_DoesNotPromote()
    {
        var card = SpacedRepetition.Update(new ReviewCard { Box = 2, Due = Today.AddDays(2) }, isCorrect: true, Today);

        Assert.Equal(2, card.Box);
        Assert.Equal(Today.AddDays(2), card.Due);
    }

    [Fact]
    public void SuccessWhenDue_PromotesUntilMastered()
    {
        var card = new ReviewCard { Box = 1, Due = Today };
        var day = Today;
        for (var i = 0; i < 10; i++)
        {
            card = SpacedRepetition.Update(card, isCorrect: true, day);
            day = card.Due;
        }

        Assert.Equal(SpacedRepetition.MaxBox, card.Box);
        Assert.True(SpacedRepetition.IsMastered(card));
    }

    // ---- Progression -------------------------------------------------------------------------

    [Fact]
    public async Task EveryAnsweredQuestion_GetsAReviewCard()
    {
        await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10), ("q2", false, 0)));

        Assert.Equal(2, _store.Stored.Reviews["q1"].Box);
        Assert.Equal(1, _store.Stored.Reviews["q2"].Box);
    }

    [Fact]
    public async Task DueReviews_ComeBackTheNextDay_MostOverdueFirst()
    {
        await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10), ("q2", false, 0)));
        var progress = await _service.GetProgressAsync();
        Assert.Empty(_service.GetDueReviews(progress));

        _time.AdvanceDays(1);
        Assert.Equal(["q2"], _service.GetDueReviews(progress).Select(q => q.Id));

        _time.AdvanceDays(2);
        Assert.Equal(["q2", "q1"], _service.GetDueReviews(progress).Select(q => q.Id));
        Assert.Equal(2, _service.CountDueReviews(progress));
    }

    [Fact]
    public async Task ReviewSession_SplitsXpByCategory_WithoutPerfectBonus()
    {
        var session = new QuizBuilder(new Random(1)).ForReview([
            _content.FindQuestion("q1")!.Question,
            _content.FindQuestion("q3")!.Question,
        ]);
        do
        {
            session.Submit(new ChoiceAnswer(((ChoiceQuizQuestion)session.Current).CorrectIndex));
        } while (session.MoveNext());
        var result = session.BuildResult();

        Assert.True(result.IsReview);
        Assert.Equal(10 + 30, result.XpEarned); // pas de bonus sans faute pour une révision

        var update = await _service.ApplyResultAsync(result);
        Assert.Null(update.Before);
        Assert.Equal(10, _store.Stored.Categories["cat"].TotalXp);
        Assert.Equal(30, _store.Stored.Categories["autre"].TotalXp);
    }

    [Fact]
    public async Task ReviewedQuestionRemovedFromContent_IsIgnored()
    {
        var progress = await _service.GetProgressAsync();
        progress.Reviews["question-supprimee"] = new ReviewCard { Box = 1, Due = Today.AddDays(-5) };

        Assert.Empty(_service.GetDueReviews(progress));
        Assert.Equal(0, _service.CountDueReviews(progress));
    }

    // ---- Objectif quotidien --------------------------------------------------------------

    [Fact]
    public async Task DailyGoal_CountsTodayXp_AndDetectsWhenReached()
    {
        await _service.UpdateSettingsAsync(s => s.DailyGoalXp = 30);

        var first = await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10)));
        Assert.Equal(10, first.DailyGoal.XpToday);
        Assert.False(first.DailyGoalJustReached);

        var second = await _service.ApplyResultAsync(Lesson("cat", "l1", ("q2", true, 20), ("q1", true, 10)));
        Assert.True(second.DailyGoal.IsReached);
        Assert.True(second.DailyGoalJustReached);

        var third = await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10)));
        Assert.False(third.DailyGoalJustReached); // déjà atteint

        _time.AdvanceDays(1);
        Assert.Equal(0, _service.GetDailyGoal(await _service.GetProgressAsync()).XpToday);
    }

    [Fact]
    public async Task DailyHistory_IsTrimmed()
    {
        await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10)));
        _time.AdvanceDays(ProgressService.DailyHistoryDays + 5);
        await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10)));

        Assert.Single(_store.Stored.DailyXp);
    }

    [Fact]
    public async Task Settings_AreSaved_AndRaiseChanged()
    {
        var raised = 0;
        _service.Changed += (_, _) => raised++;

        await _service.UpdateSettingsAsync(s =>
        {
            s.RemindersEnabled = true;
            s.ReminderTime = new TimeOnly(8, 30);
        });

        Assert.True(_store.Stored.Settings.RemindersEnabled);
        Assert.Equal(new TimeOnly(8, 30), _store.Stored.Settings.ReminderTime);
        Assert.Equal(1, raised);
        Assert.NotNull(_store.Stored.UpdatedAt);
    }

    [Fact]
    public async Task Reset_ClearsProgress_ButKeepsSettings()
    {
        await _service.UpdateSettingsAsync(s => s.DailyGoalXp = 100);
        await _service.ApplyResultAsync(Lesson("cat", "l1", ("q1", true, 10)));

        await _service.ResetAsync();

        Assert.Empty(_store.Stored.Categories);
        Assert.Empty(_store.Stored.Reviews);
        Assert.Equal(100, _store.Stored.Settings.DailyGoalXp);
    }

    [Fact]
    public async Task OldSaveFile_LoadsWithDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"quizzware-{Guid.NewGuid()}.json");
        try
        {
            // Sauvegarde au format de la version 1 : ni réglages, ni révisions, ni historique quotidien.
            await File.WriteAllTextAsync(path, """
                { "Categories": { "cat": { "TotalXp": 120, "CompletedLessons": ["l1"] } },
                  "CurrentStreak": 3, "BestStreak": 5, "LastActivityDate": "2026-09-28" }
                """);

            var loaded = await new JsonFileProgressStore(path).LoadAsync();

            Assert.Equal(120, loaded.Categories["cat"].TotalXp);
            Assert.Equal(UserSettings.DefaultDailyGoalXp, loaded.Settings.DailyGoalXp);
            Assert.False(loaded.Settings.RemindersEnabled);
            Assert.Empty(loaded.Reviews);
            Assert.Empty(loaded.DailyXp);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- Rappels -------------------------------------------------------------------------

    private static readonly UserSettings Enabled = new() { RemindersEnabled = true, ReminderTime = new TimeOnly(19, 0) };

    [Fact]
    public void Reminders_Disabled_PlanNothing()
    {
        var plan = ReminderPlanner.Plan(new UserSettings(), new DailyGoalStatus(0, 30), 3, 0, new DateTime(2026, 9, 29, 12, 0, 0));
        Assert.Empty(plan);
    }

    [Fact]
    public void Reminders_PlanTodayAndNextDays_AtTheChosenTime()
    {
        var plan = ReminderPlanner.Plan(Enabled, new DailyGoalStatus(10, 30), streak: 4, dueReviews: 6, new DateTime(2026, 9, 29, 12, 0, 0));

        Assert.Equal(ReminderPlanner.DaysAhead, plan.Count);
        Assert.Equal(new DateTime(2026, 9, 29, 19, 0, 0), plan[0].When);
        Assert.Contains("4 jours", plan[0].Title);
        Assert.Contains("6 questions à réviser", plan[0].Message);
        Assert.Contains("20 XP", plan[0].Message);
        Assert.Equal(new DateTime(2026, 9, 30, 19, 0, 0), plan[1].When);
        Assert.All(plan, r => Assert.Contains(r.Id, ReminderPlanner.AllIds));
    }

    [Fact]
    public void Reminders_SkipToday_WhenGoalReachedOrTimePassed()
    {
        var goalReached = ReminderPlanner.Plan(Enabled, new DailyGoalStatus(30, 30), 1, 0, new DateTime(2026, 9, 29, 12, 0, 0));
        var tooLate = ReminderPlanner.Plan(Enabled, new DailyGoalStatus(0, 30), 1, 0, new DateTime(2026, 9, 29, 21, 0, 0));

        Assert.All(goalReached, r => Assert.True(r.When.Date > new DateTime(2026, 9, 29)));
        Assert.All(tooLate, r => Assert.True(r.When.Date > new DateTime(2026, 9, 29)));
        Assert.Equal(ReminderPlanner.DaysAhead - 1, goalReached.Count);
    }
}
