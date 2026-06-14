using Lunadroid.Core.Services;

namespace Lunadroid.App;

public partial class App : Application
{
    private readonly AppConfigService _configService;

    public App(AppConfigService configService)
    {
        _configService = configService;
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            LoggingService.Error($"App.InitializeComponent failed: {ex.Message}", ex);
            throw;
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            LoggingService.Info("CreateWindow called");
            ApplyTheme();
            var config = _configService.Config;

            if (!config.OnboardingCompleted)
            {
                LoggingService.Info("Onboarding not completed, showing WelcomePage");
                return new Window(new Pages.WelcomePage());
            }

            LoggingService.Info("Creating AppShell");
            var shell = new AppShell();
            var window = new Window(shell);

            if (config.SecurityLockEnabled && !string.IsNullOrEmpty(config.PinCode))
            {
                shell.Loaded += async (_, _) =>
                {
                    await shell.GoToAsync(nameof(Pages.PinLockPage));
                };
            }

            LoggingService.Info("CreateWindow completed successfully");
            return window;
        }
        catch (Exception ex)
        {
            LoggingService.Error($"CreateWindow failed: {ex.Message}", ex);
            // Fallback: show a basic error page instead of crashing
            return new Window(new Pages.WelcomePage());
        }
    }

    private void ApplyTheme()
    {
        try
        {
            UserAppTheme = _configService.Config.ThemeMode switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
        catch (Exception ex)
        {
            LoggingService.Error($"ApplyTheme failed: {ex.Message}", ex);
        }
    }
}
