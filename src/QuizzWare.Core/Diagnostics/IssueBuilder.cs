using System.Text;

namespace QuizzWare.Core.Diagnostics;

/// <summary>
/// Construit les issues GitHub pré-remplies : l'utilisateur relit le texte dans son navigateur avant de le publier.
/// Pas de serveur ni de service tiers, et aucune donnée n'est envoyée sans son action.
/// </summary>
public static class IssueBuilder
{
    public const string IssuesUrl = "https://github.com/brimetz/QuizzWare/issues/new";

    /// <summary>Longueur maximale du corps encodé : au-delà, les navigateurs et GitHub tronquent ou refusent l'URL.</summary>
    public const int MaxEncodedBodyLength = 6000;

    /// <summary>Signalement d'un plantage, ou d'un problème décrit par l'utilisateur (<paramref name="reports"/> peut être vide).</summary>
    public static Uri Problem(IReadOnlyList<ErrorReport> reports, DeviceDescription device)
    {
        var title = reports.FirstOrDefault(r => r.IsCrash) is { } crash
            ? $"[Plantage] {ShortType(crash.ExceptionType)} : {Truncate(crash.Message, 80)}"
            : "[Problème] ";

        var intro = new StringBuilder()
            .AppendLine("**Que faisais-tu au moment du problème ?**")
            .AppendLine()
            .AppendLine("<!-- Par exemple : « je validais une réponse dans un quiz C++ ». Ce texte et le rapport ci-dessous seront publics : relis-les avant d'envoyer. -->")
            .AppendLine()
            .AppendLine("---")
            .AppendLine($"- Version : {device.AppVersion} · {device.Platform} {device.OsVersion} · {device.Model}");

        // On ajoute les rapports du plus récent au plus ancien, tant que l'URL reste raisonnable.
        var body = intro.ToString();
        foreach (var report in reports.OrderByDescending(r => r.Time))
        {
            var candidate = body + Format(report);
            if (Uri.EscapeDataString(candidate).Length > MaxEncodedBodyLength)
            {
                var shortened = body + Format(report with { StackTrace = FirstLines(report.StackTrace, 8) });
                if (Uri.EscapeDataString(shortened).Length <= MaxEncodedBodyLength)
                    body = shortened;
                break;
            }
            body = candidate;
        }

        var labels = reports.Any(r => r.IsCrash) ? "plantage" : "bug";
        return new Uri($"{IssuesUrl}?title={Uri.EscapeDataString(title)}&labels={labels}&body={Uri.EscapeDataString(body)}");
    }

    public static string Format(ErrorReport report) => new StringBuilder()
        .AppendLine()
        .AppendLine($"### {(report.IsCrash ? "Plantage" : "Erreur")} du {report.Time:yyyy-MM-dd HH:mm} UTC (réf. {report.Id})")
        .AppendLine(report.Context is null ? string.Empty : $"Écran : {report.Context}")
        .AppendLine($"`{report.ExceptionType}` : {report.Message}")
        .AppendLine("```")
        .AppendLine(report.StackTrace)
        .AppendLine("```")
        .ToString();

    private static string ShortType(string type) => type[(type.LastIndexOf('.') + 1)..];

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private static string FirstLines(string text, int count) => string.Join('\n', text.Split('\n').Take(count)) + "\n…";
}
