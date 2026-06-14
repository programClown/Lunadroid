using SQLite;

namespace Lunadroid.Core.Models;

public class MovieEpisode
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string MovieId { get; set; } = string.Empty;
    public string EpisodeName { get; set; } = string.Empty;
    public string PlayUrl { get; set; } = string.Empty;
    public int EpisodeIndex { get; set; }
}
