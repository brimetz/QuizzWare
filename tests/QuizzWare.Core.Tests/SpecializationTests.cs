using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;

namespace QuizzWare.Core.Tests;

public class SpecializationTests
{
    private readonly InMemoryProgressStore _store = new();
    private readonly ContentRepository _content = Sample.RepositoryWithTree();
    private readonly ProgressService _service;

    public SpecializationTests()
    {
        _service = new ProgressService(_store, _content, new FakeTimeProvider(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero)));
    }

    private Category Cat => _content.GetCategory("cat");

    private Task Pass(string unitId) => _service.ApplyResultAsync(new QuizResult("cat", unitId, 1, 1, 10));

    [Fact]
    public void Repository_FindsTrackUnits_AndTheirLanguage()
    {
        Assert.IsType<Lesson>(_content.GetUnit("cat", "s1"));
        Assert.IsType<Challenge>(_content.GetUnit("cat", "sd"));
        Assert.Equal("tech", _content.FindSpecialization("cat", "s2")!.Id);
        Assert.Null(_content.FindSpecialization("cat", "l1"));
        Assert.Equal("javascript", _content.GetCodeLanguage("cat", "s1"));
        Assert.Null(_content.GetCodeLanguage("cat", "l1"));
    }

    [Fact]
    public void MainLessons_ExcludeTracks_AllLessonsIncludeThem()
    {
        Assert.DoesNotContain(Cat.MainLessons, l => l.Id == "s1");
        Assert.Contains(Cat.AllLessons, l => l.Id == "s1");
    }

    [Fact]
    public async Task Track_StaysLocked_UntilTheUnlockingChapterIsDone()
    {
        var progress = await _service.GetProgressAsync();
        Assert.False(_service.AreSpecializationsUnlocked(progress, Cat));
        Assert.Equal(UnitState.Locked, _service.GetLessonState(progress, Cat, "s1"));

        await Pass("l1");
        await Pass("l2");

        Assert.True(_service.AreSpecializationsUnlocked(progress, Cat));
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "s1"));
        Assert.Equal(UnitState.Locked, _service.GetLessonState(progress, Cat, "s2"));
    }

    [Fact]
    public async Task Track_HasItsOwnOrder_IndependentOfTheMainPath()
    {
        await Pass("l1");
        await Pass("l2");
        await Pass("s1");
        var progress = await _service.GetProgressAsync();

        // s2 suit s1 dans son parcours, même si l3 (tronc commun) n'est pas fait
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "s2"));
        Assert.Equal(UnitState.Available, _service.GetLessonState(progress, Cat, "l3"));
    }

    [Fact]
    public async Task NodeProgress_AggregatesDescendants()
    {
        await Pass("l1");
        await Pass("l2");
        await Pass("s1");
        var progress = await _service.GetProgressAsync();
        var family = Cat.Specializations!.Nodes[0];

        Assert.Equal((1, 2), _service.GetNodeProgress(progress, Cat, family));
        Assert.Equal((0, 0), _service.GetNodeProgress(progress, Cat, family.ChildNodes[1]));
    }

    [Fact]
    public async Task UnlockedLessons_IncludeOpenedTracks()
    {
        var progress = await _service.GetProgressAsync();
        Assert.DoesNotContain(_service.GetUnlockedLessons(progress, Cat), l => l.Id == "s1");

        await Pass("l1");
        await Pass("l2");
        Assert.Contains(_service.GetUnlockedLessons(progress, Cat), l => l.Id == "s1");
    }

    [Fact]
    public void Validator_RejectsBadTree()
    {
        var category = Sample.Repository().GetCategory("cat") with
        {
            Specializations = new SpecializationTree("inconnu",
            [
                new SpecializationNode("fam", "Famille", "", "F", SpecializationKind.Family),
                new SpecializationNode("x", "X", "", "X", "planete"),
            ]),
        };

        var ex = Assert.Throws<InvalidDataException>(() => new ContentRepository([category]));
        Assert.Contains("unlockedBy", ex.Message);
        Assert.Contains("au moins une spécialisation", ex.Message);
        Assert.Contains("planete", ex.Message);
    }

    [Fact]
    public void Validator_RejectsBrokenLinks()
    {
        Category WithLeaf(SpecializationNode leaf) => Sample.Repository().GetCategory("cat") with
        {
            Specializations = new SpecializationTree(null,
                [new SpecializationNode("fam", "Famille", "", "F", SpecializationKind.Family, [leaf])]),
        };

        var ex = Assert.Throws<InvalidDataException>(() =>
            new ContentRepository([WithLeaf(new SpecializationNode("l", "L", "", "L", SpecializationKind.Library, LinkTo: "cat/inexistant"))]));
        Assert.Contains("aucune spécialisation", ex.Message);

        ex = Assert.Throws<InvalidDataException>(() =>
            new ContentRepository([WithLeaf(new SpecializationNode("l", "L", "", "L", SpecializationKind.Library, LinkTo: "sans-slash"))]));
        Assert.Contains("invalide", ex.Message);
    }

    [Fact]
    public void EmbeddedTrees_EveryCategoryHasOne_AndLinksResolve()
    {
        var repository = ContentRepository.LoadEmbedded();

        Assert.All(repository.Categories, c => Assert.NotNull(c.Specializations));

        var csharp = repository.GetCategory("csharp");
        var efLink = csharp.Specializations!.AllNodes.Single(n => n.Id == "cs-spe-efcore");
        var (category, target) = repository.Resolve(csharp, efLink);
        Assert.Equal("bases-de-donnees", category.Id);
        Assert.Equal("db-spe-efcore", target.Id);
        Assert.True(target.HasTrack);
    }

    [Theory]
    [InlineData("csharp", "cs-spe-aspnet")]
    [InlineData("architecture-infra", "infra-spe-kubernetes")]
    [InlineData("python", "py-spe-fastapi")]
    [InlineData("bases-de-donnees", "db-spe-postgresql")]
    [InlineData("architecture-systeme", "sys-spe-demarche")]
    [InlineData("bases-de-donnees", "db-spe-mongodb")]
    [InlineData("bases-de-donnees", "db-spe-efcore")]
    public void EmbeddedTracks_AreCompleteWithChallenge(string categoryId, string nodeId)
    {
        var node = ContentRepository.LoadEmbedded().GetSpecialization(categoryId, nodeId);

        Assert.True(node.HasTrack);
        Assert.All(node.Track, chapter => Assert.NotNull(chapter.Challenge));
        Assert.Equal([1, 2, 3], node.Track.SelectMany(c => c.Lessons).SelectMany(l => l.Questions ?? []).Select(q => q.Difficulty).Distinct().Order());
    }

    [Fact]
    public void EmbeddedTrees_DoorsLeadToExistingCategories()
    {
        var repository = ContentRepository.LoadEmbedded();
        var doors = repository.Categories.SelectMany(c => c.Specializations!.AllNodes).Where(n => n.IsDoor).ToList();

        Assert.NotEmpty(doors);
        Assert.All(doors, d => Assert.NotNull(repository.GetCategory(d.LinkTo!)));
    }

    [Fact]
    public void EmbeddedDatabaseTree_IsLoadedWithTracks()
    {
        var db = ContentRepository.LoadEmbedded().GetCategory("bases-de-donnees");
        var tree = db.Specializations!;

        Assert.Equal("db-ch-panorama", tree.UnlockedBy);
        Assert.All(tree.Nodes, n => Assert.Equal(SpecializationKind.Family, n.Kind));

        var mongo = tree.AllNodes.Single(n => n.Id == "db-spe-mongodb");
        var efcore = tree.AllNodes.Single(n => n.Id == "db-spe-efcore");
        Assert.True(mongo.HasTrack);
        Assert.Equal(SpecializationKind.Library, efcore.Kind);
        Assert.Equal("csharp", efcore.RelatedCategory);
        Assert.NotNull(efcore.Track[0].Challenge);
    }
}
