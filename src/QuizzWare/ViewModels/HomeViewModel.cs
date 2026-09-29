using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Leveling;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

public partial class HomeViewModel(ContentRepository content, ProgressService progressService) : ObservableObject
{
    [ObservableProperty]
    private string streakText = "🔥 0";

    [ObservableProperty]
    private string summaryText = string.Empty;

    public ObservableCollection<CategoryCardViewModel> Categories { get; } = [];

    public async Task LoadAsync()
    {
        var progress = await progressService.GetProgressAsync();

        Categories.Clear();
        foreach (var category in content.Categories)
            Categories.Add(new CategoryCardViewModel(category, progressService.GetLevel(progress, category.Id)));

        var totalXp = progress.Categories.Values.Sum(c => c.TotalXp);
        var playableLessons = content.Categories.SelectMany(c => c.AllLessons).Where(l => !l.IsPlanned).Select(l => l.Id).ToHashSet();
        var completedLessons = progress.Categories.Values.Sum(c => c.CompletedLessons.Count(playableLessons.Contains));

        StreakText = $"🔥 {progressService.GetCurrentStreak(progress)}";
        SummaryText = $"{totalXp} XP · {completedLessons}/{playableLessons.Count} leçons terminées · {content.Categories.Count} catégories";
    }
}

public sealed partial class CategoryCardViewModel(Category category, LevelInfo level)
{
    public string Name => category.Name;
    public string Description => category.Description;
    public string Icon => category.Icon;
    public Color Color { get; } = Color.FromArgb(category.Color);
    public string LevelText => category.HasContent ? LevelFormat.Level(level) : "🕓 Contenu à venir";
    public string XpText => category.HasContent ? LevelFormat.Xp(level) : "Parcours et spécialisations déjà tracés";
    public double Progress => level.Progress;
    public double Opacity => category.HasContent ? 1 : 0.6;

    [RelayCommand]
    private Task OpenAsync() => Shell.Current.GoToAsync($"{nameof(CategoryPage)}?categoryId={category.Id}");
}
