using QuizzWare.Core.Diagnostics;

namespace QuizzWare.Core.Tests;

public class DiagnosticsTests : IDisposable
{
    private static readonly DeviceDescription Device = new("1.2.0 (7)", "Android", "15", "Google Pixel 8");
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 9, 30, 0, TimeSpan.Zero);
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"quizzware-erreurs-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private static Exception Thrown(Exception ex)
    {
        try
        {
            throw ex;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    [Theory]
    [InlineData(@"C:\Users\zoe\AppData\progress.json", @"C:\Users\…\AppData\progress.json")]
    [InlineData("/Users/zoe/Library/progress.json", "/Users/…/Library/progress.json")]
    [InlineData("/home/zoe/.local/progress.json", "/home/…/.local/progress.json")]
    [InlineData("compte zoe.dupont@exemple.fr refusé", "compte …@… refusé")]
    public void Scrub_RemovesPersonalData(string input, string expected) => Assert.Equal(expected, ErrorReport.Scrub(input));

    [Fact]
    public void From_UnwrapsAggregates_AndKeepsTheCause()
    {
        var inner = Thrown(new FormatException("date invalide"));
        var wrapped = new AggregateException(new InvalidOperationException("chargement impossible", inner));

        var report = ErrorReport.From(wrapped, isCrash: true, "//LessonPage", Device, Now);

        Assert.Equal("System.InvalidOperationException", report.ExceptionType);
        Assert.Equal("chargement impossible", report.Message);
        Assert.Contains("causée par System.FormatException : date invalide", report.StackTrace);
        Assert.True(report.IsCrash);
        Assert.False(report.Handled);
    }

    [Fact]
    public void Log_KeepsTheMostRecentReports_AndSurvivesACorruptedFile()
    {
        var log = new ErrorLog(_path);
        for (var i = 0; i < ErrorLog.Capacity + 5; i++)
            log.Append(ErrorReport.From(new InvalidOperationException($"erreur {i}"), isCrash: i % 2 == 0, null, Device, Now.AddMinutes(i)));

        var all = log.All();
        Assert.Equal(ErrorLog.Capacity, all.Count);
        Assert.Equal("erreur 5", all[0].Message);

        log.MarkHandled(log.PendingCrashes().Select(r => r.Id));
        Assert.Empty(log.PendingCrashes());

        File.WriteAllText(_path, "{ corrompu");
        Assert.Empty(new ErrorLog(_path).All());
    }

    [Fact]
    public void Issue_StaysWithinUrlLimits_EvenWithManyLongReports()
    {
        var longStack = string.Join('\n', Enumerable.Range(0, 200).Select(i => $"   at QuizzWare.Quelque.Chose.Methode{i}() in Fichier.cs:line {i}"));
        var reports = Enumerable.Range(0, 10)
            .Select(i => ErrorReport.From(new InvalidOperationException($"erreur {i}"), isCrash: true, null, Device, Now.AddMinutes(i))
                with { StackTrace = longStack })
            .ToList();

        var uri = IssueBuilder.Problem(reports, Device);
        var body = uri.Query[(uri.Query.IndexOf("&body=", StringComparison.Ordinal) + 6)..];

        Assert.True(body.Length <= IssueBuilder.MaxEncodedBodyLength, $"corps de {body.Length} caractères");
        Assert.Contains("erreur 9", Uri.UnescapeDataString(body)); // le plus récent d'abord
        Assert.Contains("labels=plantage", uri.Query);
    }

    [Fact]
    public void Issue_WithoutErrors_IsAPlainProblemReport()
    {
        var uri = Uri.UnescapeDataString(IssueBuilder.Problem([], Device).ToString());

        Assert.Contains("[Problème]", uri);
        Assert.Contains("labels=bug", uri);
        Assert.Contains("1.2.0 (7) · Android 15 · Google Pixel 8", uri);
    }
}
