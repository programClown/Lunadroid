using CommunityToolkit.Mvvm.ComponentModel;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class ProfileViewModel : BaseViewModel
{
    private readonly AppConfigService _appConfigService;

    [ObservableProperty] private string _buildDateText = string.Empty;
    [ObservableProperty] private int _selectedThemeIndex;
    [ObservableProperty] private string _versionText = string.Empty;

    public ProfileViewModel(AppConfigService appConfigService)
    {
        _appConfigService = appConfigService;
        Title = "我的";
        LoadConfigFromService();
        LoadAppInfo();
    }

    public List<string> ThemeOptions { get; } = ["跟随系统", "浅色", "深色"];

    private void LoadConfigFromService()
    {
        var c = _appConfigService.Config;
        SelectedThemeIndex = c.ThemeMode switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
    }

    private void LoadAppInfo()
    {
        var version = AppInfo.Current.VersionString;
        VersionText = $"V{version}";
        BuildDateText = BuildDate.Value;
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        Application.Current!.UserAppTheme = value switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
        _appConfigService.UpdateConfig(c =>
        {
            c.ThemeMode = value switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "System"
            };
        });
    }
}