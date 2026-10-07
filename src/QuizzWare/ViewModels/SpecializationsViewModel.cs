using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

/// <summary>
/// Arbre des spécialisations d'une catégorie, aplati en lignes pour l'affichage :
/// chaque ligne connaît les traits verticaux à dessiner à sa gauche pour relier parents et enfants.
/// </summary>
public partial class SpecializationsViewModel(ContentRepository content, ProgressService progressService)
    : ObservableObject, IQueryAttributable
{
    private string? _categoryId;

    /// <summary>Choix explicites de l'utilisateur (déplié / replié), prioritaires sur l'état par défaut.</summary>
    private readonly Dictionary<string, bool> _userExpanded = [];

    /// <summary>Toutes les lignes de l'arbre, avec leurs ancêtres : une ligne n'est affichée que si tous sont dépliés.</summary>
    private List<(TreeRowViewModel Row, IReadOnlyList<TreeRowViewModel> Ancestors)> _allRows = [];

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string intro = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<TreeRowViewModel> rows = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("categoryId", out var id))
            _categoryId = id as string;
    }

    public async Task LoadAsync()
    {
        if (_categoryId is null)
            return;

        var category = content.GetCategory(_categoryId);
        if (category.Specializations is not { } tree)
            return;

        var progress = await progressService.GetProgressAsync();
        var unlocked = progressService.AreSpecializationsUnlocked(progress, category);
        var unlockingChapter = category.Chapters.FirstOrDefault(c => c.Id == tree.UnlockedBy)?.Title;
        var lockedMessage = $"Termine le chapitre « {unlockingChapter} » du tronc commun pour débloquer les spécialisations.";
        var lockedText = $"🔒 Termine « {unlockingChapter} » pour débloquer";

        Title = "Spécialisations";
        Intro = unlocked
            ? "Choisis une famille, puis une technologie, une bibliothèque ou un concept à approfondir."
            : lockedText;

        var main = category.MainLessons.ToList();
        var completed = progress.Categories.GetValueOrDefault(category.Id)?.CompletedLessons ?? [];
        var rowList = new List<(TreeRowViewModel, IReadOnlyList<TreeRowViewModel>)>
        {
            (TreeRowViewModel.Root(category, $"Tronc commun · {main.Count(l => completed.Contains(l.Id))}/{main.Count} leçons"), []),
        };

        void Visit(SpecializationNode node, IReadOnlyList<bool> guides, bool isLast, IReadOnlyList<TreeRowViewModel> ancestors)
        {
            var row = CreateRow(category, node, progress, unlocked, lockedMessage, guides, isLast);
            if (node.ChildNodes.Count > 0)
            {
                // Par défaut : déplié si le sous-arbre est en cours ou terminé, replié s'il est encore inconnu.
                row.ExpandKey = node.Id;
                row.HiddenCount = node.SelfAndDescendants.Count() - 1;
                row.IsExpanded = _userExpanded.TryGetValue(node.Id, out var choice) ? choice : row.HasProgress;
                row.OnToggle = ToggleRow;
            }
            rowList.Add((row, ancestors));

            var childGuides = guides.Append(!isLast).ToList();
            var childAncestors = ancestors.Append(row).ToList();
            for (var i = 0; i < node.ChildNodes.Count; i++)
                Visit(node.ChildNodes[i], childGuides, i == node.ChildNodes.Count - 1, childAncestors);
        }

        for (var i = 0; i < tree.Nodes.Count; i++)
            Visit(tree.Nodes[i], [], i == tree.Nodes.Count - 1, []);

        _allRows = rowList;
        UpdateVisibleRows();
    }

    private void ToggleRow(TreeRowViewModel row)
    {
        if (row.ExpandKey is null)
            return;
        row.IsExpanded = !row.IsExpanded;
        _userExpanded[row.ExpandKey] = row.IsExpanded;
        UpdateVisibleRows();
    }

    private void UpdateVisibleRows() =>
        Rows = _allRows.Where(r => r.Ancestors.All(a => a.IsExpanded)).Select(r => r.Row).ToList();

    private TreeRowViewModel CreateRow(Category category, SpecializationNode node, UserProgress progress,
        bool unlocked, string lockedMessage, IReadOnlyList<bool> guides, bool isLast)
    {
        if (node.IsDoor)
            return CreateDoorRow(category, node, unlocked, lockedMessage, guides, isLast);

        var isFamily = node.Kind == SpecializationKind.Family;
        var doors = node.SelfAndDescendants.Count(n => n.IsDoor);

        // Parcours jouables sous ce nœud, en suivant les renvois vers d'autres catégories.
        var tracks = node.SelfAndDescendants
            .Where(n => n.ChildNodes.Count == 0)
            .Select(n => content.Resolve(category, n))
            .Where(t => t.Node.HasTrack)
            .Select(t => progressService.GetNodeProgress(progress, t.Category, t.Node))
            .ToList();
        var (done, total) = (tracks.Sum(t => t.Completed), tracks.Sum(t => t.Total));

        // Un renvoi dépend aussi du déblocage de l'arbre de sa catégorie d'origine.
        var (targetCategory, target) = content.Resolve(category, node);
        var targetLocked = node.IsLink && target.HasTrack && !progressService.AreSpecializationsUnlocked(progress, targetCategory);
        if (targetLocked)
        {
            var chapter = targetCategory.Chapters.FirstOrDefault(c => c.Id == targetCategory.Specializations?.UnlockedBy)?.Title;
            lockedMessage = $"Ce parcours appartient à la catégorie {targetCategory.Name} : termine son chapitre « {chapter} » pour le débloquer.";
        }

        string status;
        Color statusColor;
        if (!unlocked)
            (status, statusColor) = (isFamily ? "🔒 Verrouillé" : string.Empty, Palette.TextMuted);
        else if (isFamily && tracks.Count == 0 && doors > 0)
            (status, statusColor) = ($"🚪 {doors} catégorie{(doors > 1 ? "s" : "")} à explorer", Palette.TextMuted);
        else if (isFamily)
            (status, statusColor) = (tracks.Count == 0 ? "Bientôt disponible" : $"{tracks.Count} parcours · {done}/{total} leçons", Palette.TextMuted);
        else if (!target.HasTrack)
            (status, statusColor) = ("Bientôt disponible", Palette.TextMuted);
        else if (targetLocked)
            (status, statusColor) = ($"🔒 S'ouvre dans « {targetCategory.Name} »", Palette.TextMuted);
        else if (done == total)
            (status, statusColor) = ("✅ Parcours terminé", Palette.Success);
        else
            (status, statusColor) = (done == 0 ? "▶ Commencer" : $"▶ {done}/{total} leçons", Palette.Warning);

        var related = node.IsLink
            ? $"🔗 Parcours partagé avec la catégorie {targetCategory.Name}"
            : node.RelatedCategory is { } id && content.Categories.FirstOrDefault(c => c.Id == id) is { } relatedCategory
                ? $"🔗 Lié à la catégorie {relatedCategory.Name}"
                : null;

        return new TreeRowViewModel(targetCategory.Id, target, guides.Select(g => new TreeGuide(g)).ToList(), isLast)
        {
            Badge = node.Icon,
            BadgeColor = isFamily ? Color.FromArgb(category.Color) : Palette.SurfaceRaised,
            Title = node.Title,
            Summary = node.Summary,
            KindLabel = SpecializationLabels.Kind(node.Kind),
            RelatedText = related,
            StatusText = status,
            StatusColor = statusColor,
            Opacity = !unlocked || (tracks.Count == 0 && doors == 0) || targetLocked ? 0.5 : 1,
            IsLocked = !unlocked || targetLocked,
            LockedMessage = lockedMessage,
            HasProgress = unlocked && done > 0,
        };
    }

    /// <summary>Une porte mène à une autre catégorie : elle est toujours ouverte, même si cette catégorie n'a pas encore de contenu.</summary>
    private TreeRowViewModel CreateDoorRow(Category category, SpecializationNode node, bool unlocked, string lockedMessage,
        IReadOnlyList<bool> guides, bool isLast)
    {
        var target = content.GetCategory(node.LinkTo!);
        return new TreeRowViewModel(target.Id, node, guides.Select(g => new TreeGuide(g)).ToList(), isLast)
        {
            Badge = target.Icon,
            BadgeColor = Color.FromArgb(target.Color),
            Title = node.Title,
            Summary = node.Summary,
            KindLabel = SpecializationLabels.Kind(node.Kind),
            RelatedText = target.HasContent ? null : "🕓 Catégorie en préparation",
            StatusText = unlocked ? $"🚪 Explorer « {target.Name} »" : string.Empty,
            StatusColor = Palette.Primary,
            Opacity = unlocked ? 1 : 0.5,
            IsLocked = !unlocked,
            LockedMessage = lockedMessage,
        };
    }
}

