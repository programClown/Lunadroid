using Lunadroid.Core.Models;

namespace Lunadroid.App;

public record ApiSourceInfo(
    string ApiBaseUrl,
    string Name,
    string? DetailBaseUrl,
    bool IsCustomApi = false,
    bool IsAdult = false
    // Example: some sources might use /vodsearch instead of /api.php/provide/vod/...
)
{
    // Note: Detail paths for HTML sources are usually part of detail_base_url construction
    // Search paths and detail paths for JSON sources can use defaults or be overridden here
}

public static class AppSettings
{
    public const int SearchMaxPages = 50;
    public const string M3U8_PATTERN = @"https?:\/\/[^""'\s]+?\.m3u8";

    public const int SearchMaxVideos = 1000; //最多搜索多少部资源
    public static readonly List<string> SelectApis = ["dyttzy", "tyyszy"];
    public static readonly List<string> SelectAdultApis = [];

    public static readonly Dictionary<string, ApiSourceInfo> ApiSitesConfig = new();
    public static readonly Dictionary<string, ApiSourceInfo> AdultApiSitesConfig = new();

    public static void UpdateSites(List<ApiSource> apiSources)
    {
        ApiSitesConfig.Clear();
        AdultApiSitesConfig.Clear();

        foreach (ApiSource apiSource in apiSources)
        {
            if (apiSource.IsAdult)
            {
                AdultApiSitesConfig.Add(apiSource.Source, new ApiSourceInfo(
                    apiSource.ApiBaseUrl,
                    DetailBaseUrl: apiSource.DetailBaseUrl,
                    IsCustomApi: apiSource.IsCustomApi,
                    IsAdult: apiSource.IsAdult,
                    Name: apiSource.Name
                ));
            }
            else
            {
                ApiSitesConfig.Add(apiSource.Source, new ApiSourceInfo(apiSource.ApiBaseUrl,
                    DetailBaseUrl: apiSource.DetailBaseUrl,
                    IsCustomApi: apiSource.IsCustomApi,
                    IsAdult: apiSource.IsAdult,
                    Name: apiSource.Name
                ));
            }
        }
        SelectApis.Clear();
        SelectAdultApis.Clear();

        SelectApis.AddRange(ApiSitesConfig.Select(apiSourceInfo => apiSourceInfo.Key));
        SelectAdultApis.AddRange(AdultApiSitesConfig.Select(apiSourceInfo => apiSourceInfo.Key));
    }
}