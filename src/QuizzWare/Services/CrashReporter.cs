using Microsoft.Extensions.Logging;
using QuizzWare.Core.Diagnostics;

namespace QuizzWare.Services;

/// <summary>
/// Remontée des plantages et des erreurs inattendues, sans service tiers ni collecte automatique :
/// <list type="number">
/// <item>les plantages et les erreurs récupérées sont enregistrés sur l'appareil (<see cref="ErrorLog"/>) ;</item>
/// <item>au lancement suivant un plantage, l'utilisateur se voit proposer d'envoyer le rapport ;</item>
/// <item>l'envoi ouvre une issue GitHub pré-remplie, qu'il relit avant de la publier.</item>
/// </list>
/// « Signaler un problème » (Réglages) utilise le même chemin, avec les erreurs récentes.
/// </summary>
public sealed class CrashReporter(ErrorLog log, IDeviceDescriber device, IBrowserLauncher browser, ILogger<CrashReporter> logger)
{
    private bool _installed;

    /// <summary>Branche les gestionnaires d'exceptions non interceptées : à appeler une fois, au démarrage.</summary>
    public void Install()
    {
        if (_installed)
            return;
        _installed = true;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                Record(ex, isCrash: true);
        };

        // Une tâche en échec jamais attendue : l'application continue, mais c'est un bug.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Record(e.Exception, isCrash: false);
            e.SetObserved();
        };

#if ANDROID
        // Sur Android, une exception levée depuis le code Java (boucle d'événements) arrive ici en premier.
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (_, e) => Record(e.Exception, isCrash: true);
#elif IOS
        ObjCRuntime.Runtime.MarshalManagedException += (_, e) => Record(e.Exception, isCrash: true);
#endif
    }

    /// <summary>Enregistre une erreur ; à appeler aussi pour une erreur récupérée qui ne devrait pas arriver.</summary>
    public void Record(Exception exception, bool isCrash = false, string? context = null)
    {
        try
        {
            log.Append(ErrorReport.From(exception, isCrash, context ?? CurrentScreen(), device.Describe(), DateTimeOffset.UtcNow));
        }
        catch (Exception)
        {
            // Jamais d'exception depuis le gestionnaire de plantage.
        }
        logger.LogError(exception, isCrash ? "Plantage" : "Erreur inattendue");
    }

    public IReadOnlyList<ErrorReport> PendingCrashes() => log.PendingCrashes();

    /// <summary>Erreurs enregistrées ces 7 derniers jours, jointes à « Signaler un problème ».</summary>
    public IReadOnlyList<ErrorReport> RecentErrors() =>
        log.All().Where(r => r.Time > DateTimeOffset.UtcNow.AddDays(-7)).ToList();

    /// <summary>Après un plantage, propose d'envoyer le rapport (une seule fois par plantage).</summary>
    public async Task OfferPendingReportAsync(Func<string, string, string, string, Task<bool>> ask)
    {
        var pending = PendingCrashes();
        if (pending.Count == 0)
            return;

        var send = await ask(
            "QuizzWare s'est fermé",
            "L'application s'est fermée de façon inattendue lors de ta dernière utilisation. " +
            "Veux-tu envoyer le rapport ? Il s'ouvre dans ton navigateur, tu le relis avant de le publier.",
            "Envoyer", "Non merci");

        log.MarkHandled(pending.Select(r => r.Id));
        if (send)
            await OpenAsync(pending);
    }

    /// <summary>« Signaler un problème » : issue pré-remplie avec l'appareil et les erreurs récentes.</summary>
    public Task ReportProblemAsync() => OpenAsync(RecentErrors());

    private async Task OpenAsync(IReadOnlyList<ErrorReport> reports)
    {
        try
        {
            await browser.OpenAsync(IssueBuilder.Problem(reports, device.Describe()));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ouverture du rapport impossible");
        }
    }

    private static string? CurrentScreen()
    {
        try
        {
            return Shell.Current?.CurrentState?.Location?.OriginalString;
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>Décrit l'appareil (version de l'application, système, modèle) ; remplaçable dans les tests.</summary>
public interface IDeviceDescriber
{
    DeviceDescription Describe();
}

public sealed class MauiDeviceDescriber : IDeviceDescriber
{
    public DeviceDescription Describe()
    {
        try
        {
            return new DeviceDescription(
                $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})",
                DeviceInfo.Current.Platform.ToString(),
                DeviceInfo.Current.VersionString,
                $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}");
        }
        catch (Exception)
        {
            return DeviceDescription.Unknown;
        }
    }
}

/// <summary>Ouvre une adresse dans le navigateur ; remplaçable dans les tests.</summary>
public interface IBrowserLauncher
{
    Task OpenAsync(Uri uri);
}

public sealed class MauiBrowserLauncher : IBrowserLauncher
{
    public Task OpenAsync(Uri uri) => Launcher.Default.OpenAsync(uri);
}
