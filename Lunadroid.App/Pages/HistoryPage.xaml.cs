using Lunadroid.App.ViewModels;

namespace Lunadroid.App.Pages;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadPlayHistoryCommand.Execute(null);
        _viewModel.LoadDownloadHistoryCommand.Execute(null);
    }

    private async void OnClearAllClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("确认", "确定要清除所有历史记录吗？此操作不可撤销。", "确定", "取消");
        if (confirm)
        {
            _viewModel.ClearPlayHistoryCommand.Execute(null);
            _viewModel.ClearDownloadRecordsCommand.Execute(null);
        }
    }
}
