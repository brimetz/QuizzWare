using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuizzWare.Core.Premium;

/// <summary>
/// Vérifie qu'un achat Google Play est authentique : Google signe les données de chaque achat (JSON) avec la clé
/// de l'application (Play Console › Monétisation › Configuration de la monétisation › Licences), en SHA1withRSA.
/// Un achat dont la signature ne correspond pas, ou qui concerne une autre application ou un autre produit, est refusé.
/// </summary>
public static class PlayPurchaseVerifier
{
    /// <summary>purchaseState dans le JSON signé : 0 = acheté (4 = en attente de paiement).</summary>
    private const int Purchased = 0;

    /// <param name="signedData">JSON de l'achat, tel que reçu (OriginalJson).</param>
    /// <param name="signature">Signature en base 64.</param>
    /// <param name="publicKeyBase64">Clé publique RSA de l'application (X.509, base 64), copiée depuis la Play Console.</param>
    public static bool Verify(string? signedData, string? signature, string publicKeyBase64, string packageName, string productId)
    {
        if (string.IsNullOrEmpty(signedData) || string.IsNullOrEmpty(signature) || string.IsNullOrWhiteSpace(publicKeyBase64))
            return false;

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64.Trim()), out _);
            if (!rsa.VerifyData(Encoding.UTF8.GetBytes(signedData), Convert.FromBase64String(signature),
                    HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
                return false;

            // Signature valide : on vérifie maintenant que l'achat signé est bien le nôtre.
            using var json = JsonDocument.Parse(signedData);
            var root = json.RootElement;
            return Text(root, "packageName") == packageName
                   && Products(root).Contains(productId)
                   && (!root.TryGetProperty("purchaseState", out var state) || state.GetInt32() == Purchased);
        }
        catch (Exception e) when (e is FormatException or CryptographicException or JsonException or InvalidOperationException)
        {
            return false;
        }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Selon la version de la bibliothèque de facturation : « productId » ou « productIds ».</summary>
    private static IReadOnlyList<string> Products(JsonElement root)
    {
        if (Text(root, "productId") is { } single)
            return [single];
        return root.TryGetProperty("productIds", out var many) && many.ValueKind == JsonValueKind.Array
            ? many.EnumerateArray().Where(p => p.ValueKind == JsonValueKind.String).Select(p => p.GetString()!).ToList()
            : [];
    }
}
