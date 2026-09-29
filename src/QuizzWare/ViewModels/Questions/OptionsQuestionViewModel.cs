using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.ViewModels.Questions;

/// <summary>QCM à réponse unique ou multiple, avec un extrait de code optionnel (« prédis la sortie », « revue de code »).</summary>
public sealed class OptionsQuestionViewModel : QuestionViewModel
{
    private readonly IReadOnlySet<int> _correct;

    public OptionsQuestionViewModel(ChoiceQuizQuestion question, string? language)
        : this(question, question.Choices, new HashSet<int> { question.CorrectIndex }, isMultiple: false, language)
    {
    }

    public OptionsQuestionViewModel(MultiChoiceQuizQuestion question, string? language)
        : this(question, question.Choices, question.CorrectIndices, isMultiple: true, language)
    {
    }

    private OptionsQuestionViewModel(QuizQuestion question, IReadOnlyList<string> choices, IReadOnlySet<int> correct, bool isMultiple, string? language)
    {
        _correct = correct;
        IsMultiple = isMultiple;
        CodeLines = CodeLineViewModel.FromCode(question.Source.Code, language);
        Options = choices.Select((text, i) => new OptionViewModel(i, text, isMultiple, Toggle)).ToList();
    }

    public bool IsMultiple { get; }
    public IReadOnlyList<CodeLineViewModel> CodeLines { get; }
    public bool HasCode => CodeLines.Count > 0;
    public IReadOnlyList<OptionViewModel> Options { get; }
    public string Hint => IsMultiple ? "Plusieurs réponses possibles : coche toutes les bonnes." : string.Empty;

    public override string Kind => (IsMultiple, HasCode) switch
    {
        (true, true) => "🔍 Revue de code",
        (true, false) => "☑ Plusieurs réponses",
        (false, true) => "💻 Lis le code",
        _ => "❓ Question",
    };

    public override Answer BuildAnswer()
    {
        var selected = Options.Where(o => o.IsSelected).Select(o => o.Index).ToList();
        return IsMultiple ? new MultiChoiceAnswer(selected) : new ChoiceAnswer(selected.Single());
    }

    private void Toggle(OptionViewModel option)
    {
        if (IsLocked)
            return;

        if (IsMultiple)
        {
            option.IsSelected = !option.IsSelected;
        }
        else
        {
            foreach (var o in Options)
                o.IsSelected = o == option;
        }

        CanSubmit = Options.Any(o => o.IsSelected);
    }

    protected override void ApplyCorrection(bool isCorrect)
    {
        foreach (var option in Options)
        {
            var shouldBeSelected = _correct.Contains(option.Index);
            option.SetState(shouldBeSelected ? OptionState.Correct
                : option.IsSelected ? OptionState.Wrong
                : OptionState.Dimmed);
        }
    }
}

public enum OptionState { Idle, Selected, Correct, Wrong, Dimmed }

public partial class OptionViewModel(int index, string text, bool isMultiple, Action<OptionViewModel> onTap) : ObservableObject
{
    public int Index => index;
    public string Text => text;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Marker))]
    private bool isSelected;

    [ObservableProperty]
    private Color backgroundColor = Palette.Surface;

    [ObservableProperty]
    private Color strokeColor = Palette.Stroke;

    [ObservableProperty]
    private double opacity = 1;

    /// <summary>Lettre (A, B, C...) pour un choix unique, case à cocher pour un choix multiple.</summary>
    public string Marker => isMultiple ? (IsSelected ? "☑" : "☐") : ((char)('A' + index)).ToString();

    partial void OnIsSelectedChanged(bool value) => SetState(value ? OptionState.Selected : OptionState.Idle);

    [RelayCommand]
    private void Tap() => onTap(this);

    public void SetState(OptionState state)
    {
        (BackgroundColor, StrokeColor, Opacity) = state switch
        {
            OptionState.Selected => (Palette.PrimarySurface, Palette.Primary, 1.0),
            OptionState.Correct => (Palette.SuccessSurface, Palette.Success, 1.0),
            OptionState.Wrong => (Palette.ErrorSurface, Palette.Error, 1.0),
            OptionState.Dimmed => (Palette.Surface, Palette.Stroke, 0.5),
            _ => (Palette.Surface, Palette.Stroke, 1.0),
        };
    }
}
