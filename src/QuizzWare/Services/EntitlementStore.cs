using System.Text.Json;
using QuizzWare.Core.Premium;

namespace QuizzWare.Services;

/// <summary>Où l'appareil garde le droit Premium entre deux lancements ; remplaçable dans les tests.</summary>
public interface IEntitlementStore
{
    Task<PremiumEntitlement?> LoadAsync();

    /// <summary>null efface le droit.</summary>
    Task SaveAsync(PremiumEntitlement? entitlement);
}

/// <summary>
/// Droit Premium chiffré par le système : Keystore sur Android, trousseau (Keychain) sur iOS.
/// Sur Android, ce fichier est exclu des sauvegardes (Platforms/Android/Resources/xml) : il ne peut être ni modifié
/// dans une sauvegarde puis restauré, ni recopié sur un autre appareil.
/// </summary>
public sealed class SecureEntitlementStore(ISecureStorage storage) : IEntitlementStore
{
    private const string Key = "premium.entitlement";

    public async Task<PremiumEntitlement?> LoadAsync()
    {
        try
        {
            var json = await storage.GetAsync(Key);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PremiumEntitlement>(json);
        }
        catch (Exception)
        {
            // Illisible (clé du Keystore perdue, valeur altérée) : aucun droit, il sera revérifié auprès du store.
            storage.Remove(Key);
            return null;
        }
    }

    public async Task SaveAsync(PremiumEntitlement? entitlement)
    {
        if (entitlement is null)
            storage.Remove(Key);
        else
            await storage.SetAsync(Key, JsonSerializer.Serialize(entitlement));
    }
}
