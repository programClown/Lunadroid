using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Models;
using Lunadroid.App.Services;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class PlayerViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly MovieTvService _movieTvService;

    [ObservableProperty] private List<EpisodeSubjectItem> _episodes = new();

    [ObservableProperty] private string _episodesCountText;
    [ObservableProperty] private bool _isMauiPlayer;
    [ObservableProperty] private string _mauiVideoUrl = "";
    [ObservableProperty] private List<string> _playSurfaces = ["maui播放器", "web播放器"];
    [ObservableProperty] private string _selectedPlaySurface = "web播放器";
    [ObservableProperty] private DetailResult? _videoDetail;
    private VedioSearchResult _videoSearchResult;
    private string _videoUrl = "";
    [ObservableProperty] private string _webVideoUrl = "";


    public PlayerViewModel(MovieTvService movieTvService, DatabaseService databaseService)
    {
        _movieTvService = movieTvService;
        _databaseService = databaseService;
    }

    public async Task SetOnLineVideoAsync(VedioSearchResult? videoSearchResult)
    {
        if (videoSearchResult == null) return;
        _videoSearchResult = videoSearchResult;
        await LoadDetailAsync(videoSearchResult.Source, videoSearchResult.Id);
        SelectedPlaySurface = "web播放器";
    }

    public async Task SetLocalVideoAsync(FileResult? fileResult)
    {
        if (fileResult == null) return;
        await LoadDetailAsync(fileResult.FullPath, null);
        SelectedPlaySurface = "maui播放器";
    }

    public async Task LoadDetailAsync(string source, string? vodId)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (vodId is null)
            {
                if (File.Exists(source))
                {
                    // 本地视频
                    var detail = new DetailResult
                    {
                        SourceName = "本地视频",
                        Source = "local undefined",
                        Title = Path.GetFileNameWithoutExtension(source),
                        Area = "本地",
                        Year = "9527",
                        Type = "本地",
                        Director = "未知",
                        Actor = "未知",
                        Desc = "",
                        VodId = "local undefined",
                        DetailUrl = "local undefined",
                        Episodes =
                        [
                            new EpisodeSubject
                            {
                                Name = "第1集",
                                Url = source
                            }
                        ]
                    };
                    VideoDetail = detail;
                }
                else
                {
                    IsBusy = false;
                    return;
                }
            }
            else
            {
                // 在线视频
                var detail = await _movieTvService.SearchDetail(source, vodId);
                if (detail == null) return;
                detail.SourceName ??= _videoSearchResult.SourceName;
                detail.Title ??= _videoSearchResult.Name;
                detail.Area ??= "太阳系";
                detail.Year ??= _videoSearchResult.Year.ToString();
                detail.Type ??= _videoSearchResult.Tag;
                detail.Director ??= "未知";
                detail.Actor ??= "未知";
                //detail.Desc只要<p>标签的内容
                detail.Desc = detail.Desc?.Split("<p>")[1]?.Split("</p>")[0] ?? "";
                VideoDetail = detail;
            }

            Episodes.Clear();
            if (VideoDetail.Episodes != null)
            {
                Episodes = VideoDetail.Episodes.Select(ep => new EpisodeSubjectItem
                {
                    Watched = vodId is null,
                    Name = ep.Name,
                    Url = ep.Url,
                    IsSelected = true // 默认全部选中
                }).ToList();
            }

            EpisodesCountText = $"共{Episodes.Count}集";
            var viewHistory =
                await _databaseService.GetPlayHistoryAsync(VideoDetail.VodId, VideoDetail.Source, VideoDetail.Title);
            if (viewHistory is not null)
            {
                Episodes[Episodes.IndexOf(Episodes.FirstOrDefault(ep => ep.Name == viewHistory.Episode))].Watched =
                    true;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectEpisode(EpisodeSubjectItem? episode)
    {
        if (episode == null || episode.Url == null) return;
        _videoUrl = episode.Url;
        if (IsMauiPlayer)
        {
            MauiVideoUrl = _videoUrl;
            WebVideoUrl = "";
        }
        else
        {
            WebVideoUrl = _videoUrl;
            MauiVideoUrl = "";
        }

        // 保存播放历史
        var history = new PlayHistory
        {
            VodId = VideoDetail.VodId,
            Name = VideoDetail.Title,
            Source = VideoDetail.Source,
            SourceName = VideoDetail.SourceName,
            Url = episode.Url,
            Episode = episode.Name,
            TotalEpisodeCount = VideoDetail.Episodes.Count,
            IsLocal = VideoDetail.SourceName == "本地视频"
        };
        Episodes[Episodes.IndexOf(Episodes.FirstOrDefault(ep => ep.Name == episode.Name))].Watched =
            true;
        await _databaseService.AddOrUpdatePlayHistoryAsync(history);
    }

    partial void OnSelectedPlaySurfaceChanged(string value)
    {
        IsMauiPlayer = value == "maui播放器";
        if (IsMauiPlayer)
        {
            MauiVideoUrl = _videoUrl;
            WebVideoUrl = "";
        }
        else
        {
            WebVideoUrl = _videoUrl;
            MauiVideoUrl = "";
        }
    }
}

public partial class EpisodeSubjectItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private bool _watched; //是否观看
}