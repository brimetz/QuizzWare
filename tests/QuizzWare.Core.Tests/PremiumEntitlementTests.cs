using System.Security.Cryptography;
using System.Text;
using QuizzWare.Core.Premium;

namespace QuizzWare.Core.Tests;

public class PremiumEntitlementTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Entitlement_LastsForTheOfflineGrace_ThenExpires()
    {
        var entitlement = PremiumEntitlement.VerifiedNow(Now);

        Assert.True(entitlement.IsValidAt(Now));
        Assert.True(entitlement.IsValidAt(Now + PremiumEntitlement.OfflineGrace - TimeSpan.FromMinutes(1)));
        Assert.False(entitlement.IsValidAt(Now + PremiumEntitlement.OfflineGrace));
    }

    [Fact]
    public void Entitlement_IsRefused_WhenTheClockWasTurnedBack()
    {
        var entitlement = PremiumEntitlement.VerifiedNow(Now);

        Assert.True(entitlement.IsValidAt(Now - TimeSpan.FromMinutes(1)));
        Assert.False(entitlement.IsValidAt(Now - TimeSpan.FromDays(1)));
    }

    [Fact]
    public void Entitlement_ForAnotherProduct_IsRefused() =>
        Assert.False(new PremiumEntitlement("autre_produit", Now).IsValidAt(Now));
}

public class PlayPurchaseVerifierTests
{
    private const string Package = "com.quizzware.app";
    private readonly RSA _googleKey = RSA.Create(2048);

    private string PublicKey => Convert.ToBase64String(_googleKey.ExportSubjectPublicKeyInfo());

    private static string Purchase(string package = Package, string product = PremiumPolicy.SubscriptionProductId, int state = 0) =>
        $$"""{"orderId":"GPA.1234","packageName":"{{package}}","productId":"{{product}}","purchaseTime":1764000000000,"purchaseState":{{state}},"purchaseToken":"abc","autoRenewing":true,"acknowledged":false}""";

    private string Sign(string json, RSA? key = null) =>
        Convert.ToBase64String((key ?? _googleKey).SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));

    private bool Verify(string json, string signature) =>
        PlayPurchaseVerifier.Verify(json, signature, PublicKey, Package, PremiumPolicy.SubscriptionProductId);

    [Fact]
    public void GenuinePurchase_IsAccepted()
    {
        var json = Purchase();
        Assert.True(Verify(json, Sign(json)));
    }

    [Fact]
    public void ProductIdsArray_IsAccepted()
    {
        var json = $$"""{"packageName":"{{Package}}","productIds":["{{PremiumPolicy.SubscriptionProductId}}"],"purchaseState":0}""";
        Assert.True(Verify(json, Sign(json)));
    }

    [Fact]
    public void TamperedPurchase_IsRefused()
    {
        var signature = Sign(Purchase(product: "petit_achat"));
        Assert.False(Verify(Purchase(), signature));
    }

    [Fact]
    public void PurchaseSignedWithAnotherKey_IsRefused()
    {
        using var forger = RSA.Create(2048);
        var json = Purchase();
        Assert.False(Verify(json, Sign(json, forger)));
    }

    [Theory]
    [InlineData("com.autre.app", PremiumPolicy.SubscriptionProductId, 0)]
    [InlineData(Package, "autre_produit", 0)]
    [InlineData(Package, PremiumPolicy.SubscriptionProductId, 4)] // paiement en attente
    public void SignedPurchase_OfAnotherAppProductOrPending_IsRefused(string package, string product, int state)
    {
        var json = Purchase(package, product, state);
        Assert.False(Verify(json, Sign(json)));
    }

    [Theory]
    [InlineData(null, "c2ln")]
    [InlineData("{}", null)]
    [InlineData("{}", "pas du base64 !")]
    public void MissingOrMalformedData_IsRefused(string? json, string? signature) =>
        Assert.False(PlayPurchaseVerifier.Verify(json, signature, PublicKey, Package, PremiumPolicy.SubscriptionProductId));

    [Fact]
    public void WithoutPublicKey_EverythingIsRefused()
    {
        var json = Purchase();
        Assert.False(PlayPurchaseVerifier.Verify(json, Sign(json), "", Package, PremiumPolicy.SubscriptionProductId));
    }
}
