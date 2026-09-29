using System.Collections;
using QuizzWare.ViewModels.Code;

namespace QuizzWare.Views.Controls;

/// <summary>Bloc de code : numéros de ligne, coloration syntaxique, défilement horizontal pour les lignes longues.</summary>
public partial class CodeBlockView : ContentView
{
    public static readonly BindableProperty LinesProperty = BindableProperty.Create(
        nameof(Lines), typeof(IEnumerable<CodeLineViewModel>), typeof(CodeBlockView),
        propertyChanged: (bindable, _, value) => BindableLayout.SetItemsSource(((CodeBlockView)bindable).LinesHost, (IEnumerable?)value));

    public CodeBlockView()
    {
        InitializeComponent();
    }

    public IEnumerable<CodeLineViewModel>? Lines
    {
        get => (IEnumerable<CodeLineViewModel>?)GetValue(LinesProperty);
        set => SetValue(LinesProperty, value);
    }
}
