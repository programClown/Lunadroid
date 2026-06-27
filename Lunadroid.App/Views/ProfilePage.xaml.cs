using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Lunadroid.App.ViewModels;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class ProfilePage : UraniumContentPage
{
    private const string LunadroidGitHubUrl = "https://github.com/programClown/Lunadroid";
    private const string LunaTVGitHubUrl = "https://github.com/programClown/LunaTV";

    public ProfilePage(ProfileViewModel profileViewModel)
    {
        InitializeComponent();
        BindingContext = profileViewModel;
    }

    private async void OnSettingsTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void OnLunadroidGitHubTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(LunadroidGitHubUrl);
        }
        catch
        {
            await Shell.Current.DisplayAlert("提示", "无法打开浏览器，请手动访问：" + LunadroidGitHubUrl, "确定");
        }
    }

    private async void OnLunaTVGitHubTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(LunaTVGitHubUrl);
        }
        catch
        {
            await Shell.Current.DisplayAlert("提示", "无法打开浏览器，请手动访问：" + LunaTVGitHubUrl, "确定");
        }
    }

    private async void OnQQGroupTapped(object? sender, TappedEventArgs e)
    {
        var image = new Image
        {
            Source = "kys_qrcode.jpg",
            Aspect = Aspect.AspectFill,
            HeightRequest = 280,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var label = new Label
        {
            Text = "扫码加入 QQ 讨论群",
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 8, 0, 0),
            TextColor = Application.Current!.RequestedTheme == AppTheme.Dark
                ? Colors.White
                : Colors.Black
        };

        var layout = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            HorizontalOptions = LayoutOptions.Center,
            Children = { image, label }
        };

        var popup = new Popup
        {
            Content = layout,
            WidthRequest = 320,
            HeightRequest = 380,
            CanBeDismissedByTappingOutsideOfPopup = true,
            BackgroundColor = Application.Current!.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#1E1E1E")
                : Colors.White
        };

        await this.ShowPopupAsync(popup);
    }
}