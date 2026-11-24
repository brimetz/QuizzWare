namespace QuizzWare.Core.Premium;

/// <summary>
/// Droit Premium gardé sur l'appareil : la date de la dernière vérification réussie auprès du store, pas un simple « oui ».
/// Hors ligne, il reste valable <see cref="OfflineGrace"/>, puis il faut revérifier : un abonnement résilié ou
/// une valeur recopiée d'une sauvegarde ne débloque rien durablement.
/// </summary>
public sealed record PremiumEntitlement(string ProductId, DateTimeOffset VerifiedAt)
{
    /// <summary>Durée pendant laquelle Premium reste actif sans pouvoir joindre le store.</summary>
    public static readonly TimeSpan OfflineGrace = TimeSpan.FromDays(3);

    /// <summary>Écart d'horloge toléré : une vérification « dans le futur » trahit une horloge reculée.</summary>
    public static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    public DateTimeOffset ValidUntil => VerifiedAt + OfflineGrace;

    public bool IsValidAt(DateTimeOffset now) =>
        ProductId == PremiumPolicy.SubscriptionProductId
        && now >= VerifiedAt - ClockTolerance
        && now < ValidUntil;

    public static PremiumEntitlement VerifiedNow(DateTimeOffset now) => new(PremiumPolicy.SubscriptionProductId, now);
}
