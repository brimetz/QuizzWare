using QuizzWare.ViewModels;
#if DEBUG
using QuizzWare.Services;
#endif

namespace QuizzWare.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
#if DEBUG
        AddDebugSection();
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

#if DEBUG
    /// <summary>
    /// Outils de développement, compilés uniquement en Debug : ils n'existent pas dans la version publiée.
    /// Servent à vérifier la remontée des plantages (voir <see cref="CrashReporter"/>).
    /// </summary>
    private void AddDebugSection()
    {
        var crash = new Button { Text = "💥 Provoquer un plantage" };
        crash.Clicked += (_, _) =>
            throw new InvalidOperationException("Plantage de test déclenché depuis les Réglages (Debug).");

        var error = new Button { Text = "⚠️ Enregistrer une erreur récupérée" };
        error.Clicked += async (_, _) =>
        {
            var crashes = Handler?.MauiContext?.Services.GetService<CrashReporter>();
            crashes?.Record(new TimeoutException("Erreur de test enregistrée depuis les Réglages (Debug)."), context: "Réglages › Debug");
            await DisplayAlertAsync("Erreur enregistrée", "Elle apparaît maintenant dans « Signaler un problème ».", "OK");
            await _viewModel.LoadAsync();
        };

        var title = new Label { Text = "🧪 Développement (Debug uniquement)" };
        var hint = new Label { Text = "Le plantage ferme l'application : au lancement suivant, elle propose d'envoyer le rapport." };
        title.SetDynamicResource(StyleProperty, "HeaderLabel");
        hint.SetDynamicResource(StyleProperty, "MutedLabel");
        foreach (var button in new[] { crash, error })
            button.SetDynamicResource(StyleProperty, "SecondaryButton");

        Sections.Children.Add(new VerticalStackLayout { Spacing = 10, Children = { title, hint, crash, error } });
    }
#endif
}
