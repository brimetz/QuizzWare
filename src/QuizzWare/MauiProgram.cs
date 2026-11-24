using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using QuizzWare.Core.Content;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;
using QuizzWare.Services;
using QuizzWare.ViewModels;
using QuizzWare.Views;

namespace QuizzWare;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        builder.ConfigureFonts(fonts =>
        {
            // Polices d'accessibilité (Accessibilité > Police)
            fonts.AddFont("AtkinsonHyperlegible-Regular.ttf", "Atkinson");
            fonts.AddFont("AtkinsonHyperlegible-Bold.ttf", "AtkinsonBold");
            fonts.AddFont("OpenDyslexic-Regular.otf", "OpenDyslexic");
            fonts.AddFont("OpenDyslexic-Bold.otf", "OpenDyslexicBold");
        });

        // Rappels quotidiens : notifications locales sur mobile, rien sur Windows (développement).
#if ANDROID || IOS
        Plugin.LocalNotification.LocalNotificationExtensions.UseLocalNotification(builder, _ => { });
        builder.Services.AddSingleton<IReminderService, LocalReminderService>();
#else
        builder.Services.AddSingleton<IReminderService, NoReminderService>();
#endif
        builder.Services.AddSingleton<ReminderCoordinator>();

        // Sons et vibrations des réponses
        builder.AddAudio();
        builder.Services.AddSingleton<FeedbackService>();
        builder.Services.AddSingleton<ContentReporter>();
        builder.Services.AddSingleton(Preferences.Default);
        builder.Services.AddSingleton<AppearanceService>();

        // Plantages et erreurs : journal local, envoi par l'utilisateur seulement (issue GitHub pré-remplie)
        builder.Services.AddSingleton(_ => new QuizzWare.Core.Diagnostics.ErrorLog(Path.Combine(FileSystem.AppDataDirectory, "erreurs.json")));
        builder.Services.AddSingleton<IDeviceDescriber, MauiDeviceDescriber>();
        builder.Services.AddSingleton<IBrowserLauncher, MauiBrowserLauncher>();
        builder.Services.AddSingleton<CrashReporter>();

        // Abonnement Premium : Google Play Billing / StoreKit en Release sur mobile, simulé en développement.
        // Tant qu'il n'est pas proposé (PremiumOffered dans le .csproj), les contenus Premium sont « À venir ».
#if !PREMIUM
        builder.Services.AddSingleton<ISubscriptionService, ComingSoonSubscriptionService>();
#else
        // Droit Premium chiffré (Keystore / trousseau), jamais un booléen en clair.
        builder.Services.AddSingleton(SecureStorage.Default);
        builder.Services.AddSingleton<IEntitlementStore, SecureEntitlementStore>();
#if (ANDROID || IOS) && !DEBUG
        builder.Services.AddSingleton<ISubscriptionService, StoreSubscriptionService>();
#else
        builder.Services.AddSingleton<ISubscriptionService, SimulatedSubscriptionService>();
#endif
#endif

        // Core
        builder.Services.AddSingleton(_ => ContentRepository.LoadEmbedded());
        builder.Services.AddSingleton<IProgressStore>(_ =>
            new JsonFileProgressStore(Path.Combine(FileSystem.AppDataDirectory, "progress.json")));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ProgressService>();
        builder.Services.AddSingleton(_ => new QuizBuilder(Random.Shared));

        // Pages et ViewModels (résolus par Shell lors de la navigation)
        builder.Services.AddTransient<HomePage>().AddTransient<HomeViewModel>();
        builder.Services.AddTransient<CategoryPage>().AddTransient<CategoryViewModel>();
        builder.Services.AddTransient<LessonPage>().AddTransient<LessonViewModel>();
        builder.Services.AddTransient<QuizPage>().AddTransient<QuizViewModel>();
        builder.Services.AddTransient<ResultPage>().AddTransient<ResultViewModel>();
        builder.Services.AddTransient<SpecializationsPage>().AddTransient<SpecializationsViewModel>();
        builder.Services.AddTransient<TrackPage>().AddTransient<TrackViewModel>();
        builder.Services.AddTransient<SettingsPage>().AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<OnboardingPage>().AddTransient<OnboardingViewModel>();
        builder.Services.AddTransient<PremiumPage>().AddTransient<PremiumViewModel>();
        builder.Services.AddTransient<AccessibilityPage>().AddTransient<AccessibilityViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        // Au plus tôt : un plantage pendant le démarrage doit aussi être enregistré.
        app.Services.GetRequiredService<CrashReporter>().Install();
        return app;
    }
}
