using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;

namespace QuizzWare.Core.Tests;

internal sealed class InMemoryProgressStore : IProgressStore
{
    public UserProgress Stored { get; private set; } = new();
    public int SaveCount { get; private set; }

    public Task<UserProgress> LoadAsync() => Task.FromResult(Stored);

    public Task SaveAsync(UserProgress progress)
    {
        Stored = progress;
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public void AdvanceDays(int days) => Now = Now.AddDays(days);
}

internal static class Sample
{
    public static ChoiceQuestion Question(string id, int difficulty = 1, int answerIndex = 0) => new()
    {
        Id = id,
        Prompt = $"Question {id}",
        Choices = ["A", "B", "C", "D"],
        AnswerIndex = answerIndex,
        Explanation = "Parce que.",
        Difficulty = difficulty,
    };

    public static Lesson Lesson(string id, params Question[] questions) =>
        new(id, $"Leçon {id}", "Résumé", 1, [new LessonSection("Section", "Corps")], questions);

    public static Chapter Chapter(string id, Challenge? challenge, params Lesson[] lessons) =>
        new(id, $"Chapitre {id}", "Résumé", lessons, challenge);

    public static Category Category(string id = "cat", params Chapter[] chapters) =>
        new(id, 1, "Catégorie", "Description", "CAT", "#000000", chapters);

    /// <summary>
    /// cat
    ///   ch1 : l1 (q1, q2, q3, q4), l2 (q5, q6) + défi d1 (dq1, dq2)
    ///   ch2 : l3 (q7)
    /// </summary>
    /// <summary>
    /// Comme <see cref="Repository"/>, plus un arbre ouvert par le chapitre ch1 :
    ///   fam (famille) → tech (parcours : s1, s2 + défi sd), vide (bientôt)
    /// </summary>
    public static ContentRepository RepositoryWithTree()
    {
        var category = Repository().GetCategory("cat");
        var track = Chapter("tch", new Challenge("sd", "Défi", "Résumé", [Question("sdq")]),
            Lesson("s1", Question("sq1")), Lesson("s2", Question("sq2")));
        var tree = new SpecializationTree("ch1",
        [
            new SpecializationNode("fam", "Famille", "Résumé", "F", SpecializationKind.Family,
            [
                new SpecializationNode("tech", "Techno", "Résumé", "T", SpecializationKind.Technology, Chapters: [track], Language: "javascript"),
                new SpecializationNode("vide", "Bientôt", "Résumé", "B", SpecializationKind.Library),
            ]),
        ]);
        return new ContentRepository([category with { Specializations = tree }]);
    }

    public static ContentRepository Repository() => new([
        Category("cat",
            Chapter("ch1", new Challenge("d1", "Défi", "Résumé", [Question("dq1", 2), Question("dq2", 3)]),
                Lesson("l1", Question("q1"), Question("q2"), Question("q3", 2), Question("q4", 3)),
                Lesson("l2", Question("q5"), Question("q6", 2))),
            Chapter("ch2", null,
                Lesson("l3", Question("q7")))),
    ]);
}
