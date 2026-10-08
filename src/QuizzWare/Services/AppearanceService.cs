using System.Text.Json;
using QuizzWare.Core.Appearance;
using QuizzWare.ViewModels;

namespace QuizzWare.Services;

/// <summary>
/// Applique les réglages d'accessibilité (thème, couleurs de correction, police, tailles, animations) à toute l'application.
/// Les pages lisent couleurs, polices et tailles par <c>DynamicResource</c> : elles se mettent à jour immédiatement.
/// Les réglages sont propres à l'appareil (Preferences), indépendants de la progression.
/// </summary>
public sealed class AppearanceService
{
    private const string PreferenceKey = "apparence";

    /// <summary>Tailles de base (facteur 1), par rôle. Clés des ressources dynamiques : « FontSize » + rôle.</summary>
    private static readonly Dictionary<string, double> BaseSizes = new()
    {
        ["Small"] = 13,
        ["Muted"] = 14,
        ["Body"] = 15,
        ["Reading"] = 16,
        ["Button"] = 16,
        ["SubHeader"] = 17,
        ["Header"] = 20,
        ["Title"] = 30,
    };

    private const double BaseCodeSize = 13;

    public AppearanceSettings Current { get; private set; } = new();

    public event EventHandler<AppearanceSettings>? Changed;

    /// <summary>Applique les réglages enregistrés : à appeler au démarrage, avant d'afficher la première page.</summary>
    public void ApplySaved()
    {
        AppearanceSettings saved;
        try
        {
            var json = Preferences.Default.Get(PreferenceKey, string.Empty);
            saved = string.IsNullOrEmpty(json) ? new() : JsonSerializer.Deserialize<AppearanceSettings>(json) ?? new();
        }
        catch (JsonException)
        {
            saved = new();
        }
        Apply(saved, save: false);
    }

    public void Update(Func<AppearanceSettings, AppearanceSettings> change) => Apply(change(Current), save: true);

    private void Apply(AppearanceSettings settings, bool save)
    {
        Current = settings.Normalized();
        var theme = Themes.Get(Current.Theme);
        var feedback = theme.FeedbackFor(Current.AdaptedFeedback);
        var codeSize = Math.Round(BaseCodeSize * Current.CodeScale);

        Palette.Apply(theme, Current.AdaptedFeedback, codeSize);

        if (Application.Current is { } app)
        {
            var r = app.Resources;
            void Set(string key, string hex) => r[key] = Color.FromArgb(hex);

            Set("Primary", theme.Primary);
            Set("PrimaryDark", theme.PrimaryDark);
            Set("PrimarySurface", theme.PrimarySurface);
            Set("OnPrimary", theme.OnPrimary);
            Set("Background", theme.Background);
            Set("Surface", theme.Surface);
            Set("SurfaceRaised", theme.SurfaceRaised);
            Set("Stroke", theme.Stroke);
            Set("TextPrimary", theme.TextPrimary);
            Set("TextMuted", theme.TextMuted);
            Set("Warning", theme.Warning);
            Set("Success", feedback.Success);
            Set("Error", feedback.Error);
            Set("CodeBackground", theme.CodeBackground);
            Set("CodeText", theme.CodeText);
            Set("CodeString", theme.CodeString);
            r["PrimaryBrush"] = new SolidColorBrush(Color.FromArgb(theme.Primary));
            r["SurfaceBrush"] = new SolidColorBrush(Color.FromArgb(theme.Surface));

            foreach (var (role, size) in BaseSizes)
                r["FontSize" + role] = Math.Round(size * Current.TextScale);
            r["FontSizeCode"] = codeSize;
            r["AppFont"] = FontFamilyFor(Current.Font);

            // Contrôles natifs (sélecteurs, barre de statut) : clair ou sombre selon le thème.
            app.UserAppTheme = theme.IsLight ? AppTheme.Light : AppTheme.Dark;
        }

        if (save)
            Preferences.Default.Set(PreferenceKey, JsonSerializer.Serialize(Current));
        Changed?.Invoke(this, Current);
    }

    /// <summary>Alias déclarés dans MauiProgram (ConfigureFonts) ; chaîne vide = police du système.</summary>
    public static string FontFamilyFor(string font) => font switch
    {
        AppFonts.Hyperlegible => "Atkinson",
        AppFonts.Dyslexic => "OpenDyslexic",
        _ => string.Empty,
    };
}
