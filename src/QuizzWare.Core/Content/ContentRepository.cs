using System.Text.Json;
using QuizzWare.Core.Models;

namespace QuizzWare.Core.Content;

public sealed record QuestionLocation(Category Category, Chapter Chapter, IQuizUnit Unit, Question Question);

/// <summary>Accès en lecture seule aux catégories, chapitres, leçons et défis.</summary>
public sealed class ContentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        AllowOutOfOrderMetadataProperties = true, // "type" n'a pas besoin d'être le premier champ d'une question
    };

    private readonly Dictionary<string, Category> _byId;
    private readonly Dictionary<string, QuestionLocation> _questions;

    public ContentRepository(IEnumerable<Category> categories)
    {
        Categories = categories.OrderBy(c => c.Order).ToList();
        ContentValidator.Validate(Categories);
        _byId = Categories.ToDictionary(c => c.Id);
        _questions = Categories
            .SelectMany(c => c.AllChapters.SelectMany(ch => Units(ch).Select(u => (Category: c, Chapter: ch, Unit: u))))
            .SelectMany(x => x.Unit.Questions.Select(q => new QuestionLocation(x.Category, x.Chapter, x.Unit, q)))
            .ToDictionary(l => l.Question.Id);
    }

    private static IEnumerable<IQuizUnit> Units(Chapter chapter) =>
        chapter.Challenge is { } challenge ? chapter.Lessons.Append<IQuizUnit>(challenge) : chapter.Lessons;

    /// <summary>Retrouve une question (et sa catégorie, sa leçon ou son défi) : utile aux révisions, qui mélangent les catégories.</summary>
    public QuestionLocation? FindQuestion(string questionId) => _questions.GetValueOrDefault(questionId);

    public IReadOnlyList<Category> Categories { get; }

    public Category GetCategory(string categoryId) =>
        _byId.TryGetValue(categoryId, out var category)
            ? category
            : throw new KeyNotFoundException($"Catégorie inconnue : {categoryId}");

    public Lesson GetLesson(string categoryId, string lessonId) =>
        GetCategory(categoryId).AllLessons.FirstOrDefault(l => l.Id == lessonId)
            ?? throw new KeyNotFoundException($"Leçon inconnue : {categoryId}/{lessonId}");

    /// <summary>Retrouve une leçon ou un défi par son identifiant.</summary>
    public IQuizUnit GetUnit(string categoryId, string unitId)
    {
        var category = GetCategory(categoryId);
        return (IQuizUnit?)category.AllLessons.FirstOrDefault(l => l.Id == unitId)
            ?? category.AllChapters.Select(c => c.Challenge).FirstOrDefault(c => c?.Id == unitId)
            ?? throw new KeyNotFoundException($"Leçon ou défi inconnu : {categoryId}/{unitId}");
    }

    /// <summary>Nœud de spécialisation dont le parcours contient cette leçon ou ce défi (null : tronc commun).</summary>
    public SpecializationNode? FindSpecialization(string categoryId, string unitId) =>
        GetCategory(categoryId).Specializations?.AllNodes.FirstOrDefault(n =>
            n.Track.Any(c => c.Challenge?.Id == unitId || c.Lessons.Any(l => l.Id == unitId)));

    /// <summary>Langage du code d'une leçon ou d'un défi : celui de sa spécialisation, sinon celui de la catégorie.</summary>
    public string? GetCodeLanguage(string categoryId, string? unitId) =>
        (unitId is null ? null : FindSpecialization(categoryId, unitId)?.Language) ?? GetCategory(categoryId).Language;

    /// <summary>Suit un éventuel renvoi (<see cref="SpecializationNode.LinkTo"/>) : renvoie la catégorie et le nœud qui portent réellement le parcours.</summary>
    public (Category Category, SpecializationNode Node) Resolve(Category category, SpecializationNode node)
    {
        if (!node.IsLink || node.LinkTo is not { } link)
            return (category, node);

        var (categoryId, nodeId) = ContentValidator.SplitLink(link);
        return (GetCategory(categoryId), GetSpecialization(categoryId, nodeId));
    }

    public SpecializationNode GetSpecialization(string categoryId, string nodeId) =>
        GetCategory(categoryId).Specializations?.AllNodes.FirstOrDefault(n => n.Id == nodeId)
            ?? throw new KeyNotFoundException($"Spécialisation inconnue : {categoryId}/{nodeId}");

    public const string CategoryFileName = "category.json";
    public const string SpecializationsFolder = "specialisations/";
    public const string TreeFileName = "arbre.json";

    /// <summary>
    /// Charge le contenu embarqué dans l'assembly. Deux formats coexistent :
    /// <list type="bullet">
    /// <item><c>Content/&lt;categorie&gt;.json</c> : la catégorie complète dans un seul fichier ;</item>
    /// <item><c>Content/&lt;categorie&gt;/category.json</c> (sans chapitres) + un fichier par chapitre, pris dans l'ordre alphabétique.
    /// Optionnellement, <c>specialisations/arbre.json</c> décrit l'arbre des spécialisations, et
    /// <c>specialisations/&lt;id du nœud&gt;/*.json</c> les chapitres du parcours de chaque nœud.</item>
    /// </list>
    /// </summary>
    public static ContentRepository LoadEmbedded()
    {
        var assembly = typeof(ContentRepository).Assembly;
        var files = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("Content/", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Select(name => (Resource: name, Path: name["Content/".Length..].Replace('\\', '/')))
            .ToList();

        T Read<T>(string resource)
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            try
            {
                return JsonSerializer.Deserialize<T>(stream, JsonOptions) ?? throw new InvalidDataException($"Contenu vide : {resource}");
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"JSON invalide dans {resource} : {ex.Message}", ex);
            }
        }

        var singleFiles = files.Where(f => !f.Path.Contains('/')).Select(f => Read<Category>(f.Resource));

        var folders = files.Where(f => f.Path.Contains('/')).GroupBy(f => f.Path[..f.Path.IndexOf('/')]).Select(folder =>
        {
            // Chemin relatif au dossier de la catégorie : "category.json", "01-sql.json", "specialisations/…"
            var entries = folder.Select(f => (f.Resource, Relative: f.Path[(folder.Key.Length + 1)..])).ToList();

            var metadata = entries.FirstOrDefault(e => e.Relative == CategoryFileName);
            if (metadata.Resource is null)
                throw new InvalidDataException($"Le dossier de contenu {folder.Key} n'a pas de {CategoryFileName}.");

            List<Chapter> ReadChapters(IEnumerable<(string Resource, string Relative)> chapterFiles) =>
                chapterFiles.OrderBy(e => e.Relative, StringComparer.Ordinal).Select(e => Read<Chapter>(e.Resource)).ToList();

            var category = Read<Category>(metadata.Resource) with
            {
                Chapters = ReadChapters(entries.Where(e => e != metadata && !e.Relative.Contains('/'))),
            };

            var tree = entries.FirstOrDefault(e => e.Relative == SpecializationsFolder + TreeFileName);
            if (tree.Resource is null)
                return category;

            // Chapitres de chaque nœud : specialisations/<id du nœud>/<chapitre>.json
            var tracks = entries
                .Where(e => e.Relative.StartsWith(SpecializationsFolder, StringComparison.Ordinal) && e != tree)
                .GroupBy(e => e.Relative[SpecializationsFolder.Length..].Split('/')[0])
                .ToDictionary(g => g.Key, g => ReadChapters(g));

            var specializations = Read<SpecializationTree>(tree.Resource);
            var unknown = tracks.Keys.Except(specializations.AllNodes.Select(n => n.Id)).ToList();
            if (unknown.Count > 0)
                throw new InvalidDataException($"{folder.Key} : dossiers de spécialisation sans nœud dans {TreeFileName} : {string.Join(", ", unknown)}");

            return category with { Specializations = specializations with { Nodes = AttachTracks(specializations.Nodes, tracks) } };
        });

        return new ContentRepository(singleFiles.Concat(folders));
    }

    private static List<SpecializationNode> AttachTracks(IEnumerable<SpecializationNode> nodes, IReadOnlyDictionary<string, List<Chapter>> tracks) =>
        nodes.Select(n => n with
        {
            Chapters = tracks.TryGetValue(n.Id, out var chapters) ? chapters : n.Chapters,
            Children = n.Children is null ? null : AttachTracks(n.Children, tracks),
        }).ToList();

    public static Category ParseCategory(string json) =>
        JsonSerializer.Deserialize<Category>(json, JsonOptions)
            ?? throw new InvalidDataException("Contenu vide.");
}
