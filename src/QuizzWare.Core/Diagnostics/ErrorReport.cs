using System.Text;
using System.Text.RegularExpressions;

namespace QuizzWare.Core.Diagnostics;

/// <summary>Appareil et version de l'application, joints à chaque rapport.</summary>
public sealed record DeviceDescription(string AppVersion, string Platform, string OsVersion, string Model)
{
    public static DeviceDescription Unknown { get; } = new("?", "?", "?", "?");
}

/// <summary>
/// Un plantage ou une erreur inattendue, enregistré sur l'appareil. Il ne quitte le téléphone que si l'utilisateur
/// décide de l'envoyer (issue GitHub pré-remplie, qu'il relit avant de la publier).
/// </summary>
public sealed record ErrorReport
{
    public const int MaxStackLines = 25;

    public required string Id { get; init; }
    public required DateTimeOffset Time { get; init; }
    /// <summary>Plantage (l'application s'est fermée) ou erreur récupérée.</summary>
    public required bool IsCrash { get; init; }
    /// <summary>Où l'erreur s'est produite : page affichée, action en cours.</summary>
    public string? Context { get; init; }
    public required string ExceptionType { get; init; }
    public required string Message { get; init; }
    public required string StackTrace { get; init; }
    public required DeviceDescription Device { get; init; }
    /// <summary>L'utilisateur a envoyé ce rapport, ou a choisi de ne pas l'envoyer.</summary>
    public bool Handled { get; init; }

    public static ErrorReport From(Exception exception, bool isCrash, string? context, DeviceDescription device, DateTimeOffset now)
    {
        // On remonte à l'erreur d'origine : les exceptions d'enrobage (agrégats, réflexion) n'apprennent rien.
        var root = exception;
        while (root is AggregateException { InnerExceptions.Count: 1 } or System.Reflection.TargetInvocationException && root.InnerException is not null)
            root = root.InnerException!;

        var stack = new StringBuilder();
        for (var e = root; e is not null; e = e.InnerException)
        {
            if (e != root)
                stack.AppendLine($"--- causée par {e.GetType().FullName} : {Scrub(e.Message)}");
            foreach (var line in (e.StackTrace ?? string.Empty).Split('\n').Select(l => l.TrimEnd()).Where(l => l.Length > 0).Take(MaxStackLines))
                stack.AppendLine(Scrub(line));
        }

        return new ErrorReport
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Time = now,
            IsCrash = isCrash,
            Context = context is null ? null : Scrub(context),
            ExceptionType = root.GetType().FullName ?? root.GetType().Name,
            Message = Scrub(root.Message),
            StackTrace = stack.ToString().TrimEnd(),
            Device = device,
        };
    }

    private static readonly Regex[] PersonalData =
    [
        new(@"(?i)([A-Z]:\\Users\\)[^\\\s]+", RegexOptions.Compiled),     // C:\Users\<nom>
        new(@"(/(?:Users|home)/)[^/\s]+", RegexOptions.Compiled),          // /Users/<nom>, /home/<nom>
        new(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.Compiled),            // adresses e-mail
    ];

    /// <summary>Retire ce qui pourrait identifier l'utilisateur : nom de session dans les chemins, adresses e-mail.</summary>
    public static string Scrub(string text)
    {
        text = PersonalData[0].Replace(text, "$1…");
        text = PersonalData[1].Replace(text, "$1…");
        return PersonalData[2].Replace(text, "…@…");
    }
}
