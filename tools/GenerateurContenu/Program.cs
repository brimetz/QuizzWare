using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// Génère la STRUCTURE du contenu à partir de tools/plan-des-sujets.txt (voir tools/README.md) :
//   - crée les nouvelles catégories et les chapitres « à venir » qui n'existent pas encore ;
//   - renumérote les fichiers de chapitres selon l'ordre du plan ;
//   - régénère entièrement les arbres de spécialisations (specialisations/arbre.json) ;
//   - régénère la carte des sujets (docs/CARTE.md).
// Il ne modifie JAMAIS un chapitre existant : le contenu écrit (sections, questions) est préservé.
//
// Usage, depuis n'importe quel dossier du dépôt :  dotnet run --project tools/GenerateurContenu

var root = FindRepositoryRoot();
var planPath = args.ElementAtOrDefault(0) ?? Path.Combine(root, "tools", "plan-des-sujets.txt");
var contentDir = args.ElementAtOrDefault(1) ?? Path.Combine(root, "src", "QuizzWare.Core", "Content");
var mapPath = args.ElementAtOrDefault(2) ?? Path.Combine(root, "docs", "CARTE.md");

var json = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
var categories = Parse(File.ReadAllLines(planPath));

foreach (var cat in categories)
{
    var folder = Path.Combine(contentDir, cat.Id);
    if (cat.IsNew && !File.Exists(Path.Combine(folder, "category.json")))
    {
        var meta = new JsonObject
        {
            ["id"] = cat.Id, ["order"] = cat.Order, ["name"] = cat.Name, ["description"] = cat.Description,
            ["icon"] = cat.Icon, ["color"] = cat.Color,
        };
        if (cat.Language is not null) meta["language"] = cat.Language;
        Write(Path.Combine(folder, "category.json"), meta);
    }
    else if (!Directory.Exists(folder))
        throw new InvalidOperationException($"Catégorie introuvable : {cat.Id}");

    // 1. Les chapitres existants sont mis de côté sous un nom temporaire, pour renuméroter sans collision.
    var existing = Directory.GetFiles(folder, "*.json")
        .Where(f => Path.GetFileName(f) != "category.json")
        .ToDictionary(f => JsonNode.Parse(File.ReadAllText(f))!["id"]!.GetValue<string>(), f => f);

    var unknown = existing.Keys.Except(cat.Chapters.Select(c => c.Id)).ToList();
    if (unknown.Count > 0)
        throw new InvalidOperationException($"{cat.Id} : chapitres présents mais absents du plan : {string.Join(", ", unknown)}");

    foreach (var (id, file) in existing.ToList())
    {
        File.Move(file, file + ".tmp");
        existing[id] = file + ".tmp";
    }

    // 2. Chaque chapitre du plan : on conserve le fichier existant (renuméroté), sinon on crée le chapitre « à venir ».
    foreach (var ch in cat.Chapters)
    {
        var target = Path.Combine(folder, $"{ch.Number:00}-{Slug(ch.Id)}.json");
        if (existing.TryGetValue(ch.Id, out var file))
            File.Move(file, target);
        else if (ch.Keep)
            throw new InvalidOperationException($"{cat.Id} : chapitre #keep introuvable : {ch.Id}");
        else
            Write(target, new JsonObject
            {
                ["id"] = ch.Id, ["title"] = ch.Title, ["summary"] = ch.Summary,
                ["lessons"] = new JsonArray(ch.Lessons.Select(l => (JsonNode)new JsonObject
                {
                    ["id"] = l.Id, ["title"] = l.Title, ["summary"] = l.Summary, ["difficulty"] = l.Difficulty,
                }).ToArray()),
            });

        // Relu depuis le fichier : c'est le contenu réel qui alimente la carte.
        var chapter = JsonNode.Parse(File.ReadAllText(target))!;
        ch.Title = chapter["title"]!.GetValue<string>();
        ch.Lessons.Clear();
        foreach (var l in chapter["lessons"]!.AsArray())
            ch.Lessons.Add(new LessonSpec(l!["id"]!.GetValue<string>(), l["title"]!.GetValue<string>(), "", 1, IsWritten(l)));
    }

    // 3. Arbre des spécialisations : entièrement régénéré depuis le plan.
    Write(Path.Combine(folder, "specialisations", "arbre.json"), new JsonObject
    {
        ["unlockedBy"] = cat.Unlock,
        ["nodes"] = new JsonArray(cat.Tree.Select(ToJson).ToArray()),
    });

    Console.WriteLine($"{cat.Id,-22} {cat.Chapters.Count,2} chapitres · {cat.Tree.Sum(f => f.Children.Count),2} spécialisations");
}

File.WriteAllText(mapPath, BuildMap(categories, contentDir));
Console.WriteLine($"Carte : {mapPath}");

