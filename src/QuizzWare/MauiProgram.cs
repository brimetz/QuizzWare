using Microsoft.Extensions.Logging;
using QuizzWare.Core.Content;
using QuizzWare.Core.Progress;
using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels;
using QuizzWare.Views;

namespace QuizzWare;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

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

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
