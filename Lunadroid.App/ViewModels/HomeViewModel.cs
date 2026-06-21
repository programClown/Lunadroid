using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly MovieApiService _movieApiService;

    [ObservableProperty] private bool _hasSearchResults;

    [ObservableProperty] private bool _isSearching;

    [ObservableProperty] private bool _isSearchOngoing;

    private CancellationTokenSource? _searchCts;

    [ObservableProperty] private string _searchStatusText = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _showSearchHistory = true;

    public HomeViewModel(DatabaseService databaseService, MovieApiService movieApiService)
    {
        _databaseService = databaseService;
        _movieApiService = movieApiService;
    }

    public ObservableCollection<MovieSearchResult> SearchResults { get; } = [];
    public ObservableCollection<SearchHistory> SearchHistories { get; } = [];

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        var keyword = SearchText.Trim();

        await _databaseService.AddSearchHistoryAsync(keyword);
        await LoadSearchHistoriesAsync();

        SearchResults.Clear();
        HasSearchResults = false;
        ShowSearchHistory = false;
        IsSearching = true;
        IsSearchOngoing = true;
        SearchStatusText = "正在搜索，请耐心等待...";

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            var sources = await _databaseService.GetEnabledMovieSourcesAsync();
            if (sources.Count == 0)
            {
                SearchStatusText = "没有可用的影视源，请先在\"我的\"中添加影视源";
                IsSearching = false;
                IsSearchOngoing = false;
                return;
            }

            var completedCount = 0;
            var totalSources = sources.Count;

            var tasks = sources.Select(async source =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var results = await _movieApiService.SearchAsync(source, keyword, token);

                    if (results.Count > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            foreach (var result in results)
                            {
                                if (!SearchResults.Any(r => r.Id == result.Id))
                                {
                                    SearchResults.Add(result);
                                }
                            }

                            HasSearchResults = SearchResults.Count > 0;
                        });
                    }

                    Interlocked.Increment(ref completedCount);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SearchStatusText =
                            $"已搜索 {completedCount}/{totalSources} 个源，找到 {SearchResults.Count} 个结果...";
                    });
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref completedCount);
                }
            }).ToList();

            await Task.WhenAll(tasks);

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
        }
    }

    [RelayCommand]
    private async Task MovieTappedAsync(MovieSearchResult movie)
    {
        if (movie == null) return;

        var source = await _databaseService.GetMovieSourceByIdAsync(movie.SourceId);
        if (source == null) return;

        var detail = await _movieApiService.GetDetailAsync(source, movie.DetailUrl);
        if (detail == null) return;

        var dbMovie = new Movie
        {
            Id = movie.Id,
            Title = movie.Title,
            PosterUrl = movie.PosterUrl,
            Rating = movie.Rating,
            SourceName = movie.SourceName,
            SourceId = movie.SourceId,
            Year = movie.Year,
            Category = movie.Category,
            DetailUrl = movie.DetailUrl,
            Description = detail.Description
        };
        await _databaseService.InsertOrUpdateMovieAsync(dbMovie);

        var episodes = detail.Episodes.Select(ep => new MovieEpisode
        {
            MovieId = movie.Id,
            EpisodeName = ep.Name,
            PlayUrl = ep.PlayUrl,
            EpisodeIndex = ep.Index
        }).ToList();

        if (episodes.Count > 0)
        {
            await _databaseService.DeleteEpisodesByMovieIdAsync(movie.Id);
            await _databaseService.InsertEpisodesAsync(episodes);
        }

        var episodesParam = Uri.EscapeDataString(JsonSerializer.Serialize(detail.Episodes));
        await Shell.Current.GoToAsync(
            $"moviedetail?movieId={Uri.EscapeDataString(movie.Id)}&title={Uri.EscapeDataString(movie.Title)}&poster={Uri.EscapeDataString(movie.PosterUrl)}&rating={movie.Rating}&sourceName={Uri.EscapeDataString(movie.SourceName)}&episodes={episodesParam}");
    }

    [RelayCommand]
    private async Task LoadSearchHistoriesAsync()
    {
        var histories = await _databaseService.GetSearchHistoriesAsync();
        SearchHistories.Clear();
        foreach (var h in histories.Take(20))
        {
            SearchHistories.Add(h);
        }
    }

    [RelayCommand]
    private async Task DeleteSearchHistoryAsync(SearchHistory history)
    {
        if (history == null) return;
        await _databaseService.DeleteSearchHistoryByIdAsync(history.Id);
        SearchHistories.Remove(history);
    }

    [RelayCommand]
    private async Task ClearSearchHistoriesAsync()
    {
        await _databaseService.ClearSearchHistoriesAsync();
        SearchHistories.Clear();
    }

    [RelayCommand]
    private async Task SearchFromHistoryAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        SearchText = keyword;
        await SearchCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void ToggleSearchHistory()
    {
        ShowSearchHistory = !ShowSearchHistory;
    }

    public async Task InitializeAsync()
    {
        await LoadSearchHistoriesAsync();
    }
}