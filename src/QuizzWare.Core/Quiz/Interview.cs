using QuizzWare.Core.Content;

namespace QuizzWare.Core.Quiz;

/// <summary>Un niveau de simulation d'entretien : nombre de questions, durée et difficulté visée.</summary>
public sealed record InterviewLevel(string Id, string Label, int QuestionCount, TimeSpan Duration, int MinDifficulty, int MaxDifficulty)
{
    public static readonly InterviewLevel Junior = new("junior", "Junior", 10, TimeSpan.FromMinutes(8), 1, 2);
    public static readonly InterviewLevel Confirme = new("confirme", "Confirmé", 15, TimeSpan.FromMinutes(12), 2, 3);
    public static readonly InterviewLevel Senior = new("senior", "Senior", 15, TimeSpan.FromMinutes(10), 3, 3);

    public static IReadOnlyList<InterviewLevel> All { get; } = [Junior, Confirme, Senior];

    public static InterviewLevel Get(string id) =>
        All.FirstOrDefault(l => l.Id == id) ?? throw new KeyNotFoundException($"Niveau d'entretien inconnu : {id}");

    public string Description => $"{Label} · {QuestionCount} questions · {Duration.TotalMinutes:0} min";
}

/// <param name="Title">Titre du chapitre.</param>
public sealed record TopicScore(string Title, int Correct, int Total)
{
    public int Percent => Total == 0 ? 0 : Correct * 100 / Total;
}

/// <summary>Bilan d'une simulation d'entretien : verdict et sujets à retravailler.</summary>
public sealed record InterviewReport(
    InterviewLevel Level,
    int Correct,
    int Total,
    TimeSpan Elapsed,
    bool TimeExpired,
    IReadOnlyList<TopicScore> WeakTopics)
{
    public const int PassPercent = 80;
    public const int AlmostPercent = 60;
    public const int MaxWeakTopics = 3;

    public int Percent => Total == 0 ? 0 : Correct * 100 / Total;

    public string Verdict => Percent switch
    {
        >= PassPercent => $"Entretien réussi : tu as le niveau {Level.Label}.",
        >= AlmostPercent => "Presque : quelques notions à consolider.",
        _ => "Pas encore : revois les sujets ci-dessous, puis retente.",
    };

    /// <param name="unanswered">Questions non posées faute de temps : comptées fausses, mais pas comme des sujets à retravailler.</param>
    public static InterviewReport Evaluate(QuizResult result, InterviewLevel level, TimeSpan elapsed, bool timeExpired, ContentRepository content,
        IReadOnlySet<string>? unanswered = null)
    {
        var weak = (result.Outcomes ?? [])
            .Where(o => unanswered?.Contains(o.QuestionId) != true)
            .Select(o => (Outcome: o, Location: content.FindQuestion(o.QuestionId)))
            .Where(x => x.Location is not null)
            .GroupBy(x => x.Location!.Chapter.Title)
            .Select(g => new TopicScore(g.Key, g.Count(x => x.Outcome.IsCorrect), g.Count()))
            .Where(t => t.Correct < t.Total)
            .OrderBy(t => (double)t.Correct / t.Total).ThenByDescending(t => t.Total).ThenBy(t => t.Title, StringComparer.Ordinal)
            .Take(MaxWeakTopics)
            .ToList();

        return new InterviewReport(level, result.CorrectAnswers, result.TotalQuestions, elapsed, timeExpired, weak);
    }
}
