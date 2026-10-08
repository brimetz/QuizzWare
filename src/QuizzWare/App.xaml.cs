using QuizzWare.Services;

namespace QuizzWare;

public partial class App : Application
{
    private readonly ReminderCoordinator _reminders;
    private readonly ISubscriptionService _subscription;
    private readonly CrashReporter _crashes;

    public App(ReminderCoordinator reminders, ISubscriptionService subscription, AppearanceService appearance, CrashReporter crashes)
    {
        InitializeComponent();
        appearance.ApplySaved(); // thème, police et tailles choisis dans Accessibilité, avant la première page
        _reminders = reminders;
        _subscription = subscription;
        _crashes = crashes;
        _reminders.Start();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell()) { Title = "QuizzWare" };

    protected override async void OnStart()
    {
        base.OnStart();
        try
        {
            await _reminders.RescheduleAsync();
            await _subscription.RefreshAsync(); // un abonnement résilié ou renouvelé ailleurs est pris en compte

            // Après un plantage lors de la dernière utilisation : proposer d'envoyer le rapport, une fois la page affichée.
            await Task.Delay(TimeSpan.FromSeconds(1));
            if (Shell.Current is { } shell)
                await _crashes.OfferPendingReportAsync(shell.DisplayAlert);
        }
        catch (Exception ex)
        {
            _crashes.Record(ex, context: "démarrage");
        }
    }

    // Au retour au premier plan, le jour a pu changer : on recalcule les rappels (objectif, série, révisions dues).
    protected override async void OnResume()
    {
        base.OnResume();
        try
        {
            await _reminders.RescheduleAsync();
            await _subscription.RefreshAsync();
        }
        catch (Exception ex)
        {
            _crashes.Record(ex, context: "retour au premier plan");
        }
    }
}
