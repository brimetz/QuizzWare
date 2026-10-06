using QuizzWare.Core.Leveling;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

public class QuizTests
{
    private readonly QuizBuilder _builder = new(new Random(42));

    private static QuizzWare.Core.Models.Category Cat() => Sample.Repository().GetCategory("cat");

    [Fact]
    public void Prepare_KeepsCorrectAnswerAfterShuffle()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);

        foreach (var q in session.Questions.Cast<ChoiceQuizQuestion>())
        {
            var source = (QuizzWare.Core.Models.ChoiceQuestion)q.Source;
            Assert.Equal(source.Choices[source.AnswerIndex], q.Choices[q.CorrectIndex]);
        }
    }

    [Fact]
    public void ForLesson_ContainsAllLessonQuestions()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);

        Assert.Equal(["q1", "q2", "q3", "q4"], session.Questions.Select(q => q.Source.Id).Order());
        Assert.Equal("l1", session.UnitId);
    }

    [Fact]
    public void ForChallenge_KeepsQuestionOrder()
    {
        var category = Cat();
        var session = _builder.ForChallenge(category, category.Chapters[0].Challenge!);

        Assert.Equal(["dq1", "dq2"], session.Questions.Select(q => q.Source.Id));
        Assert.Equal("d1", session.UnitId);
    }

    [Fact]
    public void ForPractice_AtLowLevel_OnlyEasyQuestionsFromGivenLessons()
    {
        var category = Cat();
        var lessons = category.Chapters[0].Lessons;
        var session = _builder.ForPractice(category, level: 1, lessons, count: 3);

        Assert.Equal(3, session.Questions.Count);
        Assert.All(session.Questions, q => Assert.Equal(1, q.Source.Difficulty));
        Assert.DoesNotContain(session.Questions, q => q.Source.Id == "q7");
        Assert.Null(session.UnitId);
    }

    [Fact]
    public void ForPractice_NotEnoughEligible_FillsWithEasiestRemaining()
    {
        var category = Cat();
        var session = _builder.ForPractice(category, level: 1, category.Chapters[0].Lessons, count: 5);

        Assert.Equal(5, session.Questions.Count);
        Assert.DoesNotContain(session.Questions, q => q.Source.Difficulty == 3);
    }

    [Fact]
    public void Session_PerfectRun_EarnsXpAndBonus()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);

        do
        {
            var q = (ChoiceQuizQuestion)session.Current;
            Assert.True(session.Submit(new ChoiceAnswer(q.CorrectIndex)).IsCorrect);
        } while (session.MoveNext());

        var result = session.BuildResult();
        var expectedXp = (1 + 1 + 2 + 3) * LevelSystem.XpPerDifficultyPoint + LevelSystem.PerfectQuizBonus;
        Assert.True(result.IsPerfect);
        Assert.Equal(4, result.CorrectAnswers);
        Assert.Equal(expectedXp, result.XpEarned);
    }

    [Fact]
    public void Session_WrongAnswers_EarnNothing()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);

        do
        {
            var q = (ChoiceQuizQuestion)session.Current;
            Assert.False(session.Submit(new ChoiceAnswer((q.CorrectIndex + 1) % q.Choices.Count)).IsCorrect);
        } while (session.MoveNext());

        var result = session.BuildResult();
        Assert.Equal(0, result.XpEarned);
        Assert.Equal(0, result.ScorePercent);
    }

    [Fact]
    public void Session_WrongAnswer_IsReplayedAtTheEnd_WithoutXpOrScoreChange()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);
        var firstId = session.Current.Source.Id;

        // Première question ratée, les autres réussies.
        var first = (ChoiceQuizQuestion)session.Current;
        Assert.False(session.Submit(new ChoiceAnswer((first.CorrectIndex + 1) % first.Choices.Count)).IsCorrect);
        Assert.Equal(1, session.PendingRetries);
        while (session.MoveNext() && !session.IsRetry)
        {
            var q = (ChoiceQuizQuestion)session.Current;
            session.Submit(new ChoiceAnswer(q.CorrectIndex));
        }

        // La reprise arrive après les 4 questions, et sa réussite ne rapporte rien.
        Assert.True(session.IsRetry);
        Assert.Equal(4, session.CurrentIndex);
        Assert.Equal(firstId, session.Current.Source.Id);
        var retry = (ChoiceQuizQuestion)session.Current;
        var answer = session.Submit(new ChoiceAnswer(retry.CorrectIndex));
        Assert.True(answer.IsCorrect);
        Assert.Equal(0, answer.XpEarned);
        Assert.True(session.IsCompleted);
        Assert.Equal(4, session.SettledCount);

        var result = session.BuildResult();
        Assert.Equal(3, result.CorrectAnswers);
        Assert.False(result.IsPerfect);
        Assert.False(result.Outcomes!.Single(o => o.QuestionId == firstId).IsCorrect);
    }

    [Fact]
    public void Session_RetriesAreLimited()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);
        var steps = 0;

        do
        {
            var q = (ChoiceQuizQuestion)session.Current;
            session.Submit(new ChoiceAnswer((q.CorrectIndex + 1) % q.Choices.Count));
            steps++;
        } while (session.MoveNext());

        Assert.Equal(4 * (1 + QuizSession.MaxRetries), steps);
        Assert.True(session.IsCompleted);
    }

    [Fact]
    public void Session_CannotAnswerTwiceOrSkip()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);

        Assert.False(session.MoveNext());
        session.Submit(new ChoiceAnswer(0));
        Assert.Throws<InvalidOperationException>(() => session.Submit(new ChoiceAnswer(0)));
        Assert.Throws<InvalidOperationException>(() => session.BuildResult());
    }

    [Fact]
    public void Session_WrongAnswerType_Throws()
    {
        var category = Cat();
        var session = _builder.ForLesson(category, category.Chapters[0].Lessons[0]);

        Assert.Throws<ArgumentException>(() => session.Submit(new LineAnswer(0)));
    }
}
