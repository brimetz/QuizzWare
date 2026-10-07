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

        // Abonnement Premium : Google Play Billing en Release sur mobile, simulé en développement.
#if (ANDROID || IOS) && !DEBUG
        builder.Services.AddSingleton<ISubscriptionService, StoreSubscriptionService>();
#else
        builder.Services.AddSingleton<ISubscriptionService, SimulatedSubscriptionService>();
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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
