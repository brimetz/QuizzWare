using QuizzWare.Core.Premium;
using QuizzWare.Services;

namespace QuizzWare.App.Tests;

/// <summary>Le droit Premium est daté, chiffré et revérifié : jamais un booléen en clair.</summary>
public class EntitlementTests
{
    private readonly FakeSecureStorage _secure = new();
    private readonly FakePreferences _preferences = new();
    private readonly MovableClock _clock = new(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));

    private TestSubscription Create() => new(new SecureEntitlementStore(_secure), _clock, _preferences);

    [Fact]
    public void OldPlainBoolean_IsErasedAndIgnored()
    {
        _preferences.Set("premium.active", true);

        var subscription = Create();

        Assert.False(subscription.IsPremium);
        Assert.False(_preferences.ContainsKey("premium.active"));
    }

    [Fact]
    public async Task GrantedEntitlement_IsEncryptedAndSurvivesARestart()
    {
        await Create().GrantAsync();

        Assert.NotNull(_secure.Values["premium.entitlement"]);
        Assert.True(Create().IsPremium);
    }

    [Fact]
    public async Task WithoutNewVerification_PremiumExpiresAfterTheOfflineGrace()
    {
        var subscription = Create();
        await subscription.GrantAsync();

        _clock.Advance(PremiumEntitlement.OfflineGrace - TimeSpan.FromHours(1));
        Assert.True(subscription.IsPremium);

        _clock.Advance(TimeSpan.FromHours(1));
        Assert.False(subscription.IsPremium);
    }

    [Fact]
    public async Task Revoke_ErasesTheEntitlement()
    {
        var subscription = Create();
        await subscription.GrantAsync();

        await subscription.RevokeAsync();

        Assert.False(subscription.IsPremium);
        Assert.False(_secure.Values.ContainsKey("premium.entitlement"));
        Assert.False(Create().IsPremium);
    }

    [Fact]
    public async Task UnreadableValue_GrantsNothingAndIsErased()
    {
        _secure.Values["premium.entitlement"] = "{ pas du json";

        Assert.Null(await new SecureEntitlementStore(_secure).LoadAsync());
        Assert.False(_secure.Values.ContainsKey("premium.entitlement"));
    }

    /// <summary>Expose l'accord et le retrait du droit, que seuls les vrais services déclenchent après vérification.</summary>
    private sealed class TestSubscription(IEntitlementStore store, TimeProvider time, IPreferences preferences)
        : SubscriptionServiceBase(store, time, preferences)
    {
        public override bool IsSimulated => true;
        public new Task GrantAsync() => base.GrantAsync();
        public new Task RevokeAsync() => base.RevokeAsync();
        public override Task RefreshAsync() => Task.CompletedTask;
        public override Task<string?> GetPriceAsync() => Task.FromResult<string?>(null);
        public override Task<PurchaseOutcome> PurchaseAsync() => Task.FromResult(PurchaseOutcome.Unavailable);
        public override Task OpenManagementAsync() => Task.CompletedTask;
    }
}

internal sealed class FakeSecureStorage : ISecureStorage
{
    public Dictionary<string, string> Values { get; } = [];

    public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value)
    {
        Values[key] = value;
        return Task.CompletedTask;
    }

    public bool Remove(string key) => Values.Remove(key);
    public void RemoveAll() => Values.Clear();
}

internal sealed class MovableClock(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}
