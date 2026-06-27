using Lunadroid.App.ViewModels;
using Lunadroid.App.Views;

namespace Lunadroid.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Navigating += OnNavigating;

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(PlayerPage), typeof(PlayerPage));
    }

    private void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (e.Source == ShellNavigationSource.ShellSectionChanged)
        {
            try
            {
                var homeViewModel = App.Services.GetService<HomeViewModel>();
                if (homeViewModel?.IsSearchOngoing == true)
                {
                    e.Cancel();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}