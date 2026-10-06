using System.Text;
using Microsoft.Extensions.Logging;

namespace QuizzWare.Services;

/// <summary>
/// « Signaler une erreur » : ouvre une issue GitHub pré-remplie sur le dépôt du contenu.
/// Pas de serveur à maintenir, et le public (des développeurs) a presque toujours un compte GitHub.
/// </summary>
public sealed class ContentReporter(ILogger<ContentReporter> logger)
{
    public const string IssuesUrl = "https://github.com/brimetz/QuizzWare/issues/new";

    /// <param name="subject">Ce qui est signalé : « question cpp-types-q1 », « leçon cpp-types »…</param>
    /// <param name="context">Lignes de contexte ajoutées au corps (énoncé, catégorie…).</param>
    public static Uri BuildUri(string subject, IEnumerable<string> context)
    {
        var body = new StringBuilder()
            .AppendLine("**Qu'est-ce qui ne va pas ?**")
            .AppendLine()
            .AppendLine("<!-- Réponse fausse, explication incomplète, code qui ne compile pas, faute… -->")
            .AppendLine()
            .AppendLine("---");
        foreach (var line in context)
            body.AppendLine(line);
        body.AppendLine($"- Version : {AppInfo.Current.VersionString} ({DeviceInfo.Current.Platform})");

        var query = $"title={Uri.EscapeDataString($"[Contenu] {subject}")}" +
                    $"&labels={Uri.EscapeDataString("contenu")}" +
                    $"&body={Uri.EscapeDataString(body.ToString())}";
        return new Uri($"{IssuesUrl}?{query}");
    }

    public async Task ReportAsync(string subject, IEnumerable<string> context)
    {
        try
        {
            await Launcher.Default.OpenAsync(BuildUri(subject, context));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ouverture du signalement impossible");
            await Shell.Current.DisplayAlert("Signaler une erreur",
                $"Impossible d'ouvrir le navigateur. Tu peux signaler « {subject} » sur github.com/brimetz/QuizzWare/issues.", "OK");
        }
    }
}
