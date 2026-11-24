using QuizzWare.Services;
using QuizzWare.ViewModels;

namespace QuizzWare.App.Tests;

/// <summary>Abonnement pas encore proposé : rien ne se débloque, et les contenus Premium sont annoncés « À venir ».</summary>
public class ComingSoonTests
{
    private readonly ComingSoonSubscriptionService _subscription = new();

    [Fact]
    public async Task NothingCanBeBoughtOrRestored()
    {
        Assert.False(_subscription.IsOffered);
        Assert.False(_subscription.IsPremium);
        Assert.Equal(PurchaseOutcome.Unavailable, await _subscription.PurchaseAsync());
        Assert.False(await _subscription.RestoreAsync());
        Assert.Null(await _subscription.GetPriceAsync());
        Assert.False(_subscription.IsPremium);
    }

    [Fact]
    public void LabelsSayComingSoonInsteadOfPremium()
    {
        Assert.Equal("🕓 À venir", PremiumLabels.Badge(_subscription));
        Assert.Equal("🕓 « Réseau » arrive bientôt", PremiumLabels.CategoryStatus(_subscription, "Réseau"));
        Assert.DoesNotContain("Premium", PremiumLabels.Badge(_subscription));
    }
}
