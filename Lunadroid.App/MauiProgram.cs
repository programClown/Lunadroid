using CommunityToolkit.Maui;
using Lunadroid.App.ViewModels;
using Lunadroid.Core.Services;
using UraniumUI;

namespace Lunadroid.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement(true)
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                fonts.AddMaterialSymbolsFonts();
            });

        builder.Services.AddCommunityToolkitDialogs();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lunadroid.db");
        builder.Services.AddSingleton(new DatabaseService(dbPath));
        builder.Services.AddSingleton<MovieApiService>();
        builder.Services.AddTransient<HomePageViewModel>();

        return builder.Build();
    }
}