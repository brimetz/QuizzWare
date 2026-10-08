using QuizzWare.Core.Diagnostics;
using QuizzWare.Services;

namespace QuizzWare.App.Tests;

public class CrashReporterTests : IDisposable
{
    private readonly TempFile _file = new();
    private readonly FakeBrowser _browser = new();
    private readonly ErrorLog _log;
    private readonly CrashReporter _reporter;

    public CrashReporterTests()
    {
        _log = new ErrorLog(_file.Path);
        _reporter = new CrashReporter(_log, new FakeDevice(), _browser, new NullLogger<CrashReporter>());
    }

    public void Dispose() => _file.Dispose();

    private static Exception Thrown()
    {
        try
        {
            throw new InvalidOperationException(@"Fichier C:\Users\zoe\progress.json introuvable");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    [Fact]
    public async Task Crash_IsOfferedOnce_AndOpensAPrefilledIssue()
    {
        _reporter.Record(Thrown(), isCrash: true, context: "//QuizPage");
        var asked = 0;

        await _reporter.OfferPendingReportAsync((_, _, _, _) => { asked++; return Task.FromResult(true); });
        await _reporter.OfferPendingReportAsync((_, _, _, _) => { asked++; return Task.FromResult(true); });

        Assert.Equal(1, asked);
        var uri = Uri.UnescapeDataString(Assert.Single(_browser.Opened).ToString());
        Assert.Contains("[Plantage] InvalidOperationException", uri);
        Assert.Contains("Google Pixel 8", uri);
        Assert.Contains("//QuizPage", uri);
        Assert.DoesNotContain("zoe", uri); // le nom de session est retiré des chemins
    }

    [Fact]
    public async Task DeclinedCrash_IsNotOfferedAgain_NorSent()
    {
        _reporter.Record(Thrown(), isCrash: true);

        await _reporter.OfferPendingReportAsync((_, _, _, _) => Task.FromResult(false));

        Assert.Empty(_reporter.PendingCrashes());
        Assert.Empty(_browser.Opened);
    }

    [Fact]
    public async Task RecoveredErrors_AreNotOfferedAsCrashes_ButJoinTheProblemReport()
    {
        _reporter.Record(Thrown(), isCrash: false, context: "chargement de la leçon");

        await _reporter.OfferPendingReportAsync((_, _, _, _) => throw new Xunit.Sdk.XunitException("ne doit pas demander"));
        await _reporter.ReportProblemAsync();

        var uri = Uri.UnescapeDataString(Assert.Single(_browser.Opened).ToString());
        Assert.Contains("[Problème]", uri);
        Assert.Contains("chargement de la leçon", uri);
    }

    [Fact]
    public void Record_NeverThrows_EvenWhenTheLogCannotBeWritten()
    {
        var broken = new CrashReporter(new ErrorLog(Path.Combine(_file.Path, "\0invalide")), new FakeDevice(), _browser,
            new NullLogger<CrashReporter>());

        var error = Record.Exception(() => broken.Record(Thrown(), isCrash: true));

        Assert.Null(error);
    }
}
