using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Services;

namespace Lunadroid.App.ViewModels;

[QueryProperty(nameof(IsSetupMode), "IsSetupMode")]
public partial class PinLockViewModel : ObservableObject
{
    [ObservableProperty] private string _pin = string.Empty;
    [ObservableProperty] private string _confirmPin = string.Empty;
    [ObservableProperty] private bool _isSetupMode;
    [ObservableProperty] private bool _isConfirmPhase;
    [ObservableProperty] private string _errorMessage = string.Empty;
    private string _firstPin = string.Empty;

    public int PinLength => Pin.Length;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public string Subtitle => IsSetupMode ? (IsConfirmPhase ? "请再次输入PIN码以确认" : "请输入4-6位数字PIN码") : "请输入您的PIN码以解锁";

    partial void OnPinChanged(string value) { OnPropertyChanged(nameof(PinLength)); OnPropertyChanged(nameof(HasError)); }
    partial void OnErrorMessageChanged(string value) { OnPropertyChanged(nameof(HasError)); }
    partial void OnIsConfirmPhaseChanged(bool value) { OnPropertyChanged(nameof(Subtitle)); }
    partial void OnIsSetupModeChanged(bool value) { OnPropertyChanged(nameof(Subtitle)); }

    [RelayCommand] private void EnterDigit(string digit)
    {
        if (!IsConfirmPhase) { if (Pin.Length < 6) Pin += digit; }
        else { if (ConfirmPin.Length < 6) ConfirmPin += digit; }
        ErrorMessage = string.Empty;
    }

    [RelayCommand] private void DeleteDigit()
    {
        if (!IsConfirmPhase) { if (Pin.Length > 0) Pin = Pin[..^1]; }
        else { if (ConfirmPin.Length > 0) ConfirmPin = ConfirmPin[..^1]; }
    }

    [RelayCommand] private async Task ConfirmAsync()
    {
        if (IsSetupMode)
        {
            if (!IsConfirmPhase)
            {
                if (Pin.Length < 4) { ErrorMessage = "PIN至少需要4位"; return; }
                _firstPin = Pin;
                IsConfirmPhase = true;
                Pin = string.Empty;
                return;
            }
            if (ConfirmPin != _firstPin) { ErrorMessage = "两次输入不一致"; ConfirmPin = string.Empty; return; }
            AppServices.AppConfig.UpdateConfig(c => { c.PinCode = _firstPin; c.SecurityLockEnabled = true; });
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            var savedPin = AppServices.AppConfig.Config.PinCode;
            if (Pin == savedPin) { await Shell.Current.GoToAsync(".."); }
            else { ErrorMessage = "PIN码错误"; Pin = string.Empty; }
        }
    }
}
