using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.ViewModels.Code;
using QuizzWare.Views;

namespace QuizzWare.ViewModels;

public sealed record SectionItem(string Title, string Body, IReadOnlyList<CodeLineViewModel> CodeLines)
{
    public bool HasCode => CodeLines.Count > 0;
}

public partial class LessonViewModel(ContentRepository content) : ObservableObject, IQueryAttributable
{
    private string? _categoryId;
    private string? _lessonId;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string summary = string.Empty;

    [ObservableProperty]
    private string quizButtonText = string.Empty;

    public ObservableCollection<SectionItem> Sections { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _categoryId = query.TryGetValue("categoryId", out var c) ? c as string : null;
        _lessonId = query.TryGetValue("unitId", out var l) ? l as string : null;
    }

    public void Load()
    {
        if (_categoryId is null || _lessonId is null || Sections.Count > 0)
            return;

        var language = content.GetCodeLanguage(_categoryId, _lessonId);
        var lesson = content.GetLesson(_categoryId, _lessonId);
        Title = lesson.Title;
        Summary = lesson.Summary;
        QuizButtonText = $"Passer le quizz ({lesson.Questions?.Count ?? 0} questions)";
        foreach (var section in lesson.Sections ?? [])
            Sections.Add(new SectionItem(section.Title, section.Body, CodeLineViewModel.FromCode(section.Code, section.Language ?? language)));
    }

    [RelayCommand]
    private Task StartQuizAsync() =>
        Shell.Current.GoToAsync($"{nameof(QuizPage)}?categoryId={_categoryId}&unitId={_lessonId}");
}
