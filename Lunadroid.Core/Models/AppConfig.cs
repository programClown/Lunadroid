namespace Lunadroid.Core.Models;

public class AppConfig
{
    public bool OnboardingCompleted { get; set; }
    public bool TermsAccepted { get; set; }
    public string ThemeMode { get; set; } = "Dark";
    public bool SecurityLockEnabled { get; set; }
    public string? PinCode { get; set; }
    public string CloudSourceUrl { get; set; } = "https://pz.v88.qzz.io/?format=0&source=full";
    public string DownloadDirectory { get; set; } = string.Empty;

    public bool Autoplay { get; set; }
    public int Timeout { get; set; } = 15000; // 播放器加载超时时间
    public bool FilterAds { get; set; } = true; // 是否启用广告过滤
    public bool AutoPlayNext { get; set; } = true; // 默认启用自动连播功能
    public bool AdFilteringEnabled { get; set; } = true; // 默认开启分片广告过滤
    public bool DoubanApiEnabled { get; set; } // 豆瓣API
    public bool HomeAutoLoadDoubanEnabled { get; set; } // 首页自动加载豆瓣数据
    public bool ForceApiNeedSpecialSource { get; set; } //强制使用api
}