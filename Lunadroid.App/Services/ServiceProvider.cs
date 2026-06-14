using Lunadroid.Core.Services;

namespace Lunadroid.App.Services;

public static class AppServices
{
    public static DatabaseService Database { get; set; } = null!;
    public static MovieApiService MovieApi { get; set; } = null!;
    public static HlsDownloadService HlsDownload { get; set; } = null!;
    public static AppConfigService AppConfig { get; set; } = null!;
}
