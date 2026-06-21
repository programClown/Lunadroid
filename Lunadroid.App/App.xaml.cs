using Lunadroid.Core.Services;

namespace Lunadroid.App;

public partial class App : Application
{
    // Signals when DB init + seeding are done
    private readonly TaskCompletionSource _dbReady = new();

    public App(DatabaseService databaseService)
    {
        InitializeComponent();

        // Kick off init — when done, signal _dbReady
        _ = InitializeAsync(databaseService);
    }

    private async Task InitializeAsync(DatabaseService databaseService)
    {
        try
        {
            await databaseService.InitializeAsync();
            _dbReady.TrySetResult();
        }
        catch (Exception ex)
        {
            _dbReady.TrySetException(ex);
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Return a blank window immediately so the splash screen can dismiss.
        // Once the DB is ready, swap the window's page to the correct start page.
        var window = new Window(new ContentPage()); // blank placeholder

        _ = SetStartPageAsync(window);

        return window;
    }

    private async Task SetStartPageAsync(Window window)
    {
        try
        {
            // Wait for DB init + seeding to finish before reading settings
            await _dbReady.Task;

            var startPage = new AppShell();

            // Switch to the real page on the UI thread
            window.Page = startPage;
        }
        catch (Exception ex)
        {
            // Surface the error visibly rather than hanging on a blank screen
            window.Page = new ContentPage
            {
                Content = new Label
                {
                    Text = $"Startup error:\n\n{ex.Message}",
                    TextColor = Colors.Red,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(24)
                }
            };
        }
    }
}