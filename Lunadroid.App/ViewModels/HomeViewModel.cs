using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Models;
using Lunadroid.App.Services;
using Lunadroid.App.Views;
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
        MainThread.BeginInvokeOnMainThread(async void () =>
        {
            try
            {
                var apiSources = await _databaseService.GetEnabledApiSourcesAsync();
                AppSettings.UpdateSites(apiSources);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load api sources: {e.Message}");
            }
        });
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
                if (token.IsCancellationRequested) break;

                var ones = await _movieTvService.Search(api, keyword);
                // ones.ForEach(x => SearchResults.Add(x));
                foreach (var vedioSearchResult in ones)
                {
                    SearchResults.Add(vedioSearchResult);
                }

                HasSearchResults = SearchResults.Count > 0;
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
                var navigationParameters = new Dictionary<string, object>
                {
                    { "Local", result }
                };
                await Shell.Current.GoToAsync(nameof(PlayerPage), navigationParameters);
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

        var navigationParameters = new Dictionary<string, object>
        {
            { "Online", movie }
        };
        await Shell.Current.GoToAsync(nameof(PlayerPage), navigationParameters);
    }

    [RelayCommand]
    private async Task SearchFromHistoryAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        SearchText = keyword;
        await SearchCommand.ExecuteAsync(null);
    }
}