using SQLite;

namespace Lunadroid.Core.Models;

public class PlayHistory
{
    [PrimaryKey] [AutoIncrement] public int Id { get; set; }

    public string? VodId { get; set; } //电影Id
    public string? Name { get; set; } //电影名
    public string? Episode { get; set; } //剧集
    public string? Url { get; set; } //播放地址
    public string? Source { get; set; } //来源
    public string? SourceName { get; set; } //来源名称
    public int PlaybackPosition { get; set; } //播放位置
    public int Duration { get; set; } //总时长
    public int TotalEpisodeCount { get; set; } //总集数
    public bool IsLocal { get; set; } //本地影视
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}