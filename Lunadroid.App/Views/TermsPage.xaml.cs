using Lunadroid.App.ViewModels;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class TermsPage : UraniumContentPage
{
    private readonly TermsViewModel _viewModel;

    public TermsPage()
    {
        InitializeComponent();
        _viewModel = IPlatformApplication.Current!.Services.GetRequiredService<TermsViewModel>();
        BindingContext = _viewModel;
    }

    private void OnTermsLabelTapped(object? sender, TappedEventArgs e)
    {
        _viewModel.TermsChecked = !_viewModel.TermsChecked;
    }
}