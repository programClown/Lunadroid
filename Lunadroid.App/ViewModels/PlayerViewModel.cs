using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Models;
using Lunadroid.App.Services;
using System.Collections.ObjectModel;

namespace Lunadroid.App.ViewModels;

public partial class PlayerViewModel : BaseViewModel
{
    private readonly MovieTvService _movieTvService;

    [ObservableProperty] private string _actor = string.Empty;

    [ObservableProperty] private string _area = string.Empty;

    [ObservableProperty] private string _cover = string.Empty;

    [ObservableProperty] private string _desc = string.Empty;

    [ObservableProperty] private string _director = string.Empty;

    [ObservableProperty] private EpisodeSubject? _selectedEpisode;

    [ObservableProperty] private string _sourceName = string.Empty;

    [ObservableProperty] private string _title = string.Empty;

    [ObservableProperty] private string _type = string.Empty;

    [ObservableProperty] private string _videoUrl = string.Empty;

    [ObservableProperty] private string _year = string.Empty;

    public PlayerViewModel(MovieTvService movieTvService)
    {
        _movieTvService = movieTvService;
    }

    public ObservableCollection<EpisodeSubject> Episodes { get; } = [];

    public async Task LoadDetailAsync(string source, string vodId, bool isAdult = false)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            DetailResult? detail = await _movieTvService.SearchDetail(source, vodId, isAdult);
            if (detail == null) return;

            Title = detail.Title ?? string.Empty;
            Cover = detail.Cover ?? string.Empty;
            Desc = detail.Desc ?? string.Empty;
            Type = detail.Type ?? string.Empty;
            Year = detail.Year ?? string.Empty;
            Area = detail.Area ?? string.Empty;
            Director = detail.Director ?? string.Empty;
            Actor = detail.Actor ?? string.Empty;
            SourceName = detail.SourceName ?? string.Empty;

            Episodes.Clear();
            if (detail.Episodes is { Count: > 0 })
            {
                foreach (EpisodeSubject ep in detail.Episodes)
                {
                    Episodes.Add(ep);
                }

                SelectedEpisode = Episodes[0];
                VideoUrl = Episodes[0].Url ?? string.Empty;
            }
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
    private void SelectEpisode(EpisodeSubject? episode)
    {
        if (episode == null) return;
        SelectedEpisode = episode;
        VideoUrl = episode.Url ?? string.Empty;
    }
}