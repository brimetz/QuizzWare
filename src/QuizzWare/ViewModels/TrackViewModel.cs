using CommunityToolkit.Mvvm.ComponentModel;
using QuizzWare.Core.Content;
using QuizzWare.Core.Models;
using QuizzWare.Core.Progress;

namespace QuizzWare.ViewModels;

/// <summary>Le parcours d'une spécialisation (ex. MongoDB) : en-tête et chapitres.</summary>
public partial class TrackViewModel(ContentRepository content, ProgressService progressService)
    : ObservableObject, IQueryAttributable
{
    private string? _categoryId;
    private string? _nodeId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string badge = string.Empty;

    [ObservableProperty]
    private string kindLabel = string.Empty;

    [ObservableProperty]
    private string summary = string.Empty;

    [ObservableProperty]
    private string? relatedText;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private IReadOnlyList<ChapterViewModel> chapters = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _categoryId = query.TryGetValue("categoryId", out var c) ? c as string : null;
        _nodeId = query.TryGetValue("nodeId", out var n) ? n as string : null;
    }

    public async Task LoadAsync()
    {
        if (_categoryId is null || _nodeId is null)
            return;

        var category = content.GetCategory(_categoryId);
        var node = content.GetSpecialization(_categoryId, _nodeId);
        var userProgress = await progressService.GetProgressAsync();
        var (done, total) = progressService.GetNodeProgress(userProgress, category, node);

        Title = node.Title;
        Badge = node.Icon;
        KindLabel = SpecializationLabels.Kind(node.Kind);
        Summary = node.Summary;
        RelatedText = node.RelatedCategory is { } id && content.Categories.FirstOrDefault(c => c.Id == id) is { } related
            ? $"🔗 Lié à la catégorie {related.Name}"
            : null;
        ProgressText = $"{done}/{total} leçons · spécialisation de {category.Name}";
        Progress = total == 0 ? 0 : (double)done / total;
        Chapters = PathBuilder.Build(category, node.Track, userProgress, progressService);
    }
}
