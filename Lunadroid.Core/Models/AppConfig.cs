using System.Text.Json.Serialization;

namespace Lunadroid.Core.Models;

public class AppConfig
{
    public bool OnboardingCompleted { get; set; }
    public bool TermsAccepted { get; set; }
    public string ThemeMode { get; set; } = "System";
    public bool SecurityLockEnabled { get; set; }
    public string? PinCode { get; set; }
    public bool BiometricEnabled { get; set; }
    public string CloudSourceUrl { get; set; } = "https://pz.v88.qzz.io/?format=0&source=full";
    public string DownloadDirectory { get; set; } = string.Empty;
}

public class CloudSourceItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("api")]
    public string Api { get; set; } = string.Empty;
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    [JsonPropertyName("adult")]
    public bool Adult { get; set; }
}

public class MovieSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string Year { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
}

public class MovieDetailResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<EpisodeResult> Episodes { get; set; } = new();
}

public class EpisodeResult
{
    public string Name { get; set; } = string.Empty;
    public string PlayUrl { get; set; } = string.Empty;
    public int Index { get; set; }
}
