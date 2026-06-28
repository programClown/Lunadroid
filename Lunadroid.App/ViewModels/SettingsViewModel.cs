using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public class PingResult
{
    public string Name { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public long LatencyMs { get; set; } = -1;
    public bool IsSuccess { get; set; }
    public string Status => IsSuccess ? $"{LatencyMs}ms" : "超时";

    public Color StatusColor => IsSuccess
        ? LatencyMs < 300 ? Colors.Green : LatencyMs < 1000 ? Colors.Orange : Colors.Red
        : Colors.Red;
}

public class CloudApiSourceResponse
{
    [JsonPropertyName("cache_time")] public int CacheTime { get; set; }

    [JsonPropertyName("api_site")] public Dictionary<string, CloudApiSite>? ApiSite { get; set; }
}

public class CloudApiSite
{
    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("api")] public string? Api { get; set; }

    [JsonPropertyName("detail")] public string? Detail { get; set; }

    [JsonPropertyName("_comment")] public string? Comment { get; set; }
}

public partial class SettingsViewModel : BaseViewModel
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly AppConfigService _appConfigService;
    private readonly DatabaseService _databaseService;
    private readonly HttpClient _httpClient;
    [ObservableProperty] private bool _autoplay;
    [ObservableProperty] private string _cacheSizeText = "计算中...";
    [ObservableProperty] private string _cloudSourceUrl = string.Empty;
    [ObservableProperty] private string _fetchStatusText = string.Empty;
    [ObservableProperty] private bool _forceApiNeedSpecialSource;
    [ObservableProperty] private bool _isCalculatingCache;
    [ObservableProperty] private bool _isClearingCache;
    [ObservableProperty] private bool _isExporting;

    [ObservableProperty] private bool _isFetchingSources;
    [ObservableProperty] private bool _isImporting;
    [ObservableProperty] private bool _isPinging;
    [ObservableProperty] private bool _isSecurityLockEnabled;
    [ObservableProperty] private string _pingStatusText = string.Empty;

    public SettingsViewModel(DatabaseService databaseService, AppConfigService appConfigService)
    {
        _databaseService = databaseService;
        _appConfigService = appConfigService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        Title = "设置";
        LoadConfigFromService();
    }

    public ObservableCollection<ApiSourceItem> ApiSourceItems { get; } = [];
    public ObservableCollection<CacheItemInfo> CacheItems { get; } = [];
    public ObservableCollection<PingResult> PingResults { get; } = [];

    private void LoadConfigFromService()
    {
        var c = _appConfigService.Config;
        ForceApiNeedSpecialSource = c.ForceApiNeedSpecialSource;
        IsSecurityLockEnabled = c.SecurityLockEnabled;
        Autoplay = c.Autoplay;
    }

    private void SaveConfigToService()
    {
        _appConfigService.UpdateConfig(c =>
        {
            c.ForceApiNeedSpecialSource = ForceApiNeedSpecialSource;
            c.SecurityLockEnabled = IsSecurityLockEnabled;
            c.Autoplay = Autoplay;
            c.CloudSourceUrl = CloudSourceUrl;
        });
    }


    partial void OnForceApiNeedSpecialSourceChanged(bool value)
    {
        SaveConfigToService();
    }

    partial void OnAutoplayChanged(bool value)
    {
        SaveConfigToService();
    }

    partial void OnCloudSourceUrlChanged(string value)
    {
        SaveConfigToService();
    }

    partial void OnIsSecurityLockEnabledChanged(bool value)
    {
        SaveConfigToService();
    }

    public async Task LoadApiSourcesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var sources = await _databaseService.GetApiSourcesAsync();
            ApiSourceItems.Clear();
            foreach (var s in sources)
            {
                ApiSourceItems.Add(new ApiSourceItem
                {
                    Id = s.Id,
                    Source = s.Source,
                    Name = s.Name,
                    Enable = s.IsEnabled,
                    IsCustom = s.IsCustomApi
                });
            }
        }
        finally
        {
            await RefreshAppSettingsAsync();
            IsBusy = false;
        }
    }

    public async Task LoadCacheInfoAsync()
    {
        if (IsCalculatingCache) return;
        IsCalculatingCache = true;
        try
        {
            var cacheDir = FileSystem.CacheDirectory;
            var files = new List<FileInfo>();
            if (Directory.Exists(cacheDir))
            {
                files.AddRange(GetAllFiles(new DirectoryInfo(cacheDir)));
            }

            var totalSize = files.Sum(f => f.Length);
            CacheSizeText = FormatFileSize(totalSize);

            var top10 = files.OrderByDescending(f => f.Length).Take(10).ToList();
            CacheItems.Clear();
            foreach (var file in top10)
            {
                CacheItems.Add(new CacheItemInfo
                {
                    FileName = Path.GetRelativePath(cacheDir, file.FullName),
                    FilePath = file.FullName,
                    SizeText = FormatFileSize(file.Length),
                    Size = file.Length
                });
            }
        }
        catch (Exception ex)
        {
            CacheSizeText = "计算失败";
            Logger.Error($"LoadCacheInfo failed: {ex.Message}");
        }
        finally
        {
            IsCalculatingCache = false;
        }
    }

    private static List<FileInfo> GetAllFiles(DirectoryInfo dir)
    {
        var result = new List<FileInfo>();
        try
        {
            result.AddRange(dir.GetFiles());
            foreach (var subDir in dir.GetDirectories())
            {
                result.AddRange(GetAllFiles(subDir));
            }
        }
        catch
        {
        }

        return result;
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    [RelayCommand]
    private async Task DeleteCacheItemAsync(CacheItemInfo? item)
    {
        if (item == null) return;
        try
        {
            if (File.Exists(item.FilePath))
            {
                File.Delete(item.FilePath);
                var dir = Path.GetDirectoryName(item.FilePath);
                if (dir != null && Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                }
            }

            CacheItems.Remove(item);
            await LoadCacheInfoAsync();
            await Toast.Make("已删除").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"删除失败: {ex.Message}").Show();
        }
    }

    [RelayCommand]
    private async Task ClearAllCacheAsync()
    {
        if (IsClearingCache) return;
        IsClearingCache = true;
        try
        {
            var cacheDir = FileSystem.CacheDirectory;
            if (Directory.Exists(cacheDir))
            {
                foreach (var file in Directory.GetFiles(cacheDir, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }

                foreach (var dir in Directory.GetDirectories(cacheDir))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch
                    {
                    }
                }
            }

            CacheItems.Clear();
            CacheSizeText = "0 B";
            await Toast.Make("缓存已清除").Show();
        }
        catch (Exception ex)
        {
            await Toast.Make($"清除失败: {ex.Message}").Show();
        }
        finally
        {
            IsClearingCache = false;
        }
    }

    [RelayCommand]
    private async Task ResetPinCodeAsync()
    {
        _appConfigService.UpdateConfig(c => { c.PinCode = null; });

        await Toast.Make("PIN码已重置成功").Show();
    }


    [RelayCommand]
    private async Task FetchSourcesFromCloudAsync()
    {
        if (IsFetchingSources) return;
        IsFetchingSources = true;
        FetchStatusText = "正在拉取云端资源...";

        try
        {
            var url = string.IsNullOrWhiteSpace(CloudSourceUrl)
                ? "https://pz.v88.qzz.io?format=0&source=jin18"
                : CloudSourceUrl;

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {jsonString}");

                // 解析JSON响应
                var cloudData = JsonSerializer.Deserialize<CloudApiSourceResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (cloudData != null)
                {
                    var cloudApiSources = new List<ApiSource>();
                    // 处理云端数据
                    foreach (var (source, site) in cloudData.ApiSite)
                    {
                        cloudApiSources.Add(new ApiSource
                        {
                            Source = source,
                            ApiBaseUrl = site.Api,
                            DetailBaseUrl = site.Api.StartsWith(site.Detail) ? null : site.Detail,
                            Name = site.Name,
                            IsAdult = site.Name.Contains("🔞"),
                            IsCustomApi = false,
                            IsEnabled = false
                        });
                    }

                    // 删除所有API源数据后插入
                    await _databaseService.ClearApiSourcesAsync();

                    await _databaseService.AddApiSourcesAsync(cloudApiSources);

                    FetchStatusText = $"完成！共新增 {cloudApiSources.Count} 个";
                    await LoadApiSourcesAsync();
                    await RefreshAppSettingsAsync();
                }
            }
            else
            {
                FetchStatusText = $"拉取失败: {response.StatusCode}";
                Logger.Error($"HTTP请求失败: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            FetchStatusText = $"拉取失败: {ex.Message}";
            Logger.Error($"FetchSourcesFromCloud failed: {ex.Message}");
        }
        finally
        {
            IsFetchingSources = false;
        }
    }

    private static string ExtractSource(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return url.GetHashCode().ToString("x");
        }
    }

    [RelayCommand]
    private async Task ImportSourcesFromJsonAsync()
    {
        if (IsImporting) return;
        IsImporting = true;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, ["application/json", "*/*"] },
                    { DevicePlatform.WinUI, [".json"] }
                })
            });

            if (result == null) return;

            await using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            List<ApiSource> sources;
            var trimmed = json.Trim();
            if (trimmed.StartsWith("["))
            {
                sources = JsonSerializer.Deserialize<List<ApiSource>>(json, JsonOpts) ?? [];
            }
            else
            {
                sources = JsonSerializer.Deserialize<List<ApiSource>>(json, JsonOpts) ?? [];
            }

            var added = 0;
            foreach (var source in sources)
            {
                if (string.IsNullOrWhiteSpace(source.ApiBaseUrl)) continue;
                source.IsAdult = source.Name.Contains("🔞") || source.IsAdult;
                source.Id = 0;
                await _databaseService.AddApiSourceAsync(source);
                added++;
            }

            FetchStatusText = $"导入完成，共导入 {added} 个资源";
            await LoadApiSourcesAsync();
            await RefreshAppSettingsAsync();
        }
        catch (Exception ex)
        {
            FetchStatusText = $"导入失败: {ex.Message}";
            Logger.Error($"ImportSourcesFromJson failed: {ex.Message}");
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task ExportSourcesToJsonAsync()
    {
        if (IsExporting) return;
        IsExporting = true;
        try
        {
            var sources = await _databaseService.GetApiSourcesAsync();
            if (sources.Count == 0)
            {
                FetchStatusText = "没有可导出的资源";
                return;
            }

            var json = JsonSerializer.Serialize(sources, JsonOpts);
            var fileName = $"apisources_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "导出ApiSource",
                File = new ShareFile(filePath)
            });

            FetchStatusText = $"导出完成，共 {sources.Count} 个资源";
        }
        catch (Exception ex)
        {
            FetchStatusText = $"导出失败: {ex.Message}";
            Logger.Error($"ExportSourcesToJson failed: {ex.Message}");
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task PingAllSourcesAsync()
    {
        if (IsPinging) return;
        IsPinging = true;
        PingResults.Clear();
        PingStatusText = "正在测试连通性...";

        try
        {
            var sources = await _databaseService.GetApiSourcesAsync();
            if (sources.Count == 0)
            {
                PingStatusText = "没有可测试的资源";
                return;
            }

            var completed = 0;
            var tasks = sources.Select(async s =>
            {
                var result = await PingSourceAsync(s);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PingResults.Add(result);
                    completed++;
                    PingStatusText = $"已测试 {completed}/{sources.Count}";
                });
            });

            await Task.WhenAll(tasks);

            var successCount = PingResults.Count(r => r.IsSuccess);
            PingStatusText = $"测试完成: {successCount}/{sources.Count} 可用";
        }
        catch (Exception ex)
        {
            PingStatusText = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsPinging = false;
        }
    }

    private async Task<PingResult> PingSourceAsync(ApiSource source)
    {
        var result = new PingResult
        {
            Name = source.Name,
            ApiBaseUrl = source.ApiBaseUrl
        };

        try
        {
            var sw = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(source.ApiBaseUrl);
            sw.Stop();
            result.IsSuccess = response.IsSuccessStatusCode;
            result.LatencyMs = sw.ElapsedMilliseconds;
        }
        catch
        {
            result.IsSuccess = false;
            result.LatencyMs = -1;
        }

        return result;
    }

    [RelayCommand]
    private async Task DeleteApiSourceAsync(ApiSourceItem? source)
    {
        if (source == null) return;
        await _databaseService.DeleteApiSourceByIdAsync(source.Id);
        ApiSourceItems.Remove(source);
        await RefreshAppSettingsAsync();
    }

    public async void ToggleApiSourceEnabledAsync(ApiSourceItem? item)
    {
        try
        {
            if (item == null) return;
            var source = await _databaseService.GetApiSourceByIdAsync(item.Id);

            if (source != null)
            {
                source.IsEnabled = item.Enable;
                await _databaseService.UpdateApiSourceAsync(source);
                await RefreshAppSettingsAsync();
            }
        }
        catch (Exception e)
        {
            Logger.Error($"ToggleApiSourceEnabledAsync failed: {e.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectAllApiSourcesAsync()
    {
        foreach (var item in ApiSourceItems)
        {
            item.Enable = true;
            var source = await _databaseService.GetApiSourceByIdAsync(item.Id);
            if (source != null)
            {
                source.IsEnabled = true;
                await _databaseService.UpdateApiSourceAsync(source);
            }
        }

        await RefreshAppSettingsAsync();
    }

    [RelayCommand]
    private async Task DeselectAllApiSourcesAsync()
    {
        foreach (var item in ApiSourceItems)
        {
            item.Enable = false;
            var source = await _databaseService.GetApiSourceByIdAsync(item.Id);
            if (source != null)
            {
                source.IsEnabled = false;
                await _databaseService.UpdateApiSourceAsync(source);
            }
        }

        await RefreshAppSettingsAsync();
    }

    private async Task RefreshAppSettingsAsync()
    {
        var apiSources = await _databaseService.GetEnabledApiSourcesAsync();
        AppSettings.UpdateSites(apiSources);
    }
}

public partial class ApiSourceItem : ObservableObject
{
    [ObservableProperty] private bool _enable;
    public int Id { get; set; }
    public string? Source { get; set; }
    public string? Name { get; set; }
    public bool IsCustom { get; set; }
}

public partial class CacheItemInfo : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private long _size;
    [ObservableProperty] private string _sizeText = string.Empty;
}