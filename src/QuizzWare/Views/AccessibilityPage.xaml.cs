using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class AccessibilityPage : ContentPage
{
    private readonly AccessibilityViewModel _viewModel;

    public AccessibilityPage(AccessibilityViewModel viewModel)
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
