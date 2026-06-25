using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class ProfilePage : UraniumContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    private async void OnSettingsTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}