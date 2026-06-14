using SQLite;

namespace Lunadroid.Core.Models;

public class SearchHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public DateTime SearchedAt { get; set; } = DateTime.Now;
}
