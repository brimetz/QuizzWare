namespace QuizzWare.Services;

public enum PurchaseOutcome { Success, Cancelled, Pending, Unavailable, Error }

/// <summary>Abonnement Premium mensuel : état, achat et restauration.</summary>
public interface ISubscriptionService
{
    /// <summary>Dernier état connu (mis en cache : reste vrai hors ligne jusqu'à la prochaine vérification réussie).</summary>
    bool IsPremium { get; }

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

/// <summary>Garde le dernier état connu dans les préférences, pour un démarrage hors ligne.</summary>
public abstract class SubscriptionServiceBase : ISubscriptionService
{
    private const string PremiumKey = "premium.active";

    public bool IsPremium { get; private set; } = Preferences.Default.Get(PremiumKey, false);

    public abstract bool IsSimulated { get; }

    public event EventHandler? Changed;

    protected void SetPremium(bool value)
    {
        Preferences.Default.Set(PremiumKey, value);
        if (IsPremium == value)
            return;
        IsPremium = value;
        MainThread.BeginInvokeOnMainThread(() => Changed?.Invoke(this, EventArgs.Empty));
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
public sealed class SimulatedSubscriptionService : SubscriptionServiceBase
{
    public override bool IsSimulated => true;

    public override Task RefreshAsync() => Task.CompletedTask;

    public override Task<string?> GetPriceAsync() => Task.FromResult<string?>("4,99 € (simulé)");

    public override async Task<PurchaseOutcome> PurchaseAsync()
    {
        var ok = await Shell.Current.DisplayAlertAsync("Achat simulé",
            "Version de développement : aucun paiement n'est effectué. Activer Premium ?", "Activer", "Annuler");
        if (!ok)
            return PurchaseOutcome.Cancelled;
        SetPremium(true);
        return PurchaseOutcome.Success;
    }

    public override async Task OpenManagementAsync()
    {
        var stop = await Shell.Current.DisplayAlertAsync("Abonnement simulé",
            "Version de développement : résilier l'abonnement simulé ?", "Résilier", "Garder");
        if (stop)
            SetPremium(false);
    }
}
