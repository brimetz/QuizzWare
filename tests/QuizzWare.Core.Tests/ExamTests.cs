using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

/// <summary>Test de niveau et simulation d'entretien.</summary>
public class ExamTests
{
    private readonly InMemoryProgressStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly QuizBuilder _builder = new(new Random(7));
    private readonly ContentRepository _content;
    private readonly ProgressService _service;

    /// <summary>
    /// cat : c1 (a1, a2 + défi), c2 (b1, b2), c3 (d1), c4 (à venir)
    /// Chaque leçon a des questions de difficulté 1 à 3.
    /// </summary>
    public ExamTests()
    {
        static Lesson L(string id) => Sample.Lesson(id,
            Sample.Question($"{id}-q1", 1), Sample.Question($"{id}-q2", 2), Sample.Question($"{id}-q3", 3));

        var challenge = new Challenge("defi", "Défi", "Résumé",
        [
            Sample.Question("defi-q1", 3) with { Prompt = "En entretien : pourquoi ?" },
            Sample.Question("defi-q2", 3) with { Prompt = "Cas pratique 2/3 — Quel correctif ?" },
        ]);

        _content = new ContentRepository([
            Sample.Category("cat",
                Sample.Chapter("c1", challenge, L("a1"), L("a2")),
                Sample.Chapter("c2", null, L("b1"), L("b2")),
                Sample.Chapter("c3", null, L("d1")),
                Sample.Chapter("c4", null, new Lesson("planifiee", "À venir", "Résumé", 1, null, null))),
        ]);
        _service = new ProgressService(_store, _content, _time);
    }

    private Category Cat => _content.GetCategory("cat");

    private static void Answer(QuizSession session, bool correct)
    {
        var q = (ChoiceQuizQuestion)session.Current;
        session.Submit(new ChoiceAnswer(correct ? q.CorrectIndex : (q.CorrectIndex + 1) % q.Choices.Count));
    }

    // ---- Test de niveau ------------------------------------------------------------------

    [Fact]
    public void Placement_AsksTwoQuestionsPerPlayableChapter_InOrder()
    {
        var test = _builder.ForPlacement(Cat);

        Assert.Equal(["c1", "c2", "c3"], test.Chapters.Select(c => c.Id));
        Assert.Equal(6, test.Session.Questions.Count); // c3 n'a qu'une leçon, mais 3 questions : 2 sont posées
        Assert.Equal(6, test.Session.Questions.Select(q => q.Source.Id).Distinct().Count());
        Assert.All(test.Session.Questions.Take(2), q => Assert.StartsWith("a", q.Source.Id));
        Assert.All(test.Session.Questions, q => Assert.True(QuizBuilder.IsStandalone(q.Source)));
    }

    [Fact]
    public void Placement_ChapterQuestionsComeFromDifferentLessons()
    {
        var test = _builder.ForPlacement(Cat);
        var lessons = test.Session.Questions.Take(2).Select(q => q.Source.Id[..2]).ToList();

        Assert.Equal(2, lessons.Distinct().Count());
    }

    [Fact]
    public void Placement_StopsAtFirstFailedChapter()
    {
        var test = _builder.ForPlacement(Cat);

        Answer(test.Session, correct: true);
        Assert.False(test.IsFinished);
        test.Session.MoveNext();
        Answer(test.Session, correct: true);
        Assert.False(test.IsFinished); // c1 acquis : on continue
        test.Session.MoveNext();

        Answer(test.Session, correct: false);
        Assert.False(test.IsFinished); // c2 pas encore terminé
        test.Session.MoveNext();
        Answer(test.Session, correct: true);

        Assert.True(test.IsFinished);
        Assert.Equal(1, test.PassedChapters);
    }

    [Fact]
    public async Task ApplyPlacement_ValidatesLessonsOfPassedChapters_NotChallenges()
    {
        var test = _builder.ForPlacement(Cat);
        do Answer(test.Session, correct: true);
        while (!test.IsFinished && test.Session.MoveNext());

        var update = await _service.ApplyPlacementAsync(test);
        var progress = await _service.GetProgressAsync();

        Assert.Equal(3, update.PassedChapters);
        Assert.Equal(5, update.LessonsValidated); // a1, a2, b1, b2, d1
        Assert.Null(update.NextLesson);
        Assert.DoesNotContain("defi", progress.Categories["cat"].CompletedLessons);
        Assert.Equal(UnitState.Available, _service.GetChallengeState(progress, Cat, Cat.Chapters[0]));
        Assert.True(update.XpEarned > 0);
        Assert.Equal(update.XpEarned, progress.Categories["cat"].TotalXp);
    }

