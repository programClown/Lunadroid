using Lunadroid.App.ViewModels;

namespace Lunadroid.App.Pages;

public partial class MySourcesPage : ContentPage
{
    private readonly MySourcesViewModel _viewModel;

    public MySourcesPage(MySourcesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.LoadSourcesCommand.CanExecute(null))
        {
            _viewModel.LoadSourcesCommand.Execute(null);
        }
    }
}
