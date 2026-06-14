using System.Diagnostics;
using System.Text.Json;
using Lunadroid.Core.Models;

namespace Lunadroid.Core.Services;

public class MovieApiService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MovieApiService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<List<MovieSearchResult>> SearchAsync(MovieSource source, string keyword, CancellationToken cancellationToken = default)
    {
        var results = new List<MovieSearchResult>();

        try
        {
            var apiUrl = source.ApiUrl.TrimEnd('/');
            var encodedKeyword = Uri.EscapeDataString(keyword);
            var requestUrl = $"{apiUrl}?ac=detail&wd={encodedKeyword}";

            var responseJson = await _httpClient.GetStringAsync(requestUrl, cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("list", out var listElement))
                return results;

            foreach (var item in listElement.EnumerateArray())
            {
                try
                {
                    var vodId = item.GetProperty("vod_id").ToString();
                    var title = item.TryGetProperty("vod_name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                    var posterUrl = item.TryGetProperty("vod_pic", out var picEl) ? picEl.GetString() ?? string.Empty : string.Empty;
                    var typeName = item.TryGetProperty("type_name", out var typeNameEl) ? typeNameEl.GetString() ?? string.Empty : string.Empty;
                    var year = item.TryGetProperty("vod_year", out var yearEl) ? yearEl.GetString() ?? string.Empty : string.Empty;

                    double rating = 0;
                    if (item.TryGetProperty("vod_score", out var scoreEl))
                    {
                        var scoreStr = scoreEl.GetString() ?? scoreEl.ToString();
                        double.TryParse(scoreStr, out rating);
                    }

                    results.Add(new MovieSearchResult
                    {
                        Id = $"{source.Id}_{vodId}",
                        Title = title,
                        PosterUrl = posterUrl,
                        Rating = rating,
                        SourceName = source.Name,
                        SourceId = source.Id,
                        Year = year,
                        Category = typeName,
                        DetailUrl = vodId
                    });
                }
                catch (Exception ex)
                {
                    LoggingService.Warning($"Failed to parse search result item: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error($"Search failed for source '{source.Name}': {ex.Message}", ex);
        }

        return results;
    }

    public async Task<MovieDetailResult?> GetDetailAsync(MovieSource source, string detailUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiUrl = source.ApiUrl.TrimEnd('/');

            // detailUrl here is the vod_id from search results
            var requestUrl = $"{apiUrl}?ac=detail&ids={Uri.EscapeDataString(detailUrl)}";

            var responseJson = await _httpClient.GetStringAsync(requestUrl, cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("list", out var listElement))
                return null;

            var firstItem = listElement.EnumerateArray().FirstOrDefault();
            if (firstItem.ValueKind == JsonValueKind.Undefined)
                return null;

            var vodId = firstItem.GetProperty("vod_id").ToString();
            var title = firstItem.TryGetProperty("vod_name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
            var posterUrl = firstItem.TryGetProperty("vod_pic", out var picEl) ? picEl.GetString() ?? string.Empty : string.Empty;
            var description = firstItem.TryGetProperty("vod_content", out var contentEl) ? contentEl.GetString() ?? string.Empty : string.Empty;
            var year = firstItem.TryGetProperty("vod_year", out var yearEl) ? yearEl.GetString() ?? string.Empty : string.Empty;
            var typeName = firstItem.TryGetProperty("type_name", out var typeNameEl) ? typeNameEl.GetString() ?? string.Empty : string.Empty;

            double rating = 0;
            if (firstItem.TryGetProperty("vod_score", out var scoreEl))
            {
                var scoreStr = scoreEl.GetString() ?? scoreEl.ToString();
                double.TryParse(scoreStr, out rating);
            }

            var episodes = new List<EpisodeResult>();

            // Parse vod_play_url: episodes separated by #, name$url pairs separated by $
            if (firstItem.TryGetProperty("vod_play_url", out var playUrlEl))
            {
                var playUrlStr = playUrlEl.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(playUrlStr))
                {
                    var episodeSegments = playUrlStr.Split('#', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < episodeSegments.Length; i++)
                    {
                        var parts = episodeSegments[i].Split('$');
                        if (parts.Length >= 2)
                        {
                            episodes.Add(new EpisodeResult
                            {
                                Name = parts[0].Trim(),
                                PlayUrl = parts[1].Trim(),
                                Index = i
                            });
                        }
                        else if (parts.Length == 1)
                        {
                            episodes.Add(new EpisodeResult
                            {
                                Name = $"Episode {i + 1}",
                                PlayUrl = parts[0].Trim(),
                                Index = i
                            });
                        }
                    }
                }
            }

            return new MovieDetailResult
            {
                Id = $"{source.Id}_{vodId}",
                Title = title,
                PosterUrl = posterUrl,
                Rating = rating,
                Description = description,
                Year = year,
                Category = typeName,
                Episodes = episodes
            };
        }
        catch (Exception ex)
        {
            LoggingService.Error($"GetDetail failed for source '{source.Name}': {ex.Message}", ex);
        }

        return null;
    }

    public async Task<List<CloudSourceItem>> FetchCloudSourcesAsync(string url, CancellationToken cancellationToken = default)
    {
        var results = new List<CloudSourceItem>();

        try
        {
            var responseJson = await _httpClient.GetStringAsync(url, cancellationToken);

            // Try parsing as a JSON array directly
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var cloudItem = item.Deserialize<CloudSourceItem>(JsonOptions);
                    if (cloudItem != null)
                        results.Add(cloudItem);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Some sources wrap the array in an object
                if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataEl.EnumerateArray())
                    {
                        var cloudItem = item.Deserialize<CloudSourceItem>(JsonOptions);
                        if (cloudItem != null)
                            results.Add(cloudItem);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.Error($"FetchCloudSources failed: {ex.Message}", ex);
        }

        return results;
    }

    public async Task<(bool IsAccessible, long LatencyMs)> CheckSourceAccessibilityAsync(string apiUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var requestUrl = $"{apiUrl.TrimEnd('/')}?ac=detail&wd=test";

            var response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            sw.Stop();

            return (response.IsSuccessStatusCode, sw.ElapsedMilliseconds);
        }
        catch
        {
            return (false, -1);
        }
    }
}