    [Fact]
    public async Task ApplyPlacement_NextLessonIsFirstOfFailedChapter()
    {
        var test = _builder.ForPlacement(Cat);
        Answer(test.Session, correct: true);
        test.Session.MoveNext();
        Answer(test.Session, correct: false);
        Assert.True(test.IsFinished);

        var update = await _service.ApplyPlacementAsync(test);
        var progress = await _service.GetProgressAsync();

        Assert.Equal(0, update.PassedChapters);
        Assert.Equal("a1", update.NextLesson?.Id);
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "a1"));
        Assert.Equal(UnitState.Locked, _service.GetLessonState(progress, Cat, "a2"));
    }

    [Fact]
    public async Task Placement_OnlyOfferedBeforeAnyLesson()
    {
        var progress = await _service.GetProgressAsync();
        Assert.True(_service.CanTakePlacement(progress, Cat));

        progress.GetOrCreate("cat").CompletedLessons.Add("a1");
        Assert.False(_service.CanTakePlacement(progress, Cat));
    }

    // ---- Simulation d'entretien ----------------------------------------------------------

    [Fact]
    public void Interview_RespectsLevelAndExcludesContextQuestions()
    {
        var session = _builder.ForInterview(Cat, InterviewLevel.Senior with { QuestionCount = 6 });

        Assert.Equal(6, session.Questions.Count);
        Assert.All(session.Questions, q => Assert.Equal(3, q.Source.Difficulty));
        Assert.DoesNotContain(session.Questions, q => q.Source.Id == "defi-q2");
        Assert.Equal(6, session.Questions.Select(q => q.Source.Id).Distinct().Count());
    }

    [Fact]
    public void Interview_FillsWithClosestDifficulty_WhenNotEnough()
    {
        // Seulement 6 questions de difficulté 3 (5 leçons + 1 défi) : on complète avec des questions de difficulté 2.
        var session = _builder.ForInterview(Cat, InterviewLevel.Senior with { QuestionCount = 8 });

        Assert.Equal(8, session.Questions.Count);
        Assert.Equal(6, session.Questions.Count(q => q.Source.Difficulty == 3));
        Assert.Equal(2, session.Questions.Count(q => q.Source.Difficulty == 2));
    }

    [Fact]
    public void Interview_SpreadsQuestionsAcrossChapters()
    {
        var session = _builder.ForInterview(Cat, InterviewLevel.Junior with { QuestionCount = 3 });
        var chapters = session.Questions.Select(q => _content.FindQuestion(q.Source.Id)!.Chapter.Id).Distinct();

        Assert.Equal(3, chapters.Count());
    }

    [Fact]
    public void Expire_CountsUnansweredAsWrong_AndCompletes()
    {
        var session = _builder.ForInterview(Cat, InterviewLevel.Junior with { QuestionCount = 4 });
        Answer(session, correct: true);
        session.MoveNext();

        session.Expire();

        Assert.True(session.IsCompleted);
        var result = session.BuildResult();
        Assert.Equal(4, result.TotalQuestions);
        Assert.Equal(1, result.CorrectAnswers);
        Assert.Equal(3, result.Outcomes!.Count(o => !o.IsCorrect));
    }

    [Fact]
    public void Report_ListsWeakTopics_WorstFirst()
    {
        var result = new QuizResult("cat", null, 5, 2, 0,
        [
            new QuestionOutcome("a1-q1", true, 10),
            new QuestionOutcome("a2-q1", true, 10),
            new QuestionOutcome("b1-q1", false, 0),
            new QuestionOutcome("b2-q1", false, 0),
            new QuestionOutcome("d1-q1", false, 0),
        ]);

        var report = InterviewReport.Evaluate(result, InterviewLevel.Junior, TimeSpan.FromMinutes(3), timeExpired: false, _content);

        Assert.Equal(40, report.Percent);
        Assert.Equal(["Chapitre c2", "Chapitre c3"], report.WeakTopics.Select(t => t.Title));
        Assert.StartsWith("Pas encore", report.Verdict);
    }

    [Fact]
    public void Report_IgnoresUnansweredQuestionsInWeakTopics()
    {
        var result = new QuizResult("cat", null, 2, 0, 0,
            [new QuestionOutcome("a1-q1", false, 0), new QuestionOutcome("b1-q1", false, 0)]);

        var report = InterviewReport.Evaluate(result, InterviewLevel.Junior, TimeSpan.FromMinutes(8), timeExpired: true, _content,
            unanswered: new HashSet<string> { "b1-q1" });

        Assert.Equal(0, report.Percent);
        Assert.Equal(["Chapitre c1"], report.WeakTopics.Select(t => t.Title));
    }

    [Fact]
    public void RealContent_SupportsPlacementAndEveryInterviewLevel()
    {
        var content = ContentRepository.LoadEmbedded();
        foreach (var category in content.Categories.Where(c => c.MainLessons.Any(l => !l.IsPlanned)))
        {
            var test = _builder.ForPlacement(category);
            Assert.Equal(test.Chapters.Count * PlacementTest.QuestionsPerChapter, test.Session.Questions.Count);

            foreach (var level in InterviewLevel.All.Where(l => QuizBuilder.CanInterview(category, l)))
                Assert.Equal(level.QuestionCount, _builder.ForInterview(category, level).Questions.Count);
        }

        // Les troncs communs complets proposent tous les niveaux.
        foreach (var id in new[] { "cpp", "csharp", "bases-de-donnees" })
            Assert.All(InterviewLevel.All, l => Assert.True(QuizBuilder.CanInterview(content.GetCategory(id), l), $"{id} / {l.Id}"));
    }

    [Theory]
    [InlineData(8, "Entretien réussi")]
    [InlineData(6, "Presque")]
    [InlineData(5, "Pas encore")]
    public void Report_Verdict_DependsOnScore(int correct, string expected)
    {
        var report = new InterviewReport(InterviewLevel.Junior, correct, 10, TimeSpan.Zero, false, []);

        Assert.StartsWith(expected, report.Verdict);
    }
}
