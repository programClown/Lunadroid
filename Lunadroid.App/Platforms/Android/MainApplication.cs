using Android.App;
using Android.Runtime;
using Lunadroid.Core.Services;

namespace Lunadroid.App;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
		// Global unhandled exception handler for crash diagnostics
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			var ex = e.ExceptionObject as Exception;
			LoggingService.Error($"UNHANDLED EXCEPTION (IsTerminating={e.IsTerminating}): {ex?.Message}", ex);
		};
		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			LoggingService.Error($"UNOBSERVED TASK EXCEPTION: {e.Exception?.Message}", e.Exception);
			e.SetObserved();
		};
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