/// <summary>Trait vertical d'un ancêtre : visible si cet ancêtre a encore des frères en dessous.</summary>
public sealed record TreeGuide(bool HasLine);

public sealed partial class TreeRowViewModel(string categoryId, SpecializationNode? node, IReadOnlyList<TreeGuide> guides, bool isLast)
    : ObservableObject
{
    /// <summary>Le nœud a des enfants et peut être replié ou déplié (double appui).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpanderText))]
    private bool isExpanded = true;

    /// <summary>Identifiant du nœud d'origine (avant résolution d'un renvoi), null si la ligne n'a pas d'enfants.</summary>
    public string? ExpandKey { get; set; }
    public bool IsCollapsible => ExpandKey is not null;
    public int HiddenCount { get; set; }
    public Action<TreeRowViewModel>? OnToggle { get; set; }

    /// <summary>Au moins une leçon faite sous ce nœud : il est « en cours » ou « terminé ».</summary>
    public bool HasProgress { get; init; }

    public string ExpanderText => IsExpanded
        ? "▾ Touche deux fois pour replier"
        : $"▸ {HiddenCount} élément{(HiddenCount > 1 ? "s" : "")} masqué{(HiddenCount > 1 ? "s" : "")} · touche deux fois pour déplier";

    [RelayCommand]
    private void ToggleExpand() => OnToggle?.Invoke(this);

    public IReadOnlyList<TreeGuide> Guides => guides;

    /// <summary>Coude « ├─ » ou « └─ » reliant la ligne à son parent (absent pour la racine).</summary>
    public bool HasElbow => node is not null;

    /// <summary>Le trait continue sous le coude si d'autres frères suivent.</summary>
    public bool HasLineBelow => !isLast;

    public bool IsRoot => node is null;
    public bool IsFamily => node?.Kind == SpecializationKind.Family;
    public bool IsLeaf => node is not null && !IsFamily;

    public required string Badge { get; init; }
    public required Color BadgeColor { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public string KindLabel { get; init; } = string.Empty;
    public string? RelatedText { get; init; }
    public bool HasRelated => RelatedText is not null;
    public required string StatusText { get; init; }
    public Color StatusColor { get; init; } = Palette.TextMuted;
    public double Opacity { get; init; } = 1;
    public bool IsLocked { get; init; }
    public string LockedMessage { get; init; } = string.Empty;

    public Color StrokeColor => node?.HasTrack == true && !IsLocked ? Palette.Primary : Palette.Stroke;

    public static TreeRowViewModel Root(Category category, string status) =>
        new(category.Id, null, [], isLast: true)
        {
            Badge = category.Icon,
            BadgeColor = Color.FromArgb(category.Color),
            Title = category.Name,
            Summary = category.Description,
            StatusText = status,
        };

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (node is null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        // Un nœud repliable sans parcours réagit au double appui : l'appui simple, déclenché aussi, ne fait rien.
        if (IsCollapsible && !node.HasTrack)
            return;

        if (IsLocked)
        {
            await Shell.Current.DisplayAlert("Pas encore disponible", LockedMessage, "OK");
            return;
        }

        if (node.IsDoor)
        {
            await Shell.Current.GoToAsync($"{nameof(CategoryPage)}?categoryId={categoryId}");
            return;
        }

        if (node.HasTrack)
        {
            await Shell.Current.GoToAsync($"{nameof(TrackPage)}?categoryId={categoryId}&nodeId={node.Id}");
            return;
        }

        if (IsLeaf)
            await Shell.Current.DisplayAlert(node.Title, $"{node.Summary}\n\nCe parcours arrive bientôt.", "OK");
    }
}
