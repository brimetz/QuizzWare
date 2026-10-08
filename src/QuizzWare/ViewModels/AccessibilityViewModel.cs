using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Appearance;
using QuizzWare.Services;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.ViewModels;

/// <summary>
/// Réglages d'accessibilité : thème, couleurs de correction, police, tailles du texte et du code, animations.
/// Chaque changement s'applique tout de suite à l'application et à l'aperçu.
/// </summary>
public partial class AccessibilityViewModel : ObservableObject
{
    private const string SampleCode = "def moyenne(notes):\n    # somme divisée par le nombre\n    return sum(notes) / len(notes)\n\nprint(moyenne([12, 15, 18]))";

    private readonly AppearanceService _appearance;
    private bool _loading;

    public AccessibilityViewModel(AppearanceService appearance)
    {
        _appearance = appearance;

        ThemeOptions = Themes.All
            .Select(t => new AppearanceOptionViewModel(t.Id, t.Name, ThemeDescription(t), s => s with { Theme = t.Id }, Select)
            {
                Swatches = [Color.FromArgb(t.Background), Color.FromArgb(t.Surface), Color.FromArgb(t.Primary), Color.FromArgb(t.CodeKeyword)],
            })
            .ToList();

        Fonts =
        [
            new(AppFonts.System, "Police du système", "Celle de ton téléphone.", s => s with { Font = AppFonts.System }, Select),
            new(AppFonts.Hyperlegible, "Atkinson Hyperlegible", "Lettres très distinctes, conçue pour les personnes malvoyantes.",
                s => s with { Font = AppFonts.Hyperlegible }, Select) { FontFamily = AppearanceService.FontFamilyFor(AppFonts.Hyperlegible) },
            new(AppFonts.Dyslexic, "OpenDyslexic", "Lettres lestées vers le bas, pensée pour certaines personnes dyslexiques.",
                s => s with { Font = AppFonts.Dyslexic }, Select) { FontFamily = AppearanceService.FontFamilyFor(AppFonts.Dyslexic) },
        ];

        TextSizes = AppearanceSettings.TextScales
            .Select(scale => new AppearanceOptionViewModel(Key(scale), Percent(scale), string.Empty, s => s with { TextScale = scale }, Select))
            .ToList();
        CodeSizes = AppearanceSettings.CodeScales
            .Select(scale => new AppearanceOptionViewModel(Key(scale), Percent(scale), string.Empty, s => s with { CodeScale = scale }, Select))
            .ToList();
    }

    public IReadOnlyList<AppearanceOptionViewModel> ThemeOptions { get; }
    public IReadOnlyList<AppearanceOptionViewModel> Fonts { get; }
    public IReadOnlyList<AppearanceOptionViewModel> TextSizes { get; }
    public IReadOnlyList<AppearanceOptionViewModel> CodeSizes { get; }

    [ObservableProperty]
    private bool adaptedFeedback;

    [ObservableProperty]
    private bool reduceMotion;

    // --- Aperçu : une question corrigée, recalculée à chaque changement ---

    [ObservableProperty]
    private IReadOnlyList<CodeLineViewModel> previewCode = [];

    [ObservableProperty]
    private Color previewCorrectBackground = Palette.SuccessSurface;

    [ObservableProperty]
    private Color previewCorrectStroke = Palette.Success;

    [ObservableProperty]
    private Color previewWrongBackground = Palette.ErrorSurface;

    [ObservableProperty]
    private Color previewWrongStroke = Palette.Error;

    [ObservableProperty]
    private Color previewOptionBackground = Palette.Surface;

    [ObservableProperty]
    private Color previewOptionStroke = Palette.Stroke;

    public void Load()
    {
        _loading = true;
        AdaptedFeedback = _appearance.Current.AdaptedFeedback;
        ReduceMotion = _appearance.Current.ReduceMotion;
        _loading = false;
        Refresh();
    }

    partial void OnAdaptedFeedbackChanged(bool value)
    {
        if (!_loading)
            Apply(s => s with { AdaptedFeedback = value });
    }

    partial void OnReduceMotionChanged(bool value)
    {
        if (!_loading)
            Apply(s => s with { ReduceMotion = value });
    }

    [RelayCommand]
    private void Reset()
    {
        _appearance.Update(_ => new AppearanceSettings());
        Load();
    }

    private void Select(AppearanceOptionViewModel option) => Apply(option.Change);

    private void Apply(Func<AppearanceSettings, AppearanceSettings> change)
    {
        _appearance.Update(change);
        Refresh();
    }

    /// <summary>Met à jour les sélections et l'aperçu avec les couleurs et tailles du thème courant.</summary>
    private void Refresh()
    {
        var s = _appearance.Current;
        foreach (var o in ThemeOptions)
            o.Refresh(o.Id == s.Theme);
        foreach (var o in Fonts)
            o.Refresh(o.Id == s.Font);
        foreach (var o in TextSizes)
            o.Refresh(o.Id == Key(s.TextScale));
        foreach (var o in CodeSizes)
            o.Refresh(o.Id == Key(s.CodeScale));

        PreviewCode = CodeLineViewModel.FromCode(SampleCode, "python");
        (PreviewCorrectBackground, PreviewCorrectStroke) = (Palette.SuccessSurface, Palette.Success);
        (PreviewWrongBackground, PreviewWrongStroke) = (Palette.ErrorSurface, Palette.Error);
        (PreviewOptionBackground, PreviewOptionStroke) = (Palette.Surface, Palette.Stroke);
    }

    private static string ThemeDescription(Theme theme) => theme.Id switch
    {
        "sombre" => "Le thème d'origine, reposant le soir.",
        "clair" => "Fond blanc, idéal en plein jour.",
        "contraste" => "Noir et blanc, contrastes maximaux.",
        "sepia" => "Tons chauds, fatigue moins les yeux.",
        _ => string.Empty,
    };

    private static string Key(double scale) => scale.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Percent(double scale) => $"{scale * 100:0} %";
}

/// <summary>Une option sélectionnable (thème, police, taille), mise en évidence quand elle est choisie.</summary>
public partial class AppearanceOptionViewModel(
    string id, string label, string description,
    Func<AppearanceSettings, AppearanceSettings> change, Action<AppearanceOptionViewModel> onSelect) : ObservableObject
{
    public string Id => id;
    public string Label => label;
    public string Description => description;
    public bool HasDescription => description.Length > 0;
    public Func<AppearanceSettings, AppearanceSettings> Change => change;

    /// <summary>Police dans laquelle le libellé est affiché (aperçu de la police) ; vide = police de l'application.</summary>
    public string? FontFamily { get; init; }

    /// <summary>Couleurs représentatives d'un thème (fond, carte, principale, code).</summary>
    public IReadOnlyList<Color> Swatches { get; init; } = [];
    public bool HasSwatches => Swatches.Count > 0;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private Color strokeColor = Palette.Stroke;

    [ObservableProperty]
    private Color backgroundColor = Palette.Surface;

    public string SelectedMark => IsSelected ? "✓" : string.Empty;

    public void Refresh(bool selected)
    {
        IsSelected = selected;
        StrokeColor = selected ? Palette.Primary : Palette.Stroke;
        BackgroundColor = selected ? Palette.PrimarySurface : Palette.Surface;
        OnPropertyChanged(nameof(SelectedMark));
    }

    [RelayCommand]
    private void Tap() => onSelect(this);
}
