using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Models;
using Lunadroid.App.Services;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly MovieTvService _movieTvService;

    [ObservableProperty] private bool _hasSearchResults;

    [ObservableProperty] private bool _isSearching;

    [ObservableProperty] private bool _isSearchOngoing;

    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchStatusText = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;

    public HomeViewModel(DatabaseService databaseService, MovieTvService movieTvService)
    {
        _databaseService = databaseService;
        _movieTvService = movieTvService;
    }

    public ObservableCollection<VedioSearchResult> SearchResults { get; } = [];

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        var keyword = SearchText.Trim();

        SearchResults.Clear();
        HasSearchResults = false;
        IsSearching = true;
        IsSearchOngoing = true;
        SearchStatusText = "正在搜索，请耐心等待...";

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            foreach (var api in AppSettings.SelectApis)
            {
                var ones = await _movieTvService.Search(api, keyword);
                ones.ForEach(x => SearchResults.Add(x));

                if (SearchResults.Count >= AppSettings.SearchMaxVideos) break;
            }

            if (SearchResults.Count == 0)
            {
                SearchStatusText = "没有可用的影视源，请先在\"我的\"中添加影视源";
                IsSearching = false;
                IsSearchOngoing = false;
                return;
            }

            SearchStatusText = SearchResults.Count > 0
                ? $"搜索完成，共找到 {SearchResults.Count} 个结果"
                : "未找到相关影视资源";
        }
        catch (OperationCanceledException)
        {
            SearchStatusText = $"搜索已停止，已找到 {SearchResults.Count} 个结果";
        }
        finally
        {
            IsSearching = false;
            IsSearchOngoing = false;
        }
    }

    [RelayCommand]
    private void StopSearch()
    {
        _searchCts?.Cancel();
    }

    [RelayCommand]
    private async Task PickLocalFileAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    {
                        DevicePlatform.Android,
                        new[] { "video/*", "application/x-mpegURL", "application/vnd.apple.mpegurl" }
                    }
                })
            });

            if (result != null)
            {
                await Shell.Current.GoToAsync(
                    $"player?playUrl={Uri.EscapeDataString(result.FullPath)}&title={Uri.EscapeDataString(result.FileName)}&isLocal=true");
            }
        }
        catch (Exception)
        {
            Logger.Error("Pick local file failed");
        }
    }

    [RelayCommand]
    private async Task MovieTappedAsync(VedioSearchResult? movie)
    {
        if (movie == null) return;

        // var episodesParam = Uri.EscapeDataString(JsonSerializer.Serialize(detail.Episodes));
        // await Shell.Current.GoToAsync(
        //     $"moviedetail?movieId={Uri.EscapeDataString(movie.Id)}&title={Uri.EscapeDataString(movie.Title)}&poster={Uri.EscapeDataString(movie.PosterUrl)}&rating={movie.Rating}&sourceName={Uri.EscapeDataString(movie.SourceName)}&episodes={episodesParam}");
    }

    [RelayCommand]
    private async Task SearchFromHistoryAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        SearchText = keyword;
        await SearchCommand.ExecuteAsync(null);
    }
}