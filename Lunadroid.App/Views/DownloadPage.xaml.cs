using Lunadroid.App.ViewModels;
using UraniumUI.Extensions;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class DownloadPage : UraniumContentPage
{
    public DownloadPage(DownloadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = (DownloadViewModel)BindingContext;
        vm.LoadDownloadsAsync().FireAndForget();
    }
}