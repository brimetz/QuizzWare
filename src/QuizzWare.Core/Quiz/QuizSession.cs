using QuizzWare.Core.Leveling;

namespace QuizzWare.Core.Quiz;

public sealed record AnswerResult(bool IsCorrect, string Explanation, int XpEarned);

/// <param name="UnitId">Leçon ou défi joué ; null pour un entraînement.</param>
public sealed record QuizResult(string CategoryId, string? UnitId, int TotalQuestions, int CorrectAnswers, int XpEarned)
{
    public bool IsPerfect => TotalQuestions > 0 && CorrectAnswers == TotalQuestions;
    public int ScorePercent => TotalQuestions == 0 ? 0 : CorrectAnswers * 100 / TotalQuestions;
}

/// <summary>Déroulement d'un quizz : une réponse par question, dans l'ordre.</summary>
public sealed class QuizSession
{
    private readonly List<AnswerResult> _answers = [];

    public QuizSession(string categoryId, string? unitId, IReadOnlyList<QuizQuestion> questions)
    {
        if (questions.Count == 0)
            throw new ArgumentException("Un quizz doit contenir au moins une question.", nameof(questions));

        CategoryId = categoryId;
        UnitId = unitId;
        Questions = questions;
    }

    public string CategoryId { get; }
    public string? UnitId { get; }
    public IReadOnlyList<QuizQuestion> Questions { get; }
    public int CurrentIndex { get; private set; }
    public QuizQuestion Current => Questions[CurrentIndex];
    public bool IsCurrentAnswered => _answers.Count > CurrentIndex;
    public bool IsCompleted => _answers.Count == Questions.Count;
    public bool HasNext => CurrentIndex < Questions.Count - 1;
    public int CorrectCount => _answers.Count(a => a.IsCorrect);

    public AnswerResult Submit(Answer answer)
    {
        if (IsCurrentAnswered)
            throw new InvalidOperationException("La question courante a déjà une réponse.");

        var isCorrect = Current.Grade(answer);
        var xp = isCorrect ? LevelSystem.XpForCorrectAnswer(Current.Source.Difficulty) : 0;
        var result = new AnswerResult(isCorrect, Current.Source.Explanation, xp);
        _answers.Add(result);
        return result;
    }

    public bool MoveNext()
    {
        if (!IsCurrentAnswered || !HasNext)
            return false;
        CurrentIndex++;
        return true;
    }

    public QuizResult BuildResult()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Le quizz n'est pas terminé.");

        var correct = CorrectCount;
        var xp = _answers.Sum(a => a.XpEarned);
        if (correct == Questions.Count)
            xp += LevelSystem.PerfectQuizBonus;

        return new QuizResult(CategoryId, UnitId, Questions.Count, correct, xp);
    }
}
