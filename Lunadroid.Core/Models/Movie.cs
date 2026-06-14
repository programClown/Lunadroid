using SQLite;

namespace Lunadroid.Core.Models;

public class Movie
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
