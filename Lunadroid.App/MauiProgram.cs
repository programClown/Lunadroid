using CommunityToolkit.Maui;
using Lunadroid.App.ViewModels;
using Lunadroid.Core.Services;
using UraniumUI;
using Environment = Android.OS.Environment;

namespace Lunadroid.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement(false) // false to avoid Android foreground service crash
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                fonts.AddMaterialSymbolsFonts();
            });

        builder.Services.AddCommunityToolkitDialogs();

        // Database
        var publicDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments);
        var dbPath = Path.Combine(publicDir!.AbsolutePath, "com.lunadroid.app", "lunadroid.db");
        builder.Services.AddSingleton(new DatabaseService(dbPath));

        // Config
        builder.Services.AddSingleton(new AppConfigService(Path.Combine(publicDir!.AbsolutePath, "com.lunadroid.app")));

        builder.Services.AddSingleton<MovieApiService>();
        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<TermsViewModel>();
        builder.Services.AddTransient<AppLockViewModel>();
        builder.Services.AddTransient<HomeViewModel>();

        return builder.Build();
    }
}