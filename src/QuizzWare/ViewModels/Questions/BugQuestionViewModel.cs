using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.ViewModels.Questions;

/// <summary>Trouve le bug : l'utilisateur touche la ligne fautive.</summary>
public sealed class BugQuestionViewModel : QuestionViewModel
{
    private readonly BugQuizQuestion _question;
    private CodeLineViewModel? _selected;

    public BugQuestionViewModel(BugQuizQuestion question, string? language)
    {
        _question = question;
        Lines = CodeLineViewModel.FromCode(question.Source.Code, language, Select);
    }

    public override string Kind => "🐞 Trouve le bug";
    public string Hint => "Touche la ligne qui pose problème.";
    public IReadOnlyList<CodeLineViewModel> Lines { get; }

    public override Answer BuildAnswer() => new LineAnswer(_selected!.Index);

    private void Select(CodeLineViewModel line)
    {
        if (IsLocked)
            return;

        if (_selected is not null)
            _selected.Background = Colors.Transparent;
        _selected = line;
        _selected.Background = Palette.PrimarySurface;
        CanSubmit = true;
    }

    protected override void ApplyCorrection(bool isCorrect)
    {
        if (_selected is not null && !isCorrect)
            _selected.Background = Palette.ErrorSurface;
        foreach (var index in _question.BugLineIndices)
            Lines[index].Background = Palette.SuccessSurface;
    }
}
