using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

/// <summary>Une mise à jour du contenu ne doit jamais faire perdre sa progression à l'utilisateur.</summary>
public class ContentUpdateTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));

    private static UserProgress SavedWith(params string[] completed)
    {
        var progress = new UserProgress();
        progress.GetOrCreate("cat").CompletedLessons.UnionWith(completed);
        return progress;
    }

    private async Task<(ProgressService Service, UserProgress Progress, InMemoryProgressStore Store)> Open(
        ContentRepository content, UserProgress saved)
    {
        var store = new InMemoryProgressStore();
        await store.SaveAsync(saved);
        var service = new ProgressService(store, content, _time);
        return (service, await service.GetProgressAsync(), store);
    }

    // --- Identifiants renommés ou retirés -----------------------------------------------------

    [Fact]
    public async Task RenamedLesson_KeepsCompletionAndBestScore()
    {
        var saved = SavedWith("l1-ancien");
        saved.Categories["cat"].BestScores["l1-ancien"] = 90;
        var content = new ContentRepository(Sample.Repository().Categories, new Dictionary<string, string?> { ["l1-ancien"] = "l1" });

        var (service, progress, store) = await Open(content, saved);
        var cat = content.GetCategory("cat");

        Assert.Equal(UnitState.Completed, service.GetLessonState(progress, cat, "l1"));
        Assert.Equal(UnitState.Available, service.GetLessonState(progress, cat, "l2"));
        Assert.Equal(90, progress.Categories["cat"].BestScores["l1"]);
        Assert.DoesNotContain("l1-ancien", store.Stored.Categories["cat"].CompletedLessons); // la migration est sauvegardée
    }

    [Fact]
    public async Task RenamedQuestion_KeepsItsReviewCard_AndChainsAreFollowed()
    {
        var saved = new UserProgress();
        saved.Reviews["q-v1"] = new ReviewCard { Box = 4, Due = new DateOnly(2026, 10, 10) };
        var renames = new Dictionary<string, string?> { ["q-v1"] = "q-v2", ["q-v2"] = "q1" };
        var content = new ContentRepository(Sample.Repository().Categories, renames);

        var (_, progress, _) = await Open(content, saved);

        Assert.Equal(4, progress.Reviews["q1"].Box);
        Assert.False(progress.Reviews.ContainsKey("q-v1"));
    }

    [Fact]
    public async Task RetiredLesson_IsDropped_ButXpIsKept()
    {
        var saved = SavedWith("l1", "retiree");
        saved.Categories["cat"].TotalXp = 250;
        var content = new ContentRepository(Sample.Repository().Categories, new Dictionary<string, string?> { ["retiree"] = null });

        var (_, progress, _) = await Open(content, saved);

        Assert.Equal(["l1"], progress.Categories["cat"].CompletedLessons);
        Assert.Equal(250, progress.Categories["cat"].TotalXp);
    }

    [Fact]
    public async Task RenamedCategory_MovesItsProgress()
    {
        var saved = new UserProgress();
        saved.GetOrCreate("ancienne").TotalXp = 120;
        saved.Categories["ancienne"].CompletedLessons.Add("l1");
        saved.Settings.Interests = ["ancienne"];
        var content = new ContentRepository(Sample.Repository().Categories, new Dictionary<string, string?> { ["ancienne"] = "cat" });

        var (_, progress, _) = await Open(content, saved);

        Assert.Equal(120, progress.Categories["cat"].TotalXp);
        Assert.Contains("l1", progress.Categories["cat"].CompletedLessons);
        Assert.Equal(["cat"], progress.Settings.Interests);
    }

    [Fact]
    public void Renames_AreValidated()
    {
        var categories = Sample.Repository().Categories;
        Assert.Throws<InvalidDataException>(() => new ContentRepository(categories, new Dictionary<string, string?> { ["l1"] = "l2" }));
        Assert.Throws<InvalidDataException>(() => new ContentRepository(categories, new Dictionary<string, string?> { ["x"] = "inconnu" }));
        Assert.Throws<InvalidDataException>(() => new ContentRepository(categories, new Dictionary<string, string?> { ["a"] = "b", ["b"] = "a" }));
    }

    // --- Leçons ajoutées dans un parcours déjà commencé ----------------------------------------

    /// <summary>Le contenu de <see cref="Sample.Repository"/>, avec une leçon « nouvelle » insérée au début du chapitre ch1.</summary>
    private static ContentRepository WithInsertedLesson(bool withTree = false)
    {
        var source = withTree ? Sample.RepositoryWithTree() : Sample.Repository();
        var cat = source.GetCategory("cat");
        var ch1 = cat.Chapters[0];
        var updated = ch1 with { Lessons = [Sample.Lesson("nouvelle", Sample.Question("nq")), .. ch1.Lessons] };
        return new ContentRepository([cat with { Chapters = [updated, .. cat.Chapters.Skip(1)] }]);
    }

    [Fact]
    public async Task InsertedLesson_DoesNotLockWhatTheUserHadReached()
    {
        var content = WithInsertedLesson();
        var (service, progress, _) = await Open(content, SavedWith("l1", "l2", "l3"));
        var cat = content.GetCategory("cat");

        Assert.Equal(UnitState.Available, service.GetLessonState(progress, cat, "nouvelle"));
        Assert.Equal(UnitState.Completed, service.GetLessonState(progress, cat, "l2"));
        Assert.Equal(UnitState.Available, service.GetChallengeState(progress, cat, cat.Chapters[0]));
    }

    [Fact]
    public async Task InsertedLesson_StillLocksForNewUsers()
    {
        var content = WithInsertedLesson();
        var (service, progress, _) = await Open(content, new UserProgress());
        var cat = content.GetCategory("cat");

        Assert.Equal(UnitState.Available, service.GetLessonState(progress, cat, "nouvelle"));
        Assert.Equal(UnitState.Locked, service.GetLessonState(progress, cat, "l1"));
        Assert.Equal(UnitState.Locked, service.GetChallengeState(progress, cat, cat.Chapters[0]));
    }

    [Fact]
    public async Task LessonAddedToTheUnlockingChapter_KeepsTheTreeOpen()
    {
        var content = WithInsertedLesson(withTree: true);
        var cat = content.GetCategory("cat");

        var (service, progress, _) = await Open(content, SavedWith("l1", "l2", "s1"));
        Assert.True(service.AreSpecializationsUnlocked(progress, cat));
        Assert.Equal(UnitState.Available, service.GetLessonState(progress, cat, "s2"));

        var (fresh, freshProgress, _) = await Open(content, SavedWith("l1", "l2"));
        Assert.False(fresh.AreSpecializationsUnlocked(freshProgress, cat)); // personne n'avait encore ouvert l'arbre
    }

    // --- Garde-fou sur le vrai contenu ---------------------------------------------------------

    [Fact]
    public void PublishedIds_NeverDisappearWithoutARename()
    {
        var content = ContentRepository.LoadEmbedded();
        var existing = ContentRepository.ProgressIds(content.Categories).ToHashSet();
        var published = File.ReadAllLines(Path.Combine(RepositoryRoot(), "src", "QuizzWare.Core", "Progress", "identifiants-publies.txt"))
            .Where(l => l.Length > 0 && !l.StartsWith('#'));

        var lost = published
            .Where(id => !existing.Contains(id) && !content.Renames.ContainsKey(id))
            .ToList();

        Assert.True(lost.Count == 0,
            "Identifiants publiés absents du contenu : la progression des utilisateurs serait perdue. " +
            "Les inscrire dans src/QuizzWare.Core/Progress/renommages.json (nouveau nom, ou null si retiré) : " +
            string.Join(", ", lost.Take(20)));
    }

    [Fact]
    public void CurrentContent_IsFullyPublished()
    {
        // Le générateur ajoute les nouveaux identifiants à la liste ; la CI vérifie qu'il a été lancé.
        var content = ContentRepository.LoadEmbedded();
        var published = File.ReadAllLines(Path.Combine(RepositoryRoot(), "src", "QuizzWare.Core", "Progress", "identifiants-publies.txt")).ToHashSet();
        var playable = content.Categories.SelectMany(c => c.AllChapters)
            .SelectMany(ch => ch.Lessons.Cast<IQuizUnit>().Append(ch.Challenge).OfType<IQuizUnit>())
            .Where(u => u.Questions.Count > 0)
            .SelectMany(u => u.Questions.Select(q => q.Id).Prepend(u.Id));

        var missing = playable.Where(id => !published.Contains(id)).Take(20).ToList();
        Assert.True(missing.Count == 0, "Lancer dotnet run --project tools/GenerateurContenu : " + string.Join(", ", missing));
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "QuizzWare.sln")))
                return dir.FullName;
        throw new InvalidOperationException("Dépôt introuvable.");
    }
}
