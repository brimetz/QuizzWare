using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class OnboardingPage : ContentPage
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Load();
    }

    /// <summary>Le bouton retour d'Android revient à l'étape précédente au lieu de quitter le parcours.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.CanGoBack)
            _viewModel.BackCommand.Execute(null);
        return true;
    }
}
