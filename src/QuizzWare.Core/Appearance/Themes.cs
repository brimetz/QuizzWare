using System.Globalization;

namespace QuizzWare.Core.Appearance;

/// <summary>Couleurs de correction : bonne réponse, mauvaise réponse et leurs fonds.</summary>
public sealed record FeedbackColors(string Success, string SuccessSurface, string Error, string ErrorSurface);

/// <summary>
/// Un thème complet, en couleurs hexadécimales (#RRGGBB). Les contrastes sont vérifiés par les tests
/// (critères WCAG : 4,5:1 pour le texte, 3:1 pour les éléments d'interface).
/// </summary>
public sealed record Theme(
    string Id,
    string Name,
    bool IsLight,
    string Background,
    string Surface,
    string SurfaceRaised,
    string Stroke,
    string TextPrimary,
    string TextMuted,
    string Primary,
    string PrimaryDark,
    string PrimarySurface,
    string OnPrimary,
    string Warning,
    FeedbackColors Feedback,
    FeedbackColors AdaptedFeedback,
    string CodeBackground,
    string CodeText,
    string CodeKeyword,
    string CodeString,
    string CodeNumber,
    string CodeComment,
    string CodeType)
{
    public FeedbackColors FeedbackFor(bool adapted) => adapted ? AdaptedFeedback : Feedback;
}

public static class Themes
{
    // Bleu / orange (palette d'Okabe et Ito) : se distinguent avec la deutéranopie et la protanopie.
    private static readonly FeedbackColors AdaptedDark = new("#56B4E9", "#12324A", "#E69F00", "#4A3300");
    private static readonly FeedbackColors AdaptedLight = new("#0B5FA5", "#DCEBFA", "#A65200", "#FBE8D2");

    public static readonly Theme Dark = new(
        "sombre", "Sombre", IsLight: false,
        Background: "#0F1221", Surface: "#1A1F36", SurfaceRaised: "#242A47", Stroke: "#2F365A",
        TextPrimary: "#E8EAF6", TextMuted: "#9AA0C3",
        Primary: "#7C4DFF", PrimaryDark: "#5A2FD6", PrimarySurface: "#2E2560", OnPrimary: "#FFFFFF", Warning: "#FFC857",
        Feedback: new("#2ECC71", "#173D2B", "#FF5370", "#45192A"), AdaptedFeedback: AdaptedDark,
        CodeBackground: "#0A0D18", CodeText: "#E8EAF6", CodeKeyword: "#C792EA", CodeString: "#C3E88D",
        CodeNumber: "#F78C6C", CodeComment: "#8A91B8", CodeType: "#82AAFF");

    public static readonly Theme Light = new(
        "clair", "Clair", IsLight: true,
        Background: "#F5F6FA", Surface: "#FFFFFF", SurfaceRaised: "#ECEEF5", Stroke: "#C9CEDD",
        TextPrimary: "#1A1D2E", TextMuted: "#50566E",
        Primary: "#5B2FE0", PrimaryDark: "#4320B5", PrimarySurface: "#E9E2FF", OnPrimary: "#FFFFFF", Warning: "#8A5A00",
        Feedback: new("#1A7A40", "#DDF3E5", "#C62839", "#FBE1E4"), AdaptedFeedback: AdaptedLight,
        CodeBackground: "#F0F2F8", CodeText: "#1A1D2E", CodeKeyword: "#7A2FB0", CodeString: "#2E6B30",
        CodeNumber: "#A8440F", CodeComment: "#5A6178", CodeType: "#1F5FBF");

    public static readonly Theme HighContrast = new(
        "contraste", "Contraste élevé", IsLight: false,
        Background: "#000000", Surface: "#0D0D0D", SurfaceRaised: "#1F1F1F", Stroke: "#FFFFFF",
        TextPrimary: "#FFFFFF", TextMuted: "#E6E6E6",
        Primary: "#FFE14D", PrimaryDark: "#E6C200", PrimarySurface: "#3A3200", OnPrimary: "#000000", Warning: "#FFE14D",
        Feedback: new("#5CFF8A", "#003D14", "#FF7A7A", "#4D0000"), AdaptedFeedback: new("#7CC8FF", "#00284D", "#FFB000", "#4D3500"),
        CodeBackground: "#000000", CodeText: "#FFFFFF", CodeKeyword: "#FF9CFF", CodeString: "#B8FF7A",
        CodeNumber: "#FFB27A", CodeComment: "#C8C8C8", CodeType: "#9CCBFF");

    public static readonly Theme Sepia = new(
        "sepia", "Sépia", IsLight: true,
        Background: "#F4ECD8", Surface: "#FBF5E6", SurfaceRaised: "#EFE4CC", Stroke: "#CDBC97",
        TextPrimary: "#3B2F1E", TextMuted: "#5F5038",
        Primary: "#8A4B1F", PrimaryDark: "#6E3A16", PrimarySurface: "#F0DCC8", OnPrimary: "#FFFFFF", Warning: "#7E5200",
        Feedback: new("#2F6B2F", "#E1EED9", "#A8322D", "#F6DCD6"), AdaptedFeedback: AdaptedLight with { SuccessSurface = "#DCE8F2", ErrorSurface = "#F6E2CC" },
        CodeBackground: "#EFE6D2", CodeText: "#3B2F1E", CodeKeyword: "#76378A", CodeString: "#3A621C",
        CodeNumber: "#8E430F", CodeComment: "#5F5038", CodeType: "#245489");

    public static IReadOnlyList<Theme> All { get; } = [Dark, Light, HighContrast, Sepia];

    public static Theme Get(string id) => All.FirstOrDefault(t => t.Id == id) ?? Dark;

    /// <summary>Rapport de contraste WCAG 2.x entre deux couleurs #RRGGBB (de 1 à 21).</summary>
    public static double ContrastRatio(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        static double Channel(string hex, int offset)
        {
            var c = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(hex, 1) + 0.7152 * Channel(hex, 3) + 0.0722 * Channel(hex, 5);
    }
}
