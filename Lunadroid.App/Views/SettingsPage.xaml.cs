using Lunadroid.App.ViewModels;
using UraniumUI.Extensions;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class SettingsPage : UraniumContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();

        BindingContext = settingsViewModel;
        _viewModel = settingsViewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _viewModel.LoadApiSourcesAsync().FireAndForget();
    }

    private void Switch_OnToggled(object? sender, ToggledEventArgs e)
    {
        // e.Value 是 bool 类型，表示开关切换后的新状态xs
        if (sender is Switch sw && sw.BindingContext is ApiSourceItem item)
        {
            item.Enable = e.Value;
            _viewModel.ToggleApiSourceEnabledAsync(item);
        }
    }
}