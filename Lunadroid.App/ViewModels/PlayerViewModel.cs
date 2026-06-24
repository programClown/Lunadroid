using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Models;
using Lunadroid.App.Services;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class PlayerViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly MovieTvService _movieTvService;

    [ObservableProperty] private List<EpisodeSubjectItem> _episodes = new();

    [ObservableProperty] private string _episodesCountText;
    [ObservableProperty] private EpisodeSubjectItem? _selectedEpisode;
    [ObservableProperty] private DetailResult? _videoDetail;
    [ObservableProperty] private string _videoUrl = "https://vod.360zyx.vip/20250708/7T2xjBRd/index.m3u8";

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
            // var viewHistory = _viewHistoryTable.GetSingle(his =>
            //     his.VodId == VideoDetail.VodId && his.Source == SourceName && his.Name == VideoName);
            // if (viewHistory is not null)
            // {
            //     Episodes[Episodes.IndexOf(Episodes.FirstOrDefault(ep => ep.Name == viewHistory.Episode))].Watched =
            //         true;
            // }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LoadFromUrl(string url, string title)
    {
        Title = title;
        VideoUrl = url;
    }

    [RelayCommand]
    private async Task SelectEpisodeAsync(EpisodeSubjectItem? episode)
    {
        if (episode == null) return;
        SelectedEpisode = episode;
        VideoUrl = episode.Url ?? string.Empty;
    }
}

public partial class EpisodeSubjectItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string? _name;
    [ObservableProperty] private string? _url;
    [ObservableProperty] private bool _watched; //是否观看
}