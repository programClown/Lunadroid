using Lunadroid.App.Views;

namespace Lunadroid.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(PlayerPage), typeof(PlayerPage));
    }
}