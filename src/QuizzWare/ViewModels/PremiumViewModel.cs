using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Premium;
using QuizzWare.Services;

namespace QuizzWare.ViewModels;

/// <summary>
/// Page Premium : avantages, prix, achat et restauration. Ouverte depuis les réglages,
/// une catégorie Premium (paramètre categoryId) ou le quota d'entretiens atteint (paramètre reason=interview).
/// </summary>
public partial class PremiumViewModel(ISubscriptionService subscription, ContentRepository content)
    : ObservableObject, IQueryAttributable
{
    private string? _categoryId;
    private string? _reason;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotPremium))]
    private bool isPremium;

    [ObservableProperty]
    private string reasonText = string.Empty;

    [ObservableProperty]
    private bool hasReason;

    [ObservableProperty]
    private string priceText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubscribeCommand), nameof(RestoreCommand))]
    private bool isBusy;

    public bool IsNotPremium => !IsPremium;
    public bool IsSimulated => subscription.IsSimulated;

    public string InterviewsText =>
        $"🎤 Entretiens illimités (gratuit : {PremiumPolicy.FreeInterviewsPerWeek} par semaine)";

    public string CategoriesText { get; } = "🗂️ Toutes les catégories : " + string.Join(", ",
        content.Categories.Where(c => !PremiumPolicy.IsCategoryFree(c.Id)).Select(c => c.Name));

    public string FreeText => "Les langages de programmation et les fondamentaux (algorithmes, bases de données, Git, tests) restent gratuits.";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _categoryId = query.TryGetValue("categoryId", out var c) ? c as string : null;
        _reason = query.TryGetValue("reason", out var r) ? r as string : null;
    }

    public async Task LoadAsync()
    {
        subscription.Changed -= OnSubscriptionChanged;
        subscription.Changed += OnSubscriptionChanged;
        IsPremium = subscription.IsPremium;

        var category = _categoryId is null ? null : content.Categories.FirstOrDefault(c => c.Id == _categoryId);
        ReasonText = category is not null
            ? $"« {category.Name} » fait partie de QuizzWare Premium."
            : _reason == "interview"
                ? $"Tu as utilisé tes {PremiumPolicy.FreeInterviewsPerWeek} entretiens gratuits de la semaine."
                : string.Empty;
        HasReason = ReasonText.Length > 0;

        PriceText = "Chargement du prix…";
        var price = await subscription.GetPriceAsync();
        PriceText = price is null ? "Prix affiché par Google Play au moment de l'achat" : $"{price} par mois";

        await subscription.RefreshAsync();
        IsPremium = subscription.IsPremium;
    }

    public void Unload() => subscription.Changed -= OnSubscriptionChanged;

    private void OnSubscriptionChanged(object? sender, EventArgs e) => IsPremium = subscription.IsPremium;

    private bool CanAct() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task SubscribeAsync()
    {
        IsBusy = true;
        try
        {
            var outcome = await subscription.PurchaseAsync();
            IsPremium = subscription.IsPremium;
            var message = outcome switch
            {
                PurchaseOutcome.Success => null,
                PurchaseOutcome.Cancelled => null,
                PurchaseOutcome.Pending => "Ton paiement est en attente de validation. Premium s'activera dès qu'il sera confirmé.",
                PurchaseOutcome.Unavailable => "Google Play n'est pas disponible pour le moment. Vérifie ta connexion et réessaie.",
                _ => "L'achat n'a pas pu aboutir. Aucun montant n'a été débité ; réessaie dans un instant.",
            };
            if (outcome == PurchaseOutcome.Success)
            {
                await Shell.Current.DisplayAlert("Bienvenue dans Premium ⭐", "Toutes les catégories et les entretiens illimités sont débloqués. Merci pour ton soutien !", "Super");
                await Shell.Current.GoToAsync("..");
            }
            else if (message is not null)
                await Shell.Current.DisplayAlert("Abonnement", message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task RestoreAsync()
    {
        IsBusy = true;
        try
        {
            var restored = await subscription.RestoreAsync();
            IsPremium = restored;
            await Shell.Current.DisplayAlert("Restaurer mes achats", restored
                ? "Ton abonnement Premium est actif sur cet appareil."
                : "Aucun abonnement actif n'a été trouvé pour ce compte Google Play.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task ManageAsync() => subscription.OpenManagementAsync();
}
