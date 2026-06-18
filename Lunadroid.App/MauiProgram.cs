using CommunityToolkit.Maui;
using Lunadroid.App.Pages;
using Lunadroid.App.ViewModels;
using Lunadroid.App.Services;
using Lunadroid.Core.Services;
using Serilog;
using UraniumUI;
using UraniumUI.Material;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;

namespace Lunadroid.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureMopups()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement(false)
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseUraniumUIBlurs()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                fonts.AddMaterialSymbolsFonts();
            });

        var appDataDir = FileSystem.AppDataDirectory;
        var dbPath = Path.Combine(appDataDir, "lunadroid.db");
        var logDir = Path.Combine(appDataDir, "logs");
        var configDir = appDataDir;

        // Initialize Serilog first for crash diagnostics
        LoggingService.Initialize(logDir);
        LoggingService.Info("Lunadroid startup: initializing services");
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);

        try
        {
            // Register services - DB init MUST be synchronous (tables needed immediately by ViewModels)
            var dbService = new DatabaseService(dbPath);
            dbService.InitializeAsync().GetAwaiter().GetResult();
            LoggingService.Info("Database initialized successfully");

            var configService = new AppConfigService(configDir);
            LoggingService.Info("Config service initialized");

            var movieApi = new MovieApiService(new HttpClient());
            var hlsDownload = new HlsDownloadService(new HttpClient());

            builder.Services.AddSingleton(dbService);
            builder.Services.AddSingleton(configService);
            builder.Services.AddSingleton(movieApi);
            builder.Services.AddSingleton(hlsDownload);

            // Set static AppServices
            AppServices.Database = dbService;
            AppServices.MovieApi = movieApi;
            AppServices.HlsDownload = hlsDownload;
            AppServices.AppConfig = configService;

            builder.Services.AddMopupsDialogs();
            // Register ViewModels
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<MovieDetailViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<MySourcesViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<PinLockViewModel>();

            // Register Pages
            builder.Services.AddTransient<WelcomePage>();
            builder.Services.AddTransient<TermsPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<MovieDetailPage>();
            builder.Services.AddTransient<PlayerPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<MySourcesPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<PinLockPage>();

            LoggingService.Info("All services registered, build starting");
        }
        catch (Exception ex)
        {
            LoggingService.Error($"CRITICAL: Startup failed - {ex.Message}", ex);
            throw;
        }

        return builder.Build();
    }
}
