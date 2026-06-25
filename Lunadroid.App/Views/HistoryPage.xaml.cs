using Lunadroid.App.ViewModels;
using UraniumUI.Extensions;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class HistoryPage : UraniumContentPage
{
    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = (HistoryViewModel)BindingContext;
        vm.LoadHistoriesAsync().FireAndForget();
    }
}