using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class SpecializationsPage : ContentPage
{
    private readonly SpecializationsViewModel _viewModel;

    public SpecializationsPage(SpecializationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(); // rafraîchit la progression au retour d'un parcours
    }
}
