using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(); // rafraîchit les niveaux au retour d'un quizz

        // Premier lancement : le parcours de bienvenue s'affiche une seule fois.
        if (_viewModel.NeedsOnboarding)
            await Shell.Current.GoToAsync(nameof(OnboardingPage), animate: false);
    }
}
