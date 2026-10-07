using QuizzWare.Core.Content;
using QuizzWare.Core.Premium;
using QuizzWare.Core.Progress;

namespace QuizzWare.Core.Tests;

public class PremiumTests
{
    private readonly InMemoryProgressStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly ProgressService _service;

    public PremiumTests()
    {
        _service = new ProgressService(_store, Sample.Repository(), _time);
    }

    [Fact]
    public async Task FreeInterviews_AreLimitedPerRollingWeek()
    {
        for (var i = 0; i < PremiumPolicy.FreeInterviewsPerWeek; i++)
        {
            var before = await _service.GetProgressAsync();
            Assert.True(PremiumPolicy.CanStartInterview(before, _time.Now, isPremium: false));
            await _service.RecordInterviewStartAsync();
            _time.AdvanceDays(1);
        }

        var progress = await _service.GetProgressAsync();
        Assert.Equal(0, PremiumPolicy.FreeInterviewsLeft(progress, _time.Now));
        Assert.False(PremiumPolicy.CanStartInterview(progress, _time.Now, isPremium: false));
        Assert.True(PremiumPolicy.CanStartInterview(progress, _time.Now, isPremium: true));

        // Le plus ancien entretien (jour 0) sort de la fenêtre au jour 7.
        Assert.Equal(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero), PremiumPolicy.NextFreeInterviewAt(progress, _time.Now));
        _time.Now = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(1, PremiumPolicy.FreeInterviewsLeft(progress, _time.Now));
        Assert.Null(PremiumPolicy.NextFreeInterviewAt(progress, _time.Now));
    }

    [Fact]
    public async Task Reset_KeepsInterviewHistory()
    {
        await _service.RecordInterviewStartAsync();
        await _service.ResetAsync();

        var progress = await _service.GetProgressAsync();
        Assert.Equal(PremiumPolicy.FreeInterviewsPerWeek - 1, PremiumPolicy.FreeInterviewsLeft(progress, _time.Now));
    }

    [Fact]
    public async Task InterviewHistory_KeepsOnlyTheLastMonth()
    {
        await _service.RecordInterviewStartAsync();
        _time.AdvanceDays(40);
        await _service.RecordInterviewStartAsync();

        Assert.Single((await _service.GetProgressAsync()).InterviewStarts);
    }

    [Fact]
    public void Categories_LanguagesAndBasicsAreFree_OthersArePremium()
    {
        Assert.True(PremiumPolicy.CanAccessCategory("csharp", isPremium: false));
        Assert.True(PremiumPolicy.CanAccessCategory("git", isPremium: false));
        Assert.False(PremiumPolicy.CanAccessCategory("architecture-systeme", isPremium: false));
        Assert.True(PremiumPolicy.CanAccessCategory("architecture-systeme", isPremium: true));
    }

    [Fact]
    public void FreeCategories_AllExistInTheContent()
    {
        var ids = ContentRepository.LoadEmbedded().Categories.Select(c => c.Id).ToHashSet();
        Assert.All(PremiumPolicy.LanguageCategories.Concat(PremiumPolicy.BasicCategories), id => Assert.Contains(id, ids));
    }
}
