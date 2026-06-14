using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Services;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly AppConfigService _config;

    [ObservableProperty] private string _selectedTheme = "System";
    [ObservableProperty] private string _appVersion = "1.0.0";
    [ObservableProperty] private bool _securityLockEnabled;

    public SettingsViewModel(DatabaseService db, AppConfigService config)
    {
        _db = db; _config = config;
        SelectedTheme = config.Config.ThemeMode;
        SecurityLockEnabled = config.Config.SecurityLockEnabled;
        AppVersion = AppInfo.Current.VersionString;
    }

    [RelayCommand] private void ChangeTheme(string theme)
    {
        SelectedTheme = theme;
        _config.UpdateConfig(c => c.ThemeMode = theme);
        Application.Current!.UserAppTheme = theme switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    [RelayCommand] private async Task ClearAllDataAsync()
    {
        if (await Shell.Current.DisplayAlert("确认", "确定要清除所有应用数据吗？此操作不可撤销。", "确定", "取消"))
        { await _db.ClearAllDataAsync(); await Shell.Current.DisplayAlert("完成", "所有数据已清除。", "确定"); }
    }

    [RelayCommand] private async Task ResetAppAsync()
    {
        if (await Shell.Current.DisplayAlert("确认", "确定要重置应用吗？所有数据和设置将被清除。", "确定", "取消"))
        { await _db.ClearAllDataAsync(); _config.ResetConfig(); await Shell.Current.DisplayAlert("完成", "应用已重置，即将重启。", "确定"); }
    }

    [RelayCommand] private void ToggleSecurityLock()
    {
        SecurityLockEnabled = !SecurityLockEnabled;
        _config.UpdateConfig(c => c.SecurityLockEnabled = SecurityLockEnabled);
    }

    [RelayCommand] private async Task SetupPinAsync()
    {
        await Shell.Current.GoToAsync(nameof(Pages.PinLockPage),
            new Dictionary<string, object> { { "IsSetupMode", true } });
    }
}
