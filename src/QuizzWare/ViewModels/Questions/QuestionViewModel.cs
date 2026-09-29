using CommunityToolkit.Mvvm.ComponentModel;
using QuizzWare.Core.Quiz;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.ViewModels.Questions;

/// <summary>
/// Base des questions affichées. Déroulé commun : l'utilisateur compose sa réponse (<see cref="CanSubmit"/>),
/// appuie sur « Vérifier », puis la question affiche la correction (<see cref="ShowCorrection"/>).
/// </summary>
public abstract partial class QuestionViewModel : ObservableObject
{
    [ObservableProperty]
    private bool canSubmit;

    /// <summary>Vrai une fois la réponse vérifiée : les interactions sont alors bloquées.</summary>
    [ObservableProperty]
    private bool isLocked;

    /// <summary>Libellé du type de question, affiché au-dessus de l'énoncé.</summary>
    public abstract string Kind { get; }

    public abstract Answer BuildAnswer();

    public void ShowCorrection(bool isCorrect)
    {
        IsLocked = true;
        CanSubmit = false;
        ApplyCorrection(isCorrect);
    }

    protected abstract void ApplyCorrection(bool isCorrect);

    /// <param name="categoryLanguage">Langage par défaut du code, remplacé par celui de la question s'il est précisé.</param>
    public static QuestionViewModel Create(QuizQuestion question, string? categoryLanguage) =>
        CreateForLanguage(question, question.Source.Language ?? categoryLanguage);

    private static QuestionViewModel CreateForLanguage(QuizQuestion question, string? language) => question switch
    {
        ChoiceQuizQuestion q => new OptionsQuestionViewModel(q, language),
        MultiChoiceQuizQuestion q => new OptionsQuestionViewModel(q, language),
        BugQuizQuestion q => new BugQuestionViewModel(q, language),
        FillQuizQuestion q => new FillQuestionViewModel(q, language),
        OrderQuizQuestion q => new OrderQuestionViewModel(q, language),
        _ => throw new NotSupportedException(question.GetType().Name),
    };

    protected static IReadOnlyList<CodeLineViewModel> Solution(string code, string? language) =>
        CodeLineViewModel.FromCode(code, language);
}
