using Lunadroid.App.Pages;

namespace Lunadroid.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for pages that are NOT in the tab bar.
        // These are pushed on top of a tab's navigation stack via
        // Shell.Current.GoToAsync("TransactionDetailPage?code=RG84XY1234")
        Routing.RegisterRoute(nameof(MovieDetailPage), typeof(MovieDetailPage));
        Routing.RegisterRoute(nameof(PlayerPage), typeof(PlayerPage));
        Routing.RegisterRoute(nameof(PinLockPage), typeof(PinLockPage));
    }
}
