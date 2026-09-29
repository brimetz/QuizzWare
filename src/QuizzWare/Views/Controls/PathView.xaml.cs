using System.Collections;
using QuizzWare.ViewModels;

namespace QuizzWare.Views.Controls;

/// <summary>Affiche un parcours : ses chapitres, leurs leçons et leur défi entretien.</summary>
public partial class PathView : ContentView
{
    public static readonly BindableProperty ChaptersProperty = BindableProperty.Create(
        nameof(Chapters), typeof(IEnumerable<ChapterViewModel>), typeof(PathView),
        propertyChanged: (bindable, _, value) => BindableLayout.SetItemsSource(((PathView)bindable).ChaptersHost, (IEnumerable?)value));

    public PathView()
    {
        InitializeComponent();
    }

    public IEnumerable<ChapterViewModel>? Chapters
    {
        get => (IEnumerable<ChapterViewModel>?)GetValue(ChaptersProperty);
        set => SetValue(ChaptersProperty, value);
    }
}
