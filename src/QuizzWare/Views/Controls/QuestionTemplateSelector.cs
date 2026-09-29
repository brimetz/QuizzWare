using QuizzWare.ViewModels.Questions;

namespace QuizzWare.Views.Controls;

/// <summary>Choisit l'affichage adapté au type de question.</summary>
public sealed class QuestionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Options { get; set; }
    public DataTemplate? Bug { get; set; }
    public DataTemplate? Fill { get; set; }
    public DataTemplate? Order { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => item switch
    {
        OptionsQuestionViewModel => Options,
        BugQuestionViewModel => Bug,
        FillQuestionViewModel => Fill,
        OrderQuestionViewModel => Order,
        _ => null,
    } ?? throw new InvalidOperationException($"Aucun template pour {item.GetType().Name}");
}
