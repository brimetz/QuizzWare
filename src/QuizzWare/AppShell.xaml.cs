using QuizzWare.Views;

namespace QuizzWare;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(CategoryPage), typeof(CategoryPage));
        Routing.RegisterRoute(nameof(LessonPage), typeof(LessonPage));
        Routing.RegisterRoute(nameof(QuizPage), typeof(QuizPage));
        Routing.RegisterRoute(nameof(ResultPage), typeof(ResultPage));
        Routing.RegisterRoute(nameof(SpecializationsPage), typeof(SpecializationsPage));
        Routing.RegisterRoute(nameof(TrackPage), typeof(TrackPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(OnboardingPage), typeof(OnboardingPage));
    }
}
