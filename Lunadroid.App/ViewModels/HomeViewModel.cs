using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Services;
using Lunadroid.Core.Models;

namespace Lunadroid.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private ObservableCollection<MovieSearchResult> _searchResults = new();

    [ObservableProperty]
    private ObservableCollection<SearchHistory> _searchHistories = new();

    public bool HasSearchHistories => SearchHistories.Count > 0;

    public HomeViewModel()
    {
        SearchHistories.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSearchHistories));
        try
        {
            _ = LoadSearchHistoryAsync();
        }
        catch (Exception ex)
        {
            Lunadroid.Core.Services.LoggingService.Error($"HomeViewModel ctor DB access failed: {ex.Message}", ex);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        IsSearching = true;
        try
        {
            await AppServices.Database.AddSearchHistoryAsync(SearchText.Trim());
            var sources = await AppServices.Database.GetEnabledMovieSourcesAsync();
            var tasks = sources.Select(s => AppServices.MovieApi.SearchAsync(s, SearchText.Trim(), token));
            var results = await Task.WhenAll(tasks);
            var allResults = results.SelectMany(r => r).ToList();
            MainThread.BeginInvokeOnMainThread(() =>
                SearchResults = new ObservableCollection<MovieSearchResult>(allResults));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
                await Shell.Current.DisplayAlert("搜索失败", ex.Message, "确定"));
        }
        finally
        {
            IsSearching = false;
            _cts?.Dispose();
            _cts = null;
            await LoadSearchHistoryAsync();
        }
    }

    [RelayCommand]
    private void CancelSearch() => _cts?.Cancel();

    [RelayCommand]
    private async Task DeleteHistoryAsync(int id)
    {
        await AppServices.Database.DeleteSearchHistoryByIdAsync(id);
        await LoadSearchHistoryAsync();
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await AppServices.Database.ClearSearchHistoriesAsync();
        await LoadSearchHistoryAsync();
    }

    [RelayCommand]
    private async Task SelectHistoryAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return;
        SearchText = keyword;
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SelectMovieAsync(MovieSearchResult movie)
    {
        if (movie == null) return;
        await Shell.Current.GoToAsync(nameof(Pages.MovieDetailPage),
            new Dictionary<string, object> { { "MovieData", movie } });
    }

    private async Task LoadSearchHistoryAsync()
    {
        var histories = await AppServices.Database.GetSearchHistoriesAsync();
        MainThread.BeginInvokeOnMainThread(() =>
            SearchHistories = new ObservableCollection<SearchHistory>(histories));
    }
}
