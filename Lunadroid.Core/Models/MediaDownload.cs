using SQLite;

namespace Lunadroid.Core.Models;

public class MediaDownload
{
    [PrimaryKey] [AutoIncrement] public int Id { get; set; }

    public string? Source { get; set; } //来源
    public string? Name { get; set; } //电影名
    public string? Episode { get; set; } //剧集
    public string? Url { get; set; } //播放地址
    public string? LocalPath { get; set; } // 本地地址
    public bool IsDownloaded { get; set; } // 是否下载完成
    public DateTime UpdateTime { get; set; } = DateTime.Now;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}