// ---------------------------------------------------------------------------------------------

void Write(string path, JsonNode node)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, Unescape(node.ToJsonString(json)) + Environment.NewLine);
}

// System.Text.Json échappe les caractères hors du plan de base (emoji) : on les réécrit tels quels.
static string Unescape(string text) =>
    Regex.Replace(text, @"\\u(D[89AB][0-9A-F]{2})\\u(D[C-F][0-9A-F]{2})", m =>
        char.ConvertFromUtf32(char.ConvertToUtf32((char)Convert.ToInt32(m.Groups[1].Value, 16), (char)Convert.ToInt32(m.Groups[2].Value, 16))));

static bool IsWritten(JsonNode? lesson) => lesson?["questions"] is JsonArray { Count: > 0 };

static string Slug(string chapterId) => chapterId.Contains("-ch-") ? chapterId[(chapterId.IndexOf("-ch-") + 4)..] : chapterId;

static string FindRepositoryRoot()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        if (File.Exists(Path.Combine(dir.FullName, "QuizzWare.sln")))
            return dir.FullName;
    throw new InvalidOperationException("Lancer l'outil depuis le dépôt QuizzWare (QuizzWare.sln introuvable).");
}

static JsonNode ToJson(NodeSpec n)
{
    var o = new JsonObject { ["id"] = n.Id, ["title"] = n.Title, ["summary"] = n.Summary, ["icon"] = n.Icon, ["kind"] = n.Kind };
    if (n.Extras.TryGetValue("rel", out var rel)) o["relatedCategory"] = rel;
    if (n.Extras.TryGetValue("lang", out var lang)) o["language"] = lang;
    if (n.Extras.TryGetValue("link", out var link)) o["linkTo"] = link;
    if (n.Children.Count > 0) o["children"] = new JsonArray(n.Children.Select(ToJson).ToArray());
    return o;
}

static List<CategorySpec> Parse(string[] lines)
{
    var result = new List<CategorySpec>();
    CategorySpec? cat = null;
    NodeSpec? family = null;

    foreach (var raw in lines)
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith("//")) continue;

        static string[] Parts(string s) => s.Split('|').Select(p => p.Trim()).ToArray();
        var head = line.Split(' ', 2);
        var rest = head.Length > 1 ? head[1] : "";

        switch (head[0])
        {
            case "@cat":
            {
                var p = Parts(rest);
                cat = new CategorySpec(p[0], true)
                {
                    Order = int.Parse(p[1]), Name = p[2], Icon = p[3], Color = p[4],
                    Language = p[5] == "-" ? null : p[5], Description = p[6],
                };
                result.Add(cat);
                break;
            }
            case "@existing":
                cat = new CategorySpec(rest.Trim(), false);
                result.Add(cat);
                break;
            case "@unlock":
                cat!.Unlock = rest.Trim();
                break;
            case "#keep":
            {
                var p = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                cat!.Chapters.Add(new ChapterSpec(int.Parse(p[0]), p[1], true));
                break;
            }
            case "#ch":
            {
                var num = rest.Split(' ', 2);
                var p = Parts(num[1]);
                cat!.Chapters.Add(new ChapterSpec(int.Parse(num[0]), p[0], false) { Title = p[1], Summary = p[2] });
                break;
            }
            case "-":
            {
                var p = Parts(rest);
                cat!.Chapters[^1].Lessons.Add(new LessonSpec(p[0], p[1], p[2], int.Parse(p[3]), false));
                break;
            }
            case "+":
            {
                var p = Parts(rest);
                family = new NodeSpec(p[0], p[1], p[2], p[3], "famille", [], []);
                cat!.Tree.Add(family);
                break;
            }
            case "*":
            {
                var p = Parts(rest);
                var extras = p.Skip(5).Select(e => e.Split('=', 2)).ToDictionary(e => e[0], e => e[1]);
                family!.Children.Add(new NodeSpec(p[0], p[1], p[2], p[3], p[4], extras, []));
                break;
            }
            default:
                throw new FormatException($"Ligne non reconnue : {line}");
        }
    }

    return result;
}

