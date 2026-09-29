using System.Text.Json.Serialization;

namespace QuizzWare.Core.Models;

/// <summary>
/// Arbre des spécialisations d'une catégorie : des familles (ex. « Bases document »),
/// qui mènent à des technologies ou des bibliothèques (ex. MongoDB, Entity Framework Core).
/// </summary>
/// <param name="UnlockedBy">Chapitre du tronc commun à terminer pour ouvrir l'arbre (null : ouvert d'emblée).</param>
public sealed record SpecializationTree(string? UnlockedBy, IReadOnlyList<SpecializationNode> Nodes)
{
    /// <summary>Tous les nœuds, en profondeur d'abord.</summary>
    [JsonIgnore]
    public IEnumerable<SpecializationNode> AllNodes => Nodes.SelectMany(n => n.SelfAndDescendants);
}

public static class SpecializationKind
{
    public const string Family = "famille";
    public const string Technology = "techno";
    public const string Library = "lib";
    /// <summary>Ni une technologie ni une bibliothèque : une approche, un domaine (DDD, TLS, system design…).</summary>
    public const string Concept = "concept";
    /// <summary>Porte vers une autre catégorie : <see cref="SpecializationNode.LinkTo"/> contient l'id de la catégorie.</summary>
    public const string Door = "porte";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Family, Technology, Library, Concept, Door };
}

/// <param name="Kind">« famille », « techno », « lib » ou « concept » (voir <see cref="SpecializationKind"/>).</param>
/// <param name="Chapters">Parcours propre au nœud ; vide ou absent : « bientôt disponible ».</param>
/// <param name="RelatedCategory">Catégorie liée (ex. « csharp » pour Entity Framework Core).</param>
/// <param name="Language">Langage du code de ce parcours, s'il diffère de celui de la catégorie.</param>
/// <param name="LinkTo">
/// Renvoi vers le nœud d'une autre catégorie, au format « categorie/noeud » (ex. « bases-de-donnees/db-spe-efcore ») :
/// le parcours n'est écrit qu'une fois mais apparaît dans les deux arbres.
/// </param>
public sealed record SpecializationNode(
    string Id,
    string Title,
    string Summary,
    string Icon,
    string Kind,
    IReadOnlyList<SpecializationNode>? Children = null,
    IReadOnlyList<Chapter>? Chapters = null,
    string? RelatedCategory = null,
    string? Language = null,
    string? LinkTo = null)
{
    /// <summary>Renvoi vers le parcours d'un autre nœud (les portes, qui mènent à une catégorie, ne sont pas des renvois).</summary>
    [JsonIgnore]
    public bool IsLink => LinkTo is not null && !IsDoor;

    [JsonIgnore]
    public bool IsDoor => Kind == SpecializationKind.Door;

    [JsonIgnore]
    public IReadOnlyList<SpecializationNode> ChildNodes => Children ?? [];

    [JsonIgnore]
    public IReadOnlyList<Chapter> Track => Chapters ?? [];

    /// <summary>Le parcours contient au moins une leçon jouable.</summary>
    [JsonIgnore]
    public bool HasTrack => Track.SelectMany(c => c.Lessons).Any(l => !l.IsPlanned);

    [JsonIgnore]
    public IEnumerable<SpecializationNode> SelfAndDescendants => ChildNodes.SelectMany(c => c.SelfAndDescendants).Prepend(this);
}
