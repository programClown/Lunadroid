using System.Text;
using Serilog;

namespace Lunadroid.Core.Services;

public static class Logger
{
    private static ILogger? _logger;

    public static void Initialize(string logDirectory)
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "lunadroid_.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                encoding: Encoding.UTF8)
            .CreateLogger();

        _logger = Log.Logger;
        _logger.Information("Lunadroid logging initialized");
    }

    public static void Debug(string message)
    {
        _logger?.Debug(message);
    }

    public static void Info(string message)
    {
        _logger?.Information(message);
    }

    public static void Warning(string message)
    {
        _logger?.Warning(message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        _logger?.Error(ex, message);
    }

    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }
}