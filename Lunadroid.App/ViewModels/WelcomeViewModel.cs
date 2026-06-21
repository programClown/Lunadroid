using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Views;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class WelcomeViewModel : BaseViewModel
{
    private readonly AppConfigService _configService;

    public WelcomeViewModel(AppConfigService configService)
    {
        _configService = configService;
        Title = "欢迎";
    }

    [RelayCommand]
    private async Task GetStarted()
    {
        var termsPage = new TermsPage();
        Application.Current!.Windows[0].Page = termsPage;
    }
}