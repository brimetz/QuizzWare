using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Premium;
using QuizzWare.Core.Progress;
using QuizzWare.Services;

namespace QuizzWare.ViewModels;

public partial class SettingsViewModel(ProgressService progressService, IReminderService reminders, ISubscriptionService subscription)
    : ObservableObject
{
    [ObservableProperty]
    private string subscriptionTitle = string.Empty;

    [ObservableProperty]
    private string subscriptionText = string.Empty;

    [RelayCommand]
    private Task OpenPremiumAsync() => Shell.Current.GoToAsync(nameof(Views.PremiumPage));

    private bool _loading;

    public IReadOnlyList<GoalOptionViewModel> Goals { get; } =
    [
        new(10, "Détendu", "Une leçon rapide"),
        new(30, "Normal", "Environ 10 minutes"),
        new(50, "Sérieux", "Environ 15 minutes"),
        new(100, "Intense", "Une vraie séance de travail"),
    ];

    public bool RemindersSupported => reminders.IsSupported;
    public bool RemindersUnsupported => !reminders.IsSupported;

    [ObservableProperty]
    private bool remindersEnabled;

    [ObservableProperty]
    private TimeSpan reminderTime = new(19, 0, 0);

    [ObservableProperty]
    private bool soundEnabled = true;

    [ObservableProperty]
    private bool hapticsEnabled = true;

    [ObservableProperty]
    private string statsText = string.Empty;

    public async Task LoadAsync()
    {
        _loading = true;
        var progress = await progressService.GetProgressAsync();
        foreach (var goal in Goals)
        {
            goal.IsSelected = goal.Xp == progress.Settings.DailyGoalXp;
            goal.Select = SelectGoalAsync;
        }

        RemindersEnabled = progress.Settings.RemindersEnabled;
        ReminderTime = progress.Settings.ReminderTime.ToTimeSpan();
        SoundEnabled = progress.Settings.SoundEnabled;
        HapticsEnabled = progress.Settings.HapticsEnabled;
        StatsText = $"🧠 {progressService.CountMasteredQuestions(progress)} questions maîtrisées · " +
                    $"{progressService.CountDueReviews(progress)} à réviser · 🔥 meilleure série : {progress.BestStreak} jours";

        var freeLeft = PremiumPolicy.FreeInterviewsLeft(progress, progressService.Now());
        (SubscriptionTitle, SubscriptionText) = subscription.IsPremium
            ? ("⭐ Premium actif", "Toutes les catégories et les entretiens illimités. Touche pour gérer ton abonnement.")
            : ("Version gratuite", $"{freeLeft} entretien{(freeLeft > 1 ? "s" : "")} gratuit{(freeLeft > 1 ? "s" : "")} restant{(freeLeft > 1 ? "s" : "")} cette semaine. Touche pour découvrir Premium.");
        _loading = false;
    }

    private async Task SelectGoalAsync(GoalOptionViewModel selected)
    {
        foreach (var goal in Goals)
            goal.IsSelected = goal == selected;
        await progressService.UpdateSettingsAsync(s => s.DailyGoalXp = selected.Xp);
    }

    async partial void OnRemindersEnabledChanged(bool value)
    {
        if (_loading)
            return;

        if (value && !await reminders.RequestPermissionAsync())
        {
            await Shell.Current.DisplayAlert("Notifications désactivées",
                "Autorise les notifications de QuizzWare dans les réglages du téléphone pour recevoir tes rappels.", "OK");
            _loading = true;
            RemindersEnabled = false;
            _loading = false;
            return;
        }

        await progressService.UpdateSettingsAsync(s => s.RemindersEnabled = value);
    }

    async partial void OnReminderTimeChanged(TimeSpan value)
    {
        if (_loading)
            return;
        await progressService.UpdateSettingsAsync(s => s.ReminderTime = TimeOnly.FromTimeSpan(value));
    }

    async partial void OnSoundEnabledChanged(bool value)
    {
        if (!_loading)
            await progressService.UpdateSettingsAsync(s => s.SoundEnabled = value);
    }

    async partial void OnHapticsEnabledChanged(bool value)
    {
        if (!_loading)
            await progressService.UpdateSettingsAsync(s => s.HapticsEnabled = value);
    }

    [RelayCommand]
    private Task OpenAccessibilityAsync() => Shell.Current.GoToAsync(nameof(Views.AccessibilityPage));

    [RelayCommand]
    private Task RedoOnboardingAsync() => Shell.Current.GoToAsync(nameof(Views.OnboardingPage));

    [RelayCommand]
    private async Task ResetAsync()
    {
        var confirmed = await Shell.Current.DisplayAlert("Réinitialiser la progression ?",
            "XP, niveaux, leçons validées, série et révisions seront effacés. Cette action est irréversible.",
            "Tout effacer", "Annuler");
        if (!confirmed)
            return;

        await progressService.ResetAsync();
        await LoadAsync();
        await Shell.Current.DisplayAlert("C'est reparti !", "Ta progression a été réinitialisée.", "OK");
    }
}

public partial class GoalOptionViewModel(int xp, string label, string description) : ObservableObject
{
    public int Xp => xp;
    public string Label => label;
    public string Description => description;
    public string XpText => $"{xp} XP / jour";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrokeColor), nameof(BackgroundColor))]
    private bool isSelected;

    public Color StrokeColor => IsSelected ? Palette.Primary : Palette.Stroke;
    public Color BackgroundColor => IsSelected ? Palette.PrimarySurface : Palette.Surface;

    public Func<GoalOptionViewModel, Task>? Select { get; set; }

    [RelayCommand]
    private Task TapAsync() => Select?.Invoke(this) ?? Task.CompletedTask;
}
