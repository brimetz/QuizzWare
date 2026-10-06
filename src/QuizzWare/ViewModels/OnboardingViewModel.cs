using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

/// <summary>
/// Premier lancement : bienvenue, profil, centres d'intérêt, objectif quotidien, puis un parcours conseillé
/// (avec le test de niveau pour les profils expérimentés).
/// </summary>
public partial class OnboardingViewModel(ContentRepository content, ProgressService progressService) : ObservableObject
{
    public const int StepCount = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome), nameof(IsProfile), nameof(IsInterests), nameof(IsGoal), nameof(IsSummary), nameof(IsNotSummary),
        nameof(StepText), nameof(StepProgress), nameof(NextText), nameof(CanGoBack))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    private int step;

    public bool IsWelcome => Step == 0;
    public bool IsProfile => Step == 1;
    public bool IsInterests => Step == 2;
    public bool IsGoal => Step == 3;
    public bool IsSummary => Step == 4;
    public bool IsNotSummary => !IsSummary;
    public bool CanGoBack => Step is > 0 and < 4;
    public string StepText => $"{Step + 1} / {StepCount}";
    public double StepProgress => (double)(Step + 1) / StepCount;
    public string NextText => Step switch
    {
        0 => "C'est parti",
        3 => "Terminer",
        _ => "Continuer",
    };

    public IReadOnlyList<OptionViewModel> ProfileOptions { get; } =
    [
        new(Profiles.Etudiant, "🎓 Étudiant ou en reconversion", "J'apprends les bases"),
        new(Profiles.Junior, "🌱 Junior", "Moins de 2 ans d'expérience"),
        new(Profiles.Confirme, "🚀 Confirmé", "2 à 5 ans d'expérience"),
        new(Profiles.Senior, "🧠 Senior", "Plus de 5 ans, je prépare des entretiens"),
    ];

    [ObservableProperty]
    private IReadOnlyList<OptionViewModel> categoryOptions = [];

    public IReadOnlyList<GoalOptionViewModel> Goals { get; } =
    [
        new(10, "Détendu", "Une leçon rapide"),
        new(30, "Normal", "Environ 10 minutes"),
        new(50, "Sérieux", "Environ 15 minutes"),
        new(100, "Intense", "Une vraie séance de travail"),
    ];

    [ObservableProperty]
    private IReadOnlyList<RecommendationViewModel> recommendations = [];

    [ObservableProperty]
    private string summaryText = string.Empty;

    public void Load()
    {
        if (CategoryOptions.Count > 0)
            return;

        foreach (var option in ProfileOptions)
            option.Select = SelectProfile;

        // Les catégories avec du contenu d'abord : ce sont celles qu'on peut commencer tout de suite.
        CategoryOptions = content.Categories
            .OrderByDescending(c => c.MainLessons.Count(l => !l.IsPlanned))
            .Select(c => new OptionViewModel(c.Id, c.Name, c.HasContent ? $"{c.MainLessons.Count(l => !l.IsPlanned)} leçons" : "Bientôt")
            {
                Select = ToggleCategory,
            })
            .ToList();

        foreach (var goal in Goals)
        {
            goal.IsSelected = goal.Xp == UserSettings.DefaultDailyGoalXp;
            goal.Select = SelectGoal;
        }
    }

    private Task SelectProfile(OptionViewModel selected)
    {
        foreach (var option in ProfileOptions)
            option.IsSelected = option == selected;
        NextCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    private Task ToggleCategory(OptionViewModel option)
    {
        option.IsSelected = !option.IsSelected;
        NextCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    private Task SelectGoal(GoalOptionViewModel selected)
    {
        foreach (var goal in Goals)
            goal.IsSelected = goal == selected;
        return Task.CompletedTask;
    }

    private bool CanGoNext() => Step switch
    {
        1 => ProfileOptions.Any(o => o.IsSelected),
        2 => CategoryOptions.Any(o => o.IsSelected),
        4 => false,
        _ => true,
    };

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextAsync()
    {
        if (Step == 3)
        {
            await SaveAsync();
            BuildSummary();
        }
        Step++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CanGoBack)
            Step--;
    }

    /// <summary>« Passer » : on ne redemandera plus, l'accueil s'affiche directement.</summary>
    [RelayCommand]
    private async Task SkipAsync()
    {
        await progressService.UpdateSettingsAsync(s => s.OnboardingCompleted = true);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task FinishAsync() => Shell.Current.GoToAsync("..");

    private async Task SaveAsync()
    {
        var profile = ProfileOptions.First(o => o.IsSelected).Id;
        var interests = CategoryOptions.Where(o => o.IsSelected).Select(o => o.Id).ToList();
        var goal = Goals.First(g => g.IsSelected).Xp;

        await progressService.UpdateSettingsAsync(s =>
        {
            s.Profile = profile;
            s.Interests = interests;
            s.DailyGoalXp = goal;
            s.OnboardingCompleted = true;
        });
    }

    private void BuildSummary()
    {
        var profile = ProfileOptions.First(o => o.IsSelected).Id;
        var suggestPlacement = Profiles.SuggestsPlacement(profile);
        var chosen = CategoryOptions.Where(o => o.IsSelected).Select(o => content.GetCategory(o.Id)).ToList();

        Recommendations = chosen
            .Select(c => new RecommendationViewModel(c, suggestPlacement && c.Chapters.Count(ch => ch.Lessons.Any(l => !l.IsPlanned)) >= 2))
            .ToList();

        SummaryText = suggestPlacement
            ? "Tu as déjà de l'expérience : le test de niveau te fait sauter les chapitres que tu maîtrises."
            : "Commence par le premier chapitre : chaque leçon débloque la suivante.";
    }
}

/// <summary>Une option sélectionnable (profil, catégorie).</summary>
public partial class OptionViewModel(string id, string label, string description) : ObservableObject
{
    public string Id => id;
    public string Label => label;
    public string Description => description;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrokeColor), nameof(BackgroundColor))]
    private bool isSelected;

    public Color StrokeColor => IsSelected ? Palette.Primary : Palette.Stroke;
    public Color BackgroundColor => IsSelected ? Palette.PrimarySurface : Palette.Surface;

    public Func<OptionViewModel, Task>? Select { get; set; }

    [RelayCommand]
    private Task TapAsync() => Select?.Invoke(this) ?? Task.CompletedTask;
}

/// <summary>Une catégorie choisie, avec le bon point de départ.</summary>
public partial class RecommendationViewModel(Category category, bool suggestPlacement)
{
    public string Name => category.Name;
    public string Icon => category.Icon;
    public Color Color { get; } = Color.FromArgb(category.Color);
    public bool SuggestPlacement => suggestPlacement;
    public bool HasContent => category.HasContent;
    public string Hint => !category.HasContent ? "Contenu en préparation : l'arbre des sujets est déjà visible."
        : suggestPlacement ? "Test de niveau conseillé (5 à 10 minutes)."
        : "Premier chapitre : " + category.Chapters.First(c => c.Lessons.Any(l => !l.IsPlanned)).Title;

    [RelayCommand]
    private Task OpenAsync() => Shell.Current.GoToAsync($"../{nameof(CategoryPage)}?categoryId={category.Id}");

    [RelayCommand]
    private Task PlacementAsync() => Shell.Current.GoToAsync($"../{nameof(QuizPage)}?categoryId={category.Id}&mode=placement");
}
