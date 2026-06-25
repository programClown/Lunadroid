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
        await LoadDetailAsync(videoSearchResult.Source, videoSearchResult.Id);
    }

    public async Task SetLocalVideoAsync(string? playUrl)
    {
        if (playUrl == null) return;
        await LoadDetailAsync(playUrl, null);
    }

    public async Task LoadDetailAsync(string source, string? vodId)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (vodId is null)
            {
                // 本地视频
                return;
            }

            VideoDetail = await _movieTvService.SearchDetail(source, vodId);
            if (VideoDetail == null) return;

            Episodes.Clear();
            if (VideoDetail.Episodes != null)
            {
                Episodes = VideoDetail.Episodes.Select(ep => new EpisodeSubjectItem
                {
                    Watched = false,
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
            IsLocal = false
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