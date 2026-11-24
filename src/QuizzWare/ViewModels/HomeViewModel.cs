using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Leveling;
using QuizzWare.Core.Models;
using QuizzWare.Core.Premium;
using QuizzWare.Core.Progress;
using QuizzWare.Services;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

public partial class HomeViewModel(ContentRepository content, ProgressService progressService, ISubscriptionService subscription)
    : ObservableObject
{
    [ObservableProperty]
    private string streakText = "🔥 0";

    [ObservableProperty]
    private string summaryText = string.Empty;

    [ObservableProperty]
    private string dailyGoalText = string.Empty;

    [ObservableProperty]
    private double dailyGoalProgress;

    [ObservableProperty]
    private Color dailyGoalColor = Palette.Warning;

    [ObservableProperty]
    private bool hasReviews;

    [ObservableProperty]
    private string reviewText = string.Empty;

    public ObservableCollection<CategoryCardViewModel> Categories { get; } = [];

    public bool NeedsOnboarding { get; private set; }

    [RelayCommand]
    private Task StartReviewAsync() => Shell.Current.GoToAsync($"{nameof(QuizPage)}?mode=review");

    [RelayCommand]
    private Task OpenSettingsAsync() => Shell.Current.GoToAsync(nameof(SettingsPage));

    public async Task LoadAsync()
    {
        var progress = await progressService.GetProgressAsync();

        NeedsOnboarding = !progress.Settings.OnboardingCompleted;

        // Les centres d'intérêt choisis au premier lancement passent en tête.
        var interests = progress.Settings.Interests.ToHashSet();
        Categories.Clear();
        foreach (var category in content.Categories.OrderByDescending(c => interests.Contains(c.Id)))
            Categories.Add(new CategoryCardViewModel(category, progressService.GetLevel(progress, category.Id), interests.Contains(category.Id),
                isPremiumLocked: !PremiumPolicy.CanAccessCategory(category.Id, subscription.IsPremium), PremiumLabels.Badge(subscription)));

        var totalXp = progress.Categories.Values.Sum(c => c.TotalXp);
        var playableLessons = content.Categories.SelectMany(c => c.AllLessons).Where(l => !l.IsPlanned).Select(l => l.Id).ToHashSet();
        var completedLessons = progress.Categories.Values.Sum(c => c.CompletedLessons.Count(playableLessons.Contains));

        StreakText = $"🔥 {progressService.GetCurrentStreak(progress)}";

        var goal = progressService.GetDailyGoal(progress);
        DailyGoalProgress = goal.Progress;
        DailyGoalColor = goal.IsReached ? Palette.Success : Palette.Warning;
        DailyGoalText = goal.IsReached
            ? $"✅ Objectif du jour atteint · {goal.XpToday} XP"
            : $"🎯 Objectif du jour · {goal.XpToday} / {goal.GoalXp} XP";

        var due = progressService.CountDueReviews(progress);
        HasReviews = due > 0;
        ReviewText = $"🧠 Réviser · {due} question{(due > 1 ? "s" : "")}";
        SummaryText = $"{totalXp} XP · {completedLessons}/{playableLessons.Count} leçons terminées · {content.Categories.Count} catégories";
    }
}

public sealed partial class CategoryCardViewModel(Category category, LevelInfo level, bool isFavorite = false, bool isPremiumLocked = false,
    string lockBadge = "⭐ Premium")
{
    public string Name => category.Name;
    public bool IsFavorite => isFavorite;

    /// <summary>Catégorie réservée à Premium pour un utilisateur gratuit : l'ouvrir présente l'abonnement (ou ce qui arrive bientôt).</summary>
    public bool IsPremiumLocked => isPremiumLocked;
    public string LockBadge => lockBadge;
    public string Description => category.Description;
    public string Icon => category.Icon;
    public Color Color { get; } = Color.FromArgb(category.Color);
    public string LevelText => category.HasContent ? LevelFormat.Level(level) : "🕓 Contenu à venir";
    public string XpText => category.HasContent ? LevelFormat.Xp(level) : "Parcours et spécialisations déjà tracés";
    public double Progress => level.Progress;
    public double Opacity => category.HasContent ? 1 : 0.6;

    [RelayCommand]
    private Task OpenAsync() => Shell.Current.GoToAsync(isPremiumLocked
        ? $"{nameof(PremiumPage)}?categoryId={category.Id}"
        : $"{nameof(CategoryPage)}?categoryId={category.Id}");
}
