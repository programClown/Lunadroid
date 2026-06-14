using System.Text.Json;
using Lunadroid.Core.Models;

namespace Lunadroid.Core.Services;

public class AppConfigService
{
    private readonly string _configPath;
    private AppConfig _config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppConfigService(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
            Directory.CreateDirectory(configDirectory);

        _configPath = Path.Combine(configDirectory, "appconfig.json");
        _config = LoadConfig();
    }

    public AppConfig Config => _config;

    private AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_configPath), JsonOptions) ?? new AppConfig();
        }
        catch
        {
            // If config is corrupted, return defaults
        }

        return new AppConfig();
    }

    public void SaveConfig()
    {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(_config, JsonOptions));
    }

    public void UpdateConfig(Action<AppConfig> action)
    {
        action(_config);
        SaveConfig();
    }

    public void ResetConfig()
    {
        _config = new AppConfig();
        SaveConfig();
    }
}
