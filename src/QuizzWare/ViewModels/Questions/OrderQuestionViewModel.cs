using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.ViewModels.Questions;

/// <summary>
/// Remettre dans l'ordre : toucher une ligne disponible l'ajoute à la réponse,
/// toucher une ligne de la réponse la renvoie dans les lignes disponibles.
/// </summary>
public sealed partial class OrderQuestionViewModel : QuestionViewModel
{
    private readonly OrderQuizQuestion _question;
    private readonly string? _language;

    public OrderQuestionViewModel(OrderQuizQuestion question, string? language)
    {
        _question = question;
        _language = language;
        foreach (var (line, i) in question.ShuffledLines.Select((l, i) => (l, i)))
            Pool.Add(new OrderLineViewModel(i, line, SyntaxHighlighter.Highlight(line, language), Move));
    }

    public override string Kind => "↕ Remets dans l'ordre";
    public string Hint => "Touche les lignes dans l'ordre d'exécution.";
    public ObservableCollection<OrderLineViewModel> Answer { get; } = [];
    public ObservableCollection<OrderLineViewModel> Pool { get; } = [];

    [ObservableProperty]
    private bool isAnswerEmpty = true;

    [ObservableProperty]
    private bool hasPool = true;

    [ObservableProperty]
    private bool showSolution;

    public IReadOnlyList<CodeLineViewModel> SolutionLines => Solution(string.Join('\n', _question.CorrectLines), _language);

    public override Answer BuildAnswer() => new OrderAnswer(Answer.Select(l => l.Text).ToList());

    private void Move(OrderLineViewModel line)
    {
        if (IsLocked)
            return;

        if (Pool.Remove(line))
        {
            Answer.Add(line);
        }
        else if (Answer.Remove(line))
        {
            // Retour à sa place d'origine parmi les lignes disponibles.
            var position = Pool.TakeWhile(l => l.OriginalIndex < line.OriginalIndex).Count();
            Pool.Insert(position, line);
        }

        IsAnswerEmpty = Answer.Count == 0;
        HasPool = Pool.Count > 0;
        CanSubmit = Pool.Count == 0;
    }

    protected override void ApplyCorrection(bool isCorrect)
    {
        for (var i = 0; i < Answer.Count; i++)
            Answer[i].Background = Answer[i].Text == _question.CorrectLines[i] ? Palette.SuccessSurface : Palette.ErrorSurface;
        ShowSolution = !isCorrect;
    }
}

public partial class OrderLineViewModel(int originalIndex, string text, FormattedString formatted, Action<OrderLineViewModel> onTap) : ObservableObject
{
    public int OriginalIndex => originalIndex;
    public string Text => text;
    public FormattedString Formatted => formatted;

    [ObservableProperty]
    private Color background = Palette.SurfaceRaised;

    [RelayCommand]
    private void Tap() => onTap(this);
}
