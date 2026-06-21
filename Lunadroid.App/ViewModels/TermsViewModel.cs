using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class TermsViewModel : BaseViewModel
{
    private readonly AppConfigService _configService;

    [ObservableProperty] private bool _termsChecked;

    public TermsViewModel(AppConfigService configService)
    {
        _configService = configService;
        Title = "使用条款";
    }

    [RelayCommand]
    private async Task AcceptTerms()
    {
        _configService.UpdateConfig(c =>
        {
            c.TermsAccepted = true;
            c.OnboardingCompleted = true;
        });

        Application.Current!.Windows[0].Page = new AppShell();
    }
}