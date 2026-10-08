using QuizzWare.Core.Appearance;

namespace QuizzWare.ViewModels;

/// <summary>
/// Couleurs utilisées depuis le code, issues du thème choisi dans Accessibilité (voir Core/Appearance/Themes.cs).
/// Les pages en XAML utilisent les mêmes couleurs via les ressources dynamiques ; <see cref="Services.AppearanceService"/>
/// met les deux à jour ensemble.
/// </summary>
public static class Palette
{
    public static Color Primary { get; private set; } = null!;
    public static Color PrimarySurface { get; private set; } = null!;
    public static Color Surface { get; private set; } = null!;
    public static Color SurfaceRaised { get; private set; } = null!;
    public static Color Stroke { get; private set; } = null!;
    public static Color TextPrimary { get; private set; } = null!;
    public static Color TextMuted { get; private set; } = null!;
    public static Color Success { get; private set; } = null!;
    public static Color SuccessSurface { get; private set; } = null!;
    public static Color Error { get; private set; } = null!;
    public static Color ErrorSurface { get; private set; } = null!;
    public static Color Warning { get; private set; } = null!;

    // Coloration syntaxique
    public static Color CodeText { get; private set; } = null!;
    public static Color CodeKeyword { get; private set; } = null!;
    public static Color CodeString { get; private set; } = null!;
    public static Color CodeNumber { get; private set; } = null!;
    public static Color CodeComment { get; private set; } = null!;
    public static Color CodeType { get; private set; } = null!;

    /// <summary>Taille du code : les spans (lignes colorées) la reçoivent explicitement, comme leur police.</summary>
    public static double CodeFontSize { get; private set; } = 13;

    static Palette() => Apply(Themes.Dark, adaptedFeedback: false, codeFontSize: 13);

    public static void Apply(Theme theme, bool adaptedFeedback, double codeFontSize)
    {
        var feedback = theme.FeedbackFor(adaptedFeedback);
        Primary = Color.FromArgb(theme.Primary);
        PrimarySurface = Color.FromArgb(theme.PrimarySurface);
        Surface = Color.FromArgb(theme.Surface);
        SurfaceRaised = Color.FromArgb(theme.SurfaceRaised);
        Stroke = Color.FromArgb(theme.Stroke);
        TextPrimary = Color.FromArgb(theme.TextPrimary);
        TextMuted = Color.FromArgb(theme.TextMuted);
        Success = Color.FromArgb(feedback.Success);
        SuccessSurface = Color.FromArgb(feedback.SuccessSurface);
        Error = Color.FromArgb(feedback.Error);
        ErrorSurface = Color.FromArgb(feedback.ErrorSurface);
        Warning = Color.FromArgb(theme.Warning);
        CodeText = Color.FromArgb(theme.CodeText);
        CodeKeyword = Color.FromArgb(theme.CodeKeyword);
        CodeString = Color.FromArgb(theme.CodeString);
        CodeNumber = Color.FromArgb(theme.CodeNumber);
        CodeComment = Color.FromArgb(theme.CodeComment);
        CodeType = Color.FromArgb(theme.CodeType);
        CodeFontSize = codeFontSize;
    }
}
