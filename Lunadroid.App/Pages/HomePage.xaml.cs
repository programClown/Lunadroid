using Lunadroid.App.ViewModels;

namespace Lunadroid.App.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnSearchCompleted(object? sender, EventArgs e)
    {
        if (_viewModel.SearchCommand.CanExecute(null))
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }

    private void OnSearchClicked(object? sender, EventArgs e)
    {
        if (_viewModel.SearchCommand.CanExecute(null))
        {
            _viewModel.SearchCommand.Execute(null);
        }
    }
}
