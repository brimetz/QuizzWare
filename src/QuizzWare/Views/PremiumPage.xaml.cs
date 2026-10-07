using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class PremiumPage : ContentPage
{
    private readonly PremiumViewModel _viewModel;

    public PremiumPage(PremiumViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Unload();
    }
}
