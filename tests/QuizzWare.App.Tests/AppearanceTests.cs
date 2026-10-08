using QuizzWare.Core.Appearance;
using QuizzWare.Services;
using QuizzWare.ViewModels;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.App.Tests;

/// <summary>
/// Accessibilité : les réglages sont appliqués aux couleurs et tailles utilisées par le code, et enregistrés sur l'appareil.
/// Palette est statique : ces tests ne s'exécutent pas en parallèle d'autres qui la lisent.
/// </summary>
[Collection(PaletteCollection.Name)]
public class AppearanceTests : IDisposable
{
    private readonly FakePreferences _preferences = new();
    private readonly AppearanceService _service;

    public AppearanceTests()
    {
        _service = new AppearanceService(_preferences);
        _service.ApplySaved();
    }

    public void Dispose() => new AppearanceService(new FakePreferences()).ApplySaved(); // thème par défaut pour les autres tests

    [Fact]
    public void Defaults_AreTheDarkTheme()
    {
        Assert.Equal(new AppearanceSettings(), _service.Current);
        Assert.Equal(Color.FromArgb(Themes.Dark.Surface), Palette.Surface);
    }

    [Fact]
    public void Update_AppliesThemeAndFeedbackColors_AndSavesThem()
    {
        _service.Update(s => s with { Theme = Themes.Light.Id, AdaptedFeedback = true, CodeScale = 1.3 });

        Assert.Equal(Color.FromArgb(Themes.Light.Surface), Palette.Surface);
        Assert.Equal(Color.FromArgb(Themes.Light.AdaptedFeedback.Success), Palette.Success);
        Assert.Equal(17, Palette.CodeFontSize);

        var reloaded = new AppearanceService(_preferences);
        reloaded.ApplySaved();
        Assert.Equal(Themes.Light.Id, reloaded.Current.Theme);
        Assert.True(reloaded.Current.AdaptedFeedback);
    }

    [Fact]
    public void CorruptedPreferences_FallBackToDefaults()
    {
        _preferences.Set("apparence", "{ pas du json");
        var service = new AppearanceService(_preferences);

        service.ApplySaved();

        Assert.Equal(new AppearanceSettings(), service.Current);
    }

    [Fact]
    public void Highlighter_UsesThemeColors_AndCodeSize()
    {
        _service.Update(s => s with { Theme = Themes.HighContrast.Id, CodeScale = 1.15 });

        var line = SyntaxHighlighter.Highlight("return 42; // fin", "csharp");

        Assert.Contains(line.Spans, s => s.Text == "return" && s.TextColor == Palette.CodeKeyword);
        Assert.Contains(line.Spans, s => s.Text == "42" && s.TextColor == Palette.CodeNumber);
        Assert.Contains(line.Spans, s => s.Text == "// fin" && s.TextColor == Palette.CodeComment);
        Assert.All(line.Spans, s => Assert.Equal(15, s.FontSize));
    }

    [Fact]
    public void AccessibilityPage_SelectsOptionsAndResets()
    {
        var vm = new AccessibilityViewModel(_service);
        vm.Load();
        Assert.True(vm.ThemeOptions.Single(o => o.Id == Themes.Dark.Id).IsSelected);

        vm.ThemeOptions.Single(o => o.Id == Themes.Sepia.Id).TapCommand.Execute(null);
        vm.Fonts.Single(o => o.Id == AppFonts.Dyslexic).TapCommand.Execute(null);
        vm.ReduceMotion = true;

        Assert.Equal(Themes.Sepia.Id, _service.Current.Theme);
        Assert.Equal(AppFonts.Dyslexic, _service.Current.Font);
        Assert.True(_service.Current.ReduceMotion);
        Assert.True(vm.ThemeOptions.Single(o => o.Id == Themes.Sepia.Id).IsSelected);
        Assert.Equal("✓", vm.ThemeOptions.Single(o => o.Id == Themes.Sepia.Id).SelectedMark);
        Assert.Equal(Palette.SuccessSurface, vm.PreviewCorrectBackground); // l'aperçu suit le thème

        vm.ResetCommand.Execute(null);
        Assert.Equal(new AppearanceSettings(), _service.Current);
        Assert.False(vm.ReduceMotion);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PaletteCollection
{
    public const string Name = "Palette";
}
