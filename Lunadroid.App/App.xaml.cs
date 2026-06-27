using Lunadroid.App.Views;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;
using Environment = Android.OS.Environment;

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

    public static IServiceProvider Services => IPlatformApplication.Current!.Services;

    private async Task InitializeAsync(DatabaseService databaseService)
    {
        try
        {
            // Initialize logging
            var publicDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments);
            var logDir = Path.Combine(publicDir!.AbsolutePath, "com.lunadroid.app", "logs");
            Logger.Initialize(logDir);
            Logger.Info("App starting...");

            await databaseService.InitializeAsync();

            var appSources = await databaseService.GetApiSourcesAsync();
            if (appSources.Count == 0)
            {
                appSources = new List<ApiSource>
                {
                    new()
                    {
                        Id = 0,
                        Source = "ffzyapi.com",
                        Name = "🎬非凡资源",
                        ApiBaseUrl = "https://api.ffzyapi.com/api.php/provide/vod",
                        DetailBaseUrl = "",
                        IsCustomApi = false,
                        IsAdult = false,
                        IsEnabled = true,
                        CreateTime = DateTime.Now
                    }
                };
                foreach (var s in appSources)
                {
                    await databaseService.AddApiSourceAsync(s);
                }
            }

            AppSettings.UpdateSites(appSources);
            _dbReady.TrySetResult();
        }
        catch (Exception ex)
        {
            Logger.Error("App initialization failed", ex);
            _dbReady.TrySetException(ex);
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Return a blank window immediately so the splash screen can dismiss.
        // Once the DB is ready, swap the window's page to the correct start page.
        var window = new Window(new WelcomePage()); // blank placeholder

        _ = SetStartPageAsync(window);

        return window;
    }

    private async Task SetStartPageAsync(Window window)
    {
        try
        {
            // Wait for DB init + seeding to finish before reading settings
            await _dbReady.Task;
            var appConfigService = Services.GetRequiredService<AppConfigService>();
            var config = appConfigService.Config;

            Current!.UserAppTheme = config.ThemeMode switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };

            Page startPage;

            if (!config.OnboardingCompleted)
            {
                startPage = new WelcomePage();
            }
            else if (config.SecurityLockEnabled)
            {
                startPage = new AppLockPage();
            }
            else
            {
                startPage = new AppShell();
            }

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