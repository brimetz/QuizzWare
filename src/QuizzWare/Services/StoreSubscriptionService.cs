#if ANDROID || IOS
using Plugin.InAppBilling;
using QuizzWare.Core.Premium;

namespace QuizzWare.Services;

/// <summary>
/// Abonnement via Google Play Billing (et StoreKit sur iOS). Pas de compte ni de serveur :
/// l'abonnement est rattaché au compte du store, qui indique s'il est actif.
/// </summary>
public sealed class StoreSubscriptionService : SubscriptionServiceBase
{
    private const string ProductId = PremiumPolicy.SubscriptionProductId;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public override bool IsSimulated => false;

    public override async Task RefreshAsync()
    {
        try
        {
            await WithBillingAsync(async billing =>
            {
                var purchases = await billing.GetPurchasesAsync(ItemType.Subscription);
                var active = purchases.Where(IsActive).ToList();
                await AcknowledgeAsync(billing, active);
                SetPremium(active.Count > 0);
            });
        }
        catch (Exception)
        {
            // Hors ligne ou store indisponible : on garde le dernier état connu.
        }
    }

    public override async Task<string?> GetPriceAsync()
    {
        try
        {
            string? price = null;
            await WithBillingAsync(async billing =>
            {
                var products = await billing.GetProductInfoAsync(ItemType.Subscription, [ProductId]);
                price = products.FirstOrDefault()?.LocalizedPrice;
            });
            return string.IsNullOrWhiteSpace(price) ? null : price;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override async Task<PurchaseOutcome> PurchaseAsync()
    {
        try
        {
            var outcome = PurchaseOutcome.Error;
            await WithBillingAsync(async billing =>
            {
                var purchase = await billing.PurchaseAsync(ProductId, ItemType.Subscription);
                if (purchase is null)
                {
                    outcome = PurchaseOutcome.Error;
                    return;
                }

                if (purchase.State == PurchaseState.PaymentPending)
                {
                    outcome = PurchaseOutcome.Pending;
                    return;
                }

                if (IsActive(purchase))
                {
                    // Google rembourse automatiquement un achat non acquitté sous 3 jours.
                    await AcknowledgeAsync(billing, [purchase]);
                    SetPremium(true);
                    outcome = PurchaseOutcome.Success;
                }
            });
            return outcome;
        }
        catch (InAppBillingPurchaseException e) when (e.PurchaseError == PurchaseError.UserCancelled)
        {
            return PurchaseOutcome.Cancelled;
        }
        catch (InAppBillingPurchaseException e) when (e.PurchaseError == PurchaseError.AlreadyOwned)
        {
            await RefreshAsync();
            return IsPremium ? PurchaseOutcome.Success : PurchaseOutcome.Error;
        }
        catch (InAppBillingPurchaseException e) when (e.PurchaseError is PurchaseError.BillingUnavailable
            or PurchaseError.AppStoreUnavailable or PurchaseError.ServiceUnavailable or PurchaseError.ItemUnavailable)
        {
            return PurchaseOutcome.Unavailable;
        }
        catch (Exception)
        {
            return PurchaseOutcome.Error;
        }
    }

    public override Task OpenManagementAsync()
    {
#if ANDROID
        var url = $"https://play.google.com/store/account/subscriptions?sku={ProductId}&package={AppInfo.PackageName}";
#else
        var url = "https://apps.apple.com/account/subscriptions";
#endif
        return Launcher.OpenAsync(url);
    }

    private static bool IsActive(InAppBillingPurchase p) =>
        p.ProductId == ProductId && p.State is PurchaseState.Purchased or PurchaseState.Restored;

    private static async Task AcknowledgeAsync(IInAppBilling billing, IEnumerable<InAppBillingPurchase> purchases)
    {
        var tokens = purchases.Where(p => p.IsAcknowledged != true)
            .Select(p => p.PurchaseToken ?? p.TransactionIdentifier)
            .OfType<string>()
            .ToArray();
        if (tokens.Length > 0)
            await billing.FinalizePurchaseAsync(tokens);
    }

    /// <summary>Le plugin n'accepte qu'une connexion à la fois : on sérialise les appels.</summary>
    private async Task WithBillingAsync(Func<IInAppBilling, Task> action)
    {
        await _lock.WaitAsync();
        var billing = CrossInAppBilling.Current;
        try
        {
            if (!await billing.ConnectAsync())
                throw new InAppBillingPurchaseException(PurchaseError.ServiceUnavailable);
            await action(billing);
        }
        finally
        {
            await billing.DisconnectAsync();
            _lock.Release();
        }
    }
}
#endif
