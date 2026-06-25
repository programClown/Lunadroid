using Lunadroid.App.ViewModels;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class ProfilePage : UraniumContentPage
{
    public ProfilePage(ProfileViewModel profileViewModel)
    {
        InitializeComponent();
        BindingContext = profileViewModel;
    }

    private async void OnSettingsTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}