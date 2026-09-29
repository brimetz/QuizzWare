using QuizzWare.ViewModels;

namespace QuizzWare.Views;

public partial class TrackPage : ContentPage
{
    private readonly TrackViewModel _viewModel;

    public TrackPage(TrackViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync(); // rafraîchit les leçons débloquées au retour d'un quizz
    }
}
