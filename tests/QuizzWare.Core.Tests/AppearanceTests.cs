using QuizzWare.Core.Appearance;

namespace QuizzWare.Core.Tests;

/// <summary>Chaque thème doit rester lisible : contrastes WCAG vérifiés pour le texte, le code et la correction.</summary>
public class AppearanceTests
{
    public static TheoryData<string> ThemeIds => new(Themes.All.Select(t => t.Id));

    private static void AssertContrast(string fg, string bg, double minimum, string what)
    {
        var ratio = Themes.ContrastRatio(fg, bg);
        Assert.True(ratio >= minimum, $"{what} : contraste {ratio:0.00}:1 ({fg} sur {bg}), minimum {minimum}:1");
    }

    [Theory]
    [MemberData(nameof(ThemeIds))]
    public void Text_IsReadable(string id)
    {
        var t = Themes.Get(id);
        var minimum = id == Themes.HighContrast.Id ? 7 : 4.5;
        foreach (var (surface, name) in new[] { (t.Background, "fond"), (t.Surface, "carte"), (t.SurfaceRaised, "carte surélevée") })
        {
            AssertContrast(t.TextPrimary, surface, minimum, $"{id} texte sur {name}");
            AssertContrast(t.TextMuted, surface, 4.5, $"{id} texte secondaire sur {name}");
        }
        AssertContrast(t.OnPrimary, t.Primary, 4.5, $"{id} texte des boutons");
        AssertContrast(t.Warning, t.Surface, 4.5, $"{id} avertissement");
        AssertContrast(t.TextPrimary, t.PrimarySurface, 4.5, $"{id} réponse sélectionnée");
    }

    [Theory]
    [MemberData(nameof(ThemeIds))]
    public void Code_IsReadable(string id)
    {
        var t = Themes.Get(id);
        foreach (var (color, name) in new[]
                 {
                     (t.CodeText, "texte"), (t.CodeKeyword, "mot-clé"), (t.CodeString, "chaîne"),
                     (t.CodeNumber, "nombre"), (t.CodeComment, "commentaire"), (t.CodeType, "type"),
                 })
            AssertContrast(color, t.CodeBackground, 4.5, $"{id} code {name}");
    }

    [Theory]
    [MemberData(nameof(ThemeIds))]
    public void Feedback_IsVisible_InBothPalettes(string id)
    {
        var t = Themes.Get(id);
        foreach (var adapted in new[] { false, true })
        {
            var f = t.FeedbackFor(adapted);
            var label = adapted ? "adaptée" : "standard";
            // Titre de la correction (« Bonne réponse ! ») écrit dans la couleur, sur une carte.
            AssertContrast(f.Success, t.Surface, 4.5, $"{id} {label} succès");
            AssertContrast(f.Error, t.Surface, 4.5, $"{id} {label} erreur");
            // Réponse colorée : le texte reste lisible sur son fond, et le contour se voit.
            AssertContrast(t.TextPrimary, f.SuccessSurface, 4.5, $"{id} {label} texte sur fond de succès");
            AssertContrast(t.TextPrimary, f.ErrorSurface, 4.5, $"{id} {label} texte sur fond d'erreur");
            AssertContrast(f.Success, f.SuccessSurface, 3, $"{id} {label} contour de succès");
            AssertContrast(f.Error, f.ErrorSurface, 3, $"{id} {label} contour d'erreur");
        }
    }

    [Fact]
    public void ContrastRatio_MatchesWcagReferences()
    {
        Assert.Equal(21, Themes.ContrastRatio("#000000", "#FFFFFF"), 2);
        Assert.Equal(1, Themes.ContrastRatio("#777777", "#777777"), 2);
        Assert.Equal(4.48, Themes.ContrastRatio("#777777", "#FFFFFF"), 2);
    }

    [Fact]
    public void Normalized_RepairsUnexpectedValues()
    {
        var s = new AppearanceSettings { Theme = "inconnu", Font = "comic", TextScale = 9, CodeScale = 0.1 }.Normalized();

        Assert.Equal(Themes.Dark.Id, s.Theme);
        Assert.Equal(AppFonts.System, s.Font);
        Assert.Equal(1.5, s.TextScale);
        Assert.Equal(0.85, s.CodeScale);
    }
}
