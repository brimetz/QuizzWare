namespace QuizzWare.Core.Appearance;

/// <summary>Réglages d'affichage et d'accessibilité, propres à l'appareil (non liés à la progression).</summary>
public sealed record AppearanceSettings
{
    public string Theme { get; init; } = Themes.Dark.Id;

    /// <summary>Correction en bleu / orange au lieu de vert / rouge, distinguable avec les daltonismes les plus courants.</summary>
    public bool AdaptedFeedback { get; init; }

    public string Font { get; init; } = AppFonts.System;

    /// <summary>Facteur appliqué aux textes (énoncés, réponses, cours, boutons).</summary>
    public double TextScale { get; init; } = 1.0;

    /// <summary>Facteur appliqué aux extraits de code, réglable à part : une ligne de code trop grande défile.</summary>
    public double CodeScale { get; init; } = 1.0;

    /// <summary>Supprime les animations de correction et de bilan.</summary>
    public bool ReduceMotion { get; init; }

    public static IReadOnlyList<double> TextScales { get; } = [0.9, 1.0, 1.15, 1.3, 1.5];
    public static IReadOnlyList<double> CodeScales { get; } = [0.85, 1.0, 1.15, 1.3];

    /// <summary>Corrige des valeurs inattendues (ancienne version, fichier modifié) au lieu d'échouer.</summary>
    public AppearanceSettings Normalized() => this with
    {
        Theme = Themes.All.Any(t => t.Id == Theme) ? Theme : Themes.Dark.Id,
        Font = AppFonts.All.Contains(Font) ? Font : AppFonts.System,
        TextScale = Math.Clamp(TextScale, TextScales[0], TextScales[^1]),
        CodeScale = Math.Clamp(CodeScale, CodeScales[0], CodeScales[^1]),
    };
}

/// <summary>Polices proposées. Les fichiers sont embarqués dans l'application (Resources/Fonts).</summary>
public static class AppFonts
{
    public const string System = "systeme";
    /// <summary>Atkinson Hyperlegible : conçue par le Braille Institute pour les personnes malvoyantes.</summary>
    public const string Hyperlegible = "atkinson";
    /// <summary>OpenDyslexic : lettres lestées en bas, pensée pour certaines personnes dyslexiques.</summary>
    public const string Dyslexic = "opendyslexic";

    public static IReadOnlyList<string> All { get; } = [System, Hyperlegible, Dyslexic];
}
