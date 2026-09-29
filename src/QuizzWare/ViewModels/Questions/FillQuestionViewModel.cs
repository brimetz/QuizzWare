using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Content;
using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.ViewModels.Questions;

/// <summary>
/// Code à trous : toucher un mot le place dans le premier trou libre, « Annuler » retire le dernier placé.
/// </summary>
public sealed partial class FillQuestionViewModel : QuestionViewModel
{
    // Marqueur interne d'un trou dans le texte composé, remplacé ensuite par un span coloré.
    private static readonly Regex BlankMarker = new("\u0001(\\d+)\u0002", RegexOptions.Compiled);

    private readonly FillQuizQuestion _question;
    private readonly string? _language;
    private readonly TokenViewModel?[] _filled;
    private readonly Stack<int> _history = new();
    private bool[]? _blankResults;

    public FillQuestionViewModel(FillQuizQuestion question, string? language)
    {
        _question = question;
        _language = language;
        _filled = new TokenViewModel?[question.Answers.Count];
        Tokens = question.Tokens.Select(t => new TokenViewModel(t, Place)).ToList();
        RenderLines();
    }

    public override string Kind => "🧩 Complète le code";
    public string Hint => "Touche les mots pour remplir les trous, dans l'ordre.";
    public IReadOnlyList<TokenViewModel> Tokens { get; }
    public ObservableCollection<CodeLineViewModel> Lines { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    private bool canUndo;

    [ObservableProperty]
    private bool showSolution;

    public IReadOnlyList<CodeLineViewModel> SolutionLines => Solution(_question.Compose(_question.Answers), _language);

    public override Answer BuildAnswer() => new FillAnswer(_filled.Select(t => t!.Text).ToList());

    private void Place(TokenViewModel token)
    {
        if (IsLocked || token.IsUsed)
            return;

        var blank = Array.IndexOf(_filled, null);
        if (blank < 0)
            return;

        _filled[blank] = token;
        token.IsUsed = true;
        _history.Push(blank);
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (IsLocked || _history.Count == 0)
            return;

        var blank = _history.Pop();
        _filled[blank]!.IsUsed = false;
        _filled[blank] = null;
        Refresh();
    }

    private void Refresh()
    {
        CanSubmit = _filled.All(t => t is not null);
        CanUndo = _history.Count > 0;
        RenderLines();
    }

    protected override void ApplyCorrection(bool isCorrect)
    {
        _blankResults = _filled.Select((t, i) => t!.Text == _question.Answers[i]).ToArray();
        CanUndo = false;
        ShowSolution = !isCorrect;
        RenderLines();
    }

    /// <summary>Reconstruit les lignes : code coloré + trous (vides, remplis, ou corrigés).</summary>
    private void RenderLines()
    {
        var composed = new System.Text.StringBuilder(_question.Segments[0]);
        for (var i = 1; i < _question.Segments.Count; i++)
            composed.Append('\u0001').Append(i - 1).Append('\u0002').Append(_question.Segments[i]);

        Lines.Clear();
        var lines = CodeText.SplitLines(composed.ToString());
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var formatted = new FormattedString();
            var parts = BlankMarker.Split(lines[lineIndex]); // texte, n° de trou, texte, n° de trou...
            for (var p = 0; p < parts.Length; p++)
            {
                if (p % 2 == 0)
                    SyntaxHighlighter.Append(formatted, parts[p], _language);
                else
                    formatted.Spans.Add(BlankSpan(int.Parse(parts[p])));
            }
            Lines.Add(new CodeLineViewModel(lineIndex, formatted));
        }
    }

    private Span BlankSpan(int blank)
    {
        var token = _filled[blank];
        var background = _blankResults is { } results
            ? (results[blank] ? Palette.SuccessSurface : Palette.ErrorSurface)
            : token is null ? Palette.Stroke : Palette.PrimarySurface;

        var span = SyntaxHighlighter.CreateSpan(token is null ? " ??? " : $" {token.Text} ", token is null ? Palette.TextMuted : Palette.TextPrimary);
        span.BackgroundColor = background;
        span.FontAttributes = FontAttributes.Bold;
        return span;
    }
}

public partial class TokenViewModel(string text, Action<TokenViewModel> onTap) : ObservableObject
{
    public string Text => text;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Opacity))]
    private bool isUsed;

    public double Opacity => IsUsed ? 0.3 : 1;

    [RelayCommand]
    private void Tap() => onTap(this);
}
