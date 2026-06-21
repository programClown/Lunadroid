using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class AppLockViewModel : BaseViewModel
{
    private readonly AppConfigService _configService;

    [ObservableProperty] private bool _hasPin1;

    [ObservableProperty] private bool _hasPin2;

    [ObservableProperty] private bool _hasPin3;

    [ObservableProperty] private bool _hasPin4;

    [ObservableProperty] private bool _isSetup;

    [ObservableProperty] private bool _isVerifying;

    [ObservableProperty] private string _pinEntry = string.Empty;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public AppLockViewModel(AppConfigService configService)
    {
        _configService = configService;
        Title = "安全验证";
        IsVerifying = configService.Config.SecurityLockEnabled;
        IsSetup = !configService.Config.SecurityLockEnabled;
    }

    partial void OnPinEntryChanged(string value)
    {
        HasPin1 = value.Length >= 1;
        HasPin2 = value.Length >= 2;
        HasPin3 = value.Length >= 3;
        HasPin4 = value.Length >= 4;
    }

    [RelayCommand]
    private async Task AppendPin(string digit)
    {
        if (PinEntry.Length < 4)
        {
            PinEntry += digit;
            if (PinEntry.Length == 4)
            {
                await ValidatePin();
            }
        }
    }

    [RelayCommand]
    private void ClearPin()
    {
        PinEntry = string.Empty;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void DeletePin()
    {
        if (PinEntry.Length > 0)
        {
            PinEntry = PinEntry[..^1];
        }
    }

    private async Task ValidatePin()
    {
        if (IsVerifying)
        {
            if (PinEntry == _configService.Config.PinCode)
            {
                Application.Current!.Windows[0].Page = new AppShell();
            }
            else
            {
                StatusMessage = "密码错误，请重试";
                PinEntry = string.Empty;
            }
        }
        else if (IsSetup)
        {
            _configService.UpdateConfig(c =>
            {
                c.SecurityLockEnabled = true;
                c.PinCode = PinEntry;
            });
            StatusMessage = "密码设置成功";
            await Task.Delay(500);
            Application.Current!.Windows[0].Page = new AppShell();
        }
    }

    [RelayCommand]
    private async Task SkipLock()
    {
        Application.Current!.Windows[0].Page = new AppShell();
    }
}