using SQLite;

namespace Lunadroid.Core.Models;

public class DownloadRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int EpisodeId { get; set; }
    public string EpisodeName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string LocalFilePath { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public double DownloadProgress { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Downloading, Completed, Failed
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
}
