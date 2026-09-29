using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;

namespace QuizzWare.ViewModels.Code;

/// <summary>Une ligne de code affichée avec son numéro ; peut être cliquable (questions « trouve le bug »).</summary>
public partial class CodeLineViewModel(int index, FormattedString text, Action<CodeLineViewModel>? onTap = null) : ObservableObject
{
    public int Index => index;
    public string Number => (index + 1).ToString();
    public FormattedString Text => text;

    [ObservableProperty]
    private Color background = Colors.Transparent;

    [RelayCommand]
    private void Tap() => onTap?.Invoke(this);

    public static IReadOnlyList<CodeLineViewModel> FromCode(string? code, string? language, Action<CodeLineViewModel>? onTap = null) =>
        string.IsNullOrEmpty(code)
            ? []
            : CodeText.SplitLines(code).Select((line, i) => new CodeLineViewModel(i, SyntaxHighlighter.Highlight(line, language), onTap)).ToList();
}
