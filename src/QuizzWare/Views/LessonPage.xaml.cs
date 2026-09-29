using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class LessonPage : ContentPage
{
    private readonly LessonViewModel _viewModel;

    public LessonPage(LessonViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Load();
    }
}
