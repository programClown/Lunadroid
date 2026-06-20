using Lunadroid.App.Views;

namespace Lunadroid.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for pages that are NOT in the tab bar.
		// These are pushed on top of a tab's navigation stack via
		// Shell.Current.GoToAsync("SettingsPage")
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
