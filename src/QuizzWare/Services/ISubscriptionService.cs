using QuizzWare.Core.Premium;

namespace QuizzWare.Services;

public enum PurchaseOutcome { Success, Cancelled, Pending, Unavailable, Error }

/// <summary>Abonnement Premium mensuel : état, achat et restauration.</summary>
public interface ISubscriptionService
{
    /// <summary>Dernier état connu (mis en cache : reste vrai hors ligne jusqu'à la prochaine vérification réussie).</summary>
    bool IsPremium { get; }

    /// <summary>Faux tant que l'abonnement n'est pas proposé : les contenus Premium sont affichés « À venir ».</summary>
    bool IsOffered { get; }

    /// <summary>Vrai pour la version de développement, qui simule l'abonnement sans passer par un store.</summary>
    bool IsSimulated { get; }

    event EventHandler? Changed;

    /// <summary>Relit l'état de l'abonnement auprès du store. En cas d'échec (hors ligne), garde le dernier état connu.</summary>
    Task RefreshAsync();

    /// <summary>Prix localisé du mois (« 4,99 € »), null s'il est indisponible.</summary>
    Task<string?> GetPriceAsync();

    Task<PurchaseOutcome> PurchaseAsync();

    /// <summary>Retrouve un abonnement déjà payé (nouveau téléphone, réinstallation). Renvoie l'état Premium obtenu.</summary>
    Task<bool> RestoreAsync();

    /// <summary>Page du store où l'utilisateur gère ou résilie son abonnement.</summary>
    Task OpenManagementAsync();
}

/// <summary>
/// Base des services d'abonnement. L'état Premium n'est jamais un simple booléen en clair : c'est un
/// <see cref="PremiumEntitlement"/> (date de la dernière vérification réussie auprès du store), chiffré dans
/// <see cref="IEntitlementStore"/>, qui expire seul après <see cref="PremiumEntitlement.OfflineGrace"/> sans nouvelle vérification.
/// </summary>
public abstract class SubscriptionServiceBase : ISubscriptionService
{
    /// <summary>Ancien stockage en clair (booléen dans les préférences) : effacé, jamais relu.</summary>
    private const string LegacyPremiumKey = "premium.active";

    private readonly IEntitlementStore _store;
    private readonly TimeProvider _time;
    private PremiumEntitlement? _entitlement;

    protected SubscriptionServiceBase(IEntitlementStore store, TimeProvider time, IPreferences preferences)
    {
        _store = store;
        _time = time;
        preferences.Remove(LegacyPremiumKey);
        // Lu une fois, au démarrage : la première page doit connaître l'état Premium. Task.Run évite tout interblocage.
        _entitlement = Task.Run(store.LoadAsync).GetAwaiter().GetResult();
    }

    public bool IsPremium => _entitlement?.IsValidAt(_time.GetUtcNow()) == true;

    public bool IsOffered => true;

    public abstract bool IsSimulated { get; }

    public event EventHandler? Changed;

    /// <summary>Un droit a été accordé (même expiré) et pas retiré : l'abonnement n'a pas été résilié.</summary>
    protected bool HasEntitlement => _entitlement is not null;

    /// <summary>Le store vient de confirmer un abonnement actif et authentique.</summary>
    protected Task GrantAsync() => SetAsync(PremiumEntitlement.VerifiedNow(_time.GetUtcNow()));

    /// <summary>Le store vient de confirmer qu'aucun abonnement n'est actif.</summary>
    protected Task RevokeAsync() => SetAsync(null);

    private async Task SetAsync(PremiumEntitlement? entitlement)
    {
        var was = IsPremium;
        _entitlement = entitlement;
        await _store.SaveAsync(entitlement);
        if (was != IsPremium && Changed is { } changed)
            MainThread.BeginInvokeOnMainThread(() => changed(this, EventArgs.Empty));
    }

    public abstract Task RefreshAsync();
    public abstract Task<string?> GetPriceAsync();
    public abstract Task<PurchaseOutcome> PurchaseAsync();

    public virtual async Task<bool> RestoreAsync()
    {
        await RefreshAsync();
        return IsPremium;
    }

    public abstract Task OpenManagementAsync();
}

/// <summary>Version de développement : l'abonnement est simulé, sans paiement.</summary>
public sealed class SimulatedSubscriptionService(IEntitlementStore store, TimeProvider time, IPreferences preferences)
    : SubscriptionServiceBase(store, time, preferences)
{
    public override bool IsSimulated => true;

    /// <summary>Le « store » simulé confirme l'abonnement tant qu'il n'a pas été résilié.</summary>
    public override Task RefreshAsync() => HasEntitlement ? GrantAsync() : Task.CompletedTask;

    public override Task<string?> GetPriceAsync() => Task.FromResult<string?>("4,99 € (simulé)");

    public override async Task<PurchaseOutcome> PurchaseAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync("Achat simulé",
            "Version de développement : aucun paiement n'est effectué. Activer Premium ?", "Activer", "Annuler");
        if (!ok)
            return PurchaseOutcome.Cancelled;
        await GrantAsync();
        return PurchaseOutcome.Success;
    }

    public override async Task OpenManagementAsync()
    {
        var stop = await Shell.Current.DisplayAlertAsync("Abonnement simulé",
            "Version de développement : résilier l'abonnement simulé ?", "Résilier", "Garder");
        if (stop)
            await RevokeAsync();
    }
}

/// <summary>
/// Abonnement pas encore proposé : rien n'est acheté ni lu dans les préférences, les contenus Premium restent verrouillés
/// et sont présentés « À venir ».
/// </summary>
public sealed class ComingSoonSubscriptionService : ISubscriptionService
{
    public bool IsPremium => false;
    public bool IsOffered => false;
    public bool IsSimulated => false;

    public event EventHandler? Changed { add { } remove { } }

    public Task RefreshAsync() => Task.CompletedTask;
    public Task<string?> GetPriceAsync() => Task.FromResult<string?>(null);
    public Task<PurchaseOutcome> PurchaseAsync() => Task.FromResult(PurchaseOutcome.Unavailable);
    public Task<bool> RestoreAsync() => Task.FromResult(false);
    public Task OpenManagementAsync() => Task.CompletedTask;
}
