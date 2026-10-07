using QuizzWare.Services;

namespace QuizzWare;

public partial class App : Application
{
    private readonly ReminderCoordinator _reminders;
    private readonly ISubscriptionService _subscription;

    public App(ReminderCoordinator reminders, ISubscriptionService subscription)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _reminders = reminders;
        _subscription = subscription;
        _reminders.Start();
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell()) { Title = "QuizzWare" };

    protected override async void OnStart()
    {
        base.OnStart();
        await _reminders.RescheduleAsync();
        await _subscription.RefreshAsync(); // un abonnement résilié ou renouvelé ailleurs est pris en compte
    }

    // Au retour au premier plan, le jour a pu changer : on recalcule les rappels (objectif, série, révisions dues).
    protected override async void OnResume()
    {
        base.OnResume();
        await _reminders.RescheduleAsync();
        await _subscription.RefreshAsync();
    }
}
