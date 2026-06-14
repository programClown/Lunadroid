using Lunadroid.App.ViewModels;

namespace Lunadroid.App.Pages;

public partial class MovieDetailPage : ContentPage
{
    private readonly MovieDetailViewModel _viewModel;

    public MovieDetailPage(MovieDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.LoadDetailCommand.CanExecute(null))
        {
            _viewModel.LoadDetailCommand.Execute(null);
        }
    }
}
