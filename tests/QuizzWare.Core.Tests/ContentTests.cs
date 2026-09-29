using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;

namespace QuizzWare.Core.Tests;

public class ContentTests
{
    [Fact]
    public void EmbeddedContent_LoadsAllCategories_InOrder()
    {
        var repository = ContentRepository.LoadEmbedded();

        Assert.Equal(
            ["architecture-systeme", "architecture-infra", "reseau", "cpp", "csharp", "c", "python", "bases-de-donnees",
             "algorithmes", "javascript", "java", "go", "rust", "securite", "systemes", "git", "tests", "frontend", "mobile", "ia"],
            repository.Categories.Select(c => c.Id));
    }

    [Fact]
    public void EmbeddedContent_EveryCategoryWithContentCanBeLeveledUp()
    {
        foreach (var category in ContentRepository.LoadEmbedded().Categories.Where(c => c.HasContent))
        {
            var difficulties = category.AllLessons.SelectMany(l => l.Questions ?? []).Select(q => q.Difficulty).ToHashSet();
            Assert.True(difficulties.SetEquals([1, 2, 3]), $"{category.Id} doit avoir des questions de difficulté 1, 2 et 3.");
        }
    }

    [Fact]
    public void EmbeddedContent_EveryCategoryHasAFullCommonTrunk_AndATree()
    {
        foreach (var category in ContentRepository.LoadEmbedded().Categories)
        {
            Assert.True(category.Chapters.Count >= 4, $"{category.Id} : tronc commun trop court.");
            Assert.All(category.Chapters, c => Assert.NotEmpty(c.Lessons));
            Assert.NotNull(category.Specializations);
        }
    }

    [Fact]
    public void EmbeddedContent_Cpp_UsesEveryQuestionType_AndHasChallenges()
    {
        var cpp = ContentRepository.LoadEmbedded().GetCategory("cpp");
        var questions = cpp.AllLessons.SelectMany(l => l.Questions ?? [])
            .Concat(cpp.Chapters.Where(c => c.Challenge is not null).SelectMany(c => c.Challenge!.Questions ?? []))
            .ToList();

        Assert.Contains(questions, q => q is ChoiceQuestion);
        Assert.Contains(questions, q => q is MultiChoiceQuestion);
        Assert.Contains(questions, q => q is BugQuestion);
        Assert.Contains(questions, q => q is FillQuestion);
        Assert.Contains(questions, q => q is OrderQuestion);
        Assert.True(cpp.Chapters.Count(c => c.Challenge is not null) >= 2);
    }

    [Fact]
    public void EmbeddedContent_FolderCategory_LoadsChaptersInFileOrder()
    {
        var db = ContentRepository.LoadEmbedded().GetCategory("bases-de-donnees");

        Assert.Equal("sql", db.Language);
        Assert.Equal(
            ["db-ch-sql", "db-ch-panorama", "db-ch-jointures", "db-ch-modelisation",
             "db-ch-index", "db-ch-transactions", "db-ch-sql-avance", "db-ch-echelle"],
            db.Chapters.Select(c => c.Id));
        Assert.All(db.Chapters, c => Assert.NotNull(c.Challenge));
    }

    [Fact]
    public void Validator_RejectsInvalidAnswerIndex()
    {
        var bad = Sample.Category("bad", Sample.Chapter("ch", null, Sample.Lesson("l", Sample.Question("q", answerIndex: 7))));

        var ex = Assert.Throws<InvalidDataException>(() => new ContentRepository([bad]));
        Assert.Contains("answerIndex", ex.Message);
    }

    [Fact]
    public void Validator_RejectsDuplicateIds()
    {
        var bad = Sample.Category("bad", Sample.Chapter("ch", null, Sample.Lesson("l", Sample.Question("q"), Sample.Question("q"))));

        Assert.Throws<InvalidDataException>(() => new ContentRepository([bad]));
    }

    [Fact]
    public void Validator_RejectsEmptyChapter()
    {
        var bad = Sample.Category("bad", Sample.Chapter("ch", null));

        var ex = Assert.Throws<InvalidDataException>(() => new ContentRepository([bad]));
        Assert.Contains("aucune leçon", ex.Message);
    }

    [Fact]
    public async Task JsonFileStore_RoundTripsProgress()
    {
        var path = Path.Combine(Path.GetTempPath(), $"quizzware-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonFileProgressStore(path);
            var progress = new UserProgress { CurrentStreak = 3, LastActivityDate = new DateOnly(2026, 9, 29) };
            progress.GetOrCreate("reseau").CompletedLessons.Add("reseau-osi");
            progress.GetOrCreate("reseau").TotalXp = 180;

            await store.SaveAsync(progress);
            var loaded = await store.LoadAsync();

            Assert.Equal(3, loaded.CurrentStreak);
            Assert.Equal(new DateOnly(2026, 9, 29), loaded.LastActivityDate);
            Assert.Equal(180, loaded.Categories["reseau"].TotalXp);
            Assert.Contains("reseau-osi", loaded.Categories["reseau"].CompletedLessons);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task JsonFileStore_CorruptedFile_StartsFresh()
    {
        var path = Path.Combine(Path.GetTempPath(), $"quizzware-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{ pas du json");

            var loaded = await new JsonFileProgressStore(path).LoadAsync();

            Assert.Empty(loaded.Categories);
            Assert.True(File.Exists(path + ".corrupted"));
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + ".corrupted");
        }
    }
}