static string BuildMap(List<CategorySpec> categories, string contentDir)
{
    var names = new Dictionary<string, string>();
    foreach (var c in categories)
    {
        var meta = JsonNode.Parse(File.ReadAllText(Path.Combine(contentDir, c.Id, "category.json")))!;
        names[c.Id] = meta["name"]!.GetValue<string>();
        c.Order = meta["order"]!.GetValue<int>();
    }

    // Une spécialisation est « écrite » si son parcours contient au moins une leçon avec des questions.
    bool HasTrack(string categoryId, string nodeId)
    {
        var dir = Path.Combine(contentDir, categoryId, "specialisations", nodeId);
        return Directory.Exists(dir) && Directory.GetFiles(dir, "*.json")
            .Any(f => JsonNode.Parse(File.ReadAllText(f))!["lessons"]!.AsArray().Any(IsWritten));
    }

    static string Status(ChapterSpec ch) =>
        ch.Lessons.All(l => l.Written) ? "✅" : ch.Lessons.Any(l => l.Written) ? "🟡" : "🕓";

    var chapters = categories.Sum(c => c.Chapters.Count);
    var written = categories.Sum(c => c.Chapters.Count(ch => ch.Lessons.All(l => l.Written)));
    var lessons = categories.Sum(c => c.Chapters.Sum(ch => ch.Lessons.Count));
    var writtenLessons = categories.Sum(c => c.Chapters.Sum(ch => ch.Lessons.Count(l => l.Written)));
    var leaves = categories.SelectMany(c => c.Tree.SelectMany(f => f.Children.Select(n => (c, n)))).ToList();
    var doorCount = leaves.Count(x => x.n.Kind == "porte");
    var linkCount = leaves.Count(x => x.n.Kind != "porte" && x.n.Extras.ContainsKey("link"));
    var specs = leaves.Count - doorCount - linkCount;
    var tracks = leaves.Count(x => x.n.Kind != "porte" && !x.n.Extras.ContainsKey("link") && HasTrack(x.c.Id, x.n.Id));

    var sb = new StringBuilder();
    sb.AppendLine("# Carte des sujets");
    sb.AppendLine();
    sb.AppendLine("Le grand arbre de QuizzWare : chaque catégorie a un **tronc commun** (chapitres) et un **arbre de spécialisations**.");
    sb.AppendLine("Fichier généré par `tools/GenerateurContenu` à partir du plan et du contenu : ne pas modifier à la main.");
    sb.AppendLine();
    sb.AppendLine("✅ écrit · 🟡 en partie écrit · 🕓 à venir · ↪ renvoi vers le parcours d'une autre catégorie · 🚪 porte vers une autre catégorie");
    sb.AppendLine();
    sb.AppendLine("| Catégories | Chapitres de tronc commun | Leçons de tronc commun | Spécialisations | Renvois | Portes |");
    sb.AppendLine("|---|---|---|---|---|---|");
    sb.AppendLine($"| {categories.Count} | {chapters} ({written} écrits) | {lessons} ({writtenLessons} écrites) | {specs} ({tracks} écrites) | {linkCount} | {doorCount} |");
    sb.AppendLine();

    foreach (var c in categories.OrderBy(c => c.Order))
    {
        sb.AppendLine($"## {names[c.Id]}");
        sb.AppendLine();
        sb.AppendLine("**Tronc commun**");
        sb.AppendLine();
        foreach (var ch in c.Chapters.OrderBy(ch => ch.Number))
        {
            sb.AppendLine($"{ch.Number}. {Status(ch)} {ch.Title}");
            foreach (var l in ch.Lessons)
                sb.AppendLine($"   - {(l.Written ? "✅ " : "")}{l.Title}");
        }
        sb.AppendLine();
        sb.AppendLine("**Spécialisations**");
        sb.AppendLine();
        foreach (var f in c.Tree)
        {
            sb.AppendLine($"- {f.Title}");
            foreach (var n in f.Children)
            {
                var mark = n.Kind == "porte" ? $"🚪 {n.Title} → catégorie {names.GetValueOrDefault(n.Extras["link"], n.Extras["link"])}"
                    : n.Extras.TryGetValue("link", out var link) ? $"↪ {n.Title} → {names.GetValueOrDefault(link.Split('/')[0], link)}"
                    : HasTrack(c.Id, n.Id) ? $"✅ {n.Title}"
                    : $"🕓 {n.Title}";
                sb.AppendLine($"  - {mark}");
            }
        }
        sb.AppendLine();
    }

    return sb.ToString();
}

sealed class CategorySpec(string id, bool isNew)
{
    public string Id { get; } = id;
    public bool IsNew { get; } = isNew;
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public string? Language { get; set; }
    public string Description { get; set; } = "";
    public string? Unlock { get; set; }
    public List<ChapterSpec> Chapters { get; } = [];
    public List<NodeSpec> Tree { get; } = [];
}

sealed class ChapterSpec(int number, string id, bool keep)
{
    public int Number { get; } = number;
    public string Id { get; } = id;
    public bool Keep { get; } = keep;
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<LessonSpec> Lessons { get; } = [];
}

sealed record LessonSpec(string Id, string Title, string Summary, int Difficulty, bool Written);
sealed record NodeSpec(string Id, string Title, string Summary, string Icon, string Kind, Dictionary<string, string> Extras, List<NodeSpec> Children);
