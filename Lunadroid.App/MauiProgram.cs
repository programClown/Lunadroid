using Mopups.Hosting;
using UraniumUI.Icons.MaterialSymbols;
using InputKit.Shared.Controls;
using UraniumUI;
using Lunadroid.Core.Services;

namespace Lunadroid.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureMopups()
			.UseUraniumUI()
			.UseUraniumUIMaterial()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

				fonts.AddMaterialSymbolsFonts();

            });

		builder.Services.AddMopupsDialogs();

        // DatabaseService registered as singleton — App.cs resolves and inits it
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lunadroid.db");
        builder.Services.AddSingleton(new DatabaseService(dbPath));

        return builder.Build();
	}
}
