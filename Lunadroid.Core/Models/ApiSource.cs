using SQLite;

namespace Lunadroid.Core.Models;

public class ApiSource
{
    [PrimaryKey] [AutoIncrement] public int Id { get; set; }

    public string? Source { get; set; }
    public string? Name { get; set; }
    public string? ApiBaseUrl { get; set; }
    public string? DetailBaseUrl { get; set; }
    public bool IsAdult { get; set; }
    public bool IsCustomApi { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}