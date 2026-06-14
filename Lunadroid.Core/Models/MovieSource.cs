using SQLite;

namespace Lunadroid.Core.Models;

public class MovieSource
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsAdult { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsAccessible { get; set; }
    public long AccessLatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
