using QuizzWare.Core.Leveling;
using QuizzWare.Core.Models;

namespace QuizzWare.Core.Quiz;

public sealed record AnswerResult(bool IsCorrect, string Explanation, int XpEarned);

/// <summary>Résultat d'une question : alimente la révision espacée et, pour une révision, l'XP de chaque catégorie.</summary>
public sealed record QuestionOutcome(string QuestionId, bool IsCorrect, int XpEarned);

/// <param name="CategoryId">Catégorie jouée ; null pour une session de révision, qui mélange les catégories.</param>
/// <param name="UnitId">Leçon ou défi joué ; null pour un entraînement ou une révision.</param>
public sealed record QuizResult(
    string? CategoryId,
    string? UnitId,
    int TotalQuestions,
    int CorrectAnswers,
    int XpEarned,
    IReadOnlyList<QuestionOutcome>? Outcomes = null)
{
    public bool IsPerfect => TotalQuestions > 0 && CorrectAnswers == TotalQuestions;
    public int ScorePercent => TotalQuestions == 0 ? 0 : CorrectAnswers * 100 / TotalQuestions;
    public bool IsReview => CategoryId is null;
}

/// <summary>
/// Déroulement d'un quizz : une réponse par question, dans l'ordre, puis les erreurs à rejouer.
/// Seule la première réponse compte pour le score, l'XP et la révision espacée ; une question ratée
/// revient à la fin (remélangée) jusqu'à être réussie, dans la limite de <see cref="MaxRetries"/>.
/// </summary>
public sealed class QuizSession
{
    public const int MaxRetries = 2;

    private sealed record Step(QuizQuestion Question, int SourceIndex, bool IsRetry);

    private readonly List<Step> _steps;
    private readonly AnswerResult?[] _firstAnswers;
    private readonly int[] _retries;
    private readonly HashSet<int> _settled = [];
    private readonly Func<Question, QuizQuestion>? _prepareRetry;
    private bool _isCurrentAnswered;

    /// <param name="categoryId">null pour une session de révision.</param>
    /// <param name="prepareRetry">Prépare une question ratée pour la rejouer ; null pour ne pas rejouer les erreurs.</param>
    public QuizSession(string? categoryId, string? unitId, IReadOnlyList<QuizQuestion> questions,
        Func<Question, QuizQuestion>? prepareRetry = null)
    {
        if (questions.Count == 0)
            throw new ArgumentException("Un quizz doit contenir au moins une question.", nameof(questions));

        CategoryId = categoryId;
        UnitId = unitId;
        Questions = questions;
        _prepareRetry = prepareRetry;
        _steps = questions.Select((q, i) => new Step(q, i, IsRetry: false)).ToList();
        _firstAnswers = new AnswerResult?[questions.Count];
        _retries = new int[questions.Count];
    }

    public string? CategoryId { get; }
    public bool IsReview => CategoryId is null;
    public string? UnitId { get; }

    /// <summary>Les questions du quizz, sans les reprises.</summary>
    public IReadOnlyList<QuizQuestion> Questions { get; }

    /// <summary>Position dans le déroulement, reprises comprises.</summary>
    public int CurrentIndex { get; private set; }
    public QuizQuestion Current => _steps[CurrentIndex].Question;

    /// <summary>La question courante est une erreur rejouée.</summary>
    public bool IsRetry => _steps[CurrentIndex].IsRetry;
    public bool IsCurrentAnswered => _isCurrentAnswered;
    public bool HasNext => CurrentIndex < _steps.Count - 1;
    public bool IsCompleted => _isCurrentAnswered && !HasNext;
    public int CorrectCount => _firstAnswers.Count(a => a?.IsCorrect == true);

    /// <summary>Questions terminées : réussies, ou ratées sans reprise restante.</summary>
    public int SettledCount => _settled.Count;

    /// <summary>Erreurs encore à rejouer.</summary>
    public int PendingRetries => _steps.Skip(CurrentIndex + (_isCurrentAnswered ? 1 : 0)).Count(s => s.IsRetry);

    public AnswerResult Submit(Answer answer)
    {
        if (_isCurrentAnswered)
            throw new InvalidOperationException("La question courante a déjà une réponse.");

        var step = _steps[CurrentIndex];
        var isCorrect = step.Question.Grade(answer);

        // Une reprise ne rapporte rien : l'XP récompense la première réponse.
        var xp = isCorrect && !step.IsRetry ? LevelSystem.XpForCorrectAnswer(step.Question.Source.Difficulty) : 0;
        var result = new AnswerResult(isCorrect, step.Question.Source.Explanation, xp);
        if (!step.IsRetry)
            _firstAnswers[step.SourceIndex] = result;

        if (!isCorrect && _prepareRetry is not null && _retries[step.SourceIndex] < MaxRetries)
        {
            _retries[step.SourceIndex]++;
            _steps.Add(new Step(_prepareRetry(step.Question.Source), step.SourceIndex, IsRetry: true));
        }
        else
        {
            _settled.Add(step.SourceIndex);
        }

        _isCurrentAnswered = true;
        return result;
    }

    /// <summary>Première réponse à la question d'indice <paramref name="index"/> : null si elle n'a pas encore été posée.</summary>
    public bool? IsCorrectAt(int index) => _firstAnswers[index]?.IsCorrect;

    /// <summary>XP gagnée jusqu'ici (premières réponses uniquement).</summary>
    public int XpSoFar => _firstAnswers.Sum(a => a?.XpEarned ?? 0);

    /// <summary>
    /// Temps écoulé (mode entretien) : les questions sans réponse comptent comme fausses et la session se termine.
    /// </summary>
    public void Expire()
    {
        for (var i = 0; i < _firstAnswers.Length; i++)
            _firstAnswers[i] ??= new AnswerResult(false, Questions[i].Source.Explanation, 0);

        _steps.RemoveRange(CurrentIndex + 1, _steps.Count - CurrentIndex - 1);
        _isCurrentAnswered = true;
    }

    public bool MoveNext()
    {
        if (!_isCurrentAnswered || !HasNext)
            return false;
        CurrentIndex++;
        _isCurrentAnswered = false;
        return true;
    }

    public QuizResult BuildResult()
    {
        if (!IsCompleted)
            throw new InvalidOperationException("Le quizz n'est pas terminé.");

        var correct = CorrectCount;
        var xp = _firstAnswers.Sum(a => a!.XpEarned);

        // Le bonus sans faute récompense une leçon ou un entraînement, pas une révision (dont l'XP est répartie par catégorie).
        if (correct == Questions.Count && !IsReview)
            xp += LevelSystem.PerfectQuizBonus;

        var outcomes = Questions.Zip(_firstAnswers, (q, a) => new QuestionOutcome(q.Source.Id, a!.IsCorrect, a.XpEarned)).ToList();
        return new QuizResult(CategoryId, UnitId, Questions.Count, correct, xp, outcomes);
    }
}
