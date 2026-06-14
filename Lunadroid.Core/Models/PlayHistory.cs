using SQLite;

namespace Lunadroid.Core.Models;

public class PlayHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int EpisodeId { get; set; }
    public string EpisodeName { get; set; } = string.Empty;
    public string PlayUrl { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public double ProgressSeconds { get; set; }
    public double DurationSeconds { get; set; }
    public bool IsLocal { get; set; }
    public DateTime LastWatchedAt { get; set; } = DateTime.Now;
}
