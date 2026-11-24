using QuizzWare.Services;

namespace QuizzWare.ViewModels;

/// <summary>Libellés des contenus Premium : « Premium » quand l'abonnement est proposé, « À venir » sinon.</summary>
public static class PremiumLabels
{
    public static string Badge(ISubscriptionService subscription) => subscription.IsOffered ? "⭐ Premium" : "🕓 À venir";

    /// <summary>Statut d'un renvoi vers une catégorie Premium.</summary>
    public static string CategoryStatus(ISubscriptionService subscription, string categoryName) => subscription.IsOffered
        ? $"⭐ « {categoryName} » est une catégorie Premium"
        : $"🕓 « {categoryName} » arrive bientôt";

    /// <summary>Store qui vend l'abonnement, pour les textes d'achat.</summary>
    public static string StoreName => DeviceInfo.Current.Platform == DevicePlatform.iOS ? "l'App Store" : "Google Play";
}
