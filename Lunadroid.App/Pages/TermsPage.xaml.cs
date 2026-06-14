using Lunadroid.App.Services;

namespace Lunadroid.App.Pages;

public partial class TermsPage : ContentPage
{
    public TermsPage()
    {
        InitializeComponent();
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        try
        {
            var appConfig = AppServices.AppConfig;
            appConfig.UpdateConfig(config =>
            {
                config.OnboardingCompleted = true;
                config.TermsAccepted = true;
            });

            Application.Current!.Windows[0].Page = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", $"保存配置失败: {ex.Message}", "确定");
        }
    }
}
