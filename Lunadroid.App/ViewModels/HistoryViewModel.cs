using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    [ObservableProperty] private ObservableCollection<PlayHistory> _playHistoryItems = new();
    [ObservableProperty] private ObservableCollection<DownloadRecord> _downloadHistoryItems = new();
    [ObservableProperty] private string _selectedTimeFilter = "All";
    [ObservableProperty] private string _downloadSearchText = string.Empty;
    [ObservableProperty] private string _activeTab = "Play";

    public bool IsPlayTabActive => ActiveTab == "Play";
    public bool IsDownloadTabActive => ActiveTab == "Download";

    public HistoryViewModel(DatabaseService db) { _db = db; }

    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsPlayTabActive));
        OnPropertyChanged(nameof(IsDownloadTabActive));
    }

    [RelayCommand]
    private void SwitchTab(string tab)
    {
        ActiveTab = tab;
        if (tab == "Play") _ = LoadPlayHistoryAsync();
        else _ = LoadDownloadHistoryAsync();
    }

    [RelayCommand] private async Task LoadPlayHistoryAsync()
    { try { PlayHistoryItems = new ObservableCollection<PlayHistory>(await GetFilteredPlayHistoriesAsync()); } catch {} }

    [RelayCommand] private async Task LoadDownloadHistoryAsync()
    { try { DownloadHistoryItems = new ObservableCollection<DownloadRecord>(await GetFilteredDownloadRecordsAsync()); } catch {} }

    [RelayCommand]
    private void FilterPlayTime(string filter)
    {
        SelectedTimeFilter = filter;
        _ = LoadPlayHistoryAsync();
        _ = LoadDownloadHistoryAsync();
    }

    [RelayCommand]
    private void FilterDownloadStatus(string status)
    {
        SelectedTimeFilter = status;
        _ = LoadDownloadHistoryAsync();
    }

    [RelayCommand]
    private async Task DeletePlayHistoryAsync(PlayHistory item)
    {
        if (item == null) return;
        await _db.DeletePlayHistoryByIdAsync(item.Id);
        PlayHistoryItems.Remove(item);
    }

    [RelayCommand]
    private async Task DeleteDownloadHistoryAsync(DownloadRecord item)
    {
        if (item == null) return;
        await _db.DeleteDownloadRecordByIdAsync(item.Id);
        DownloadHistoryItems.Remove(item);
    }

    [RelayCommand] private async Task ClearPlayHistoryAsync()
    {
        if (await Shell.Current.DisplayAlert("确认", "确定要清除所有播放记录吗？", "确定", "取消"))
        { await _db.ClearPlayHistoriesAsync(); PlayHistoryItems.Clear(); }
    }

    [RelayCommand] private async Task ClearDownloadRecordsAsync()
    {
        if (await Shell.Current.DisplayAlert("确认", "确定要清除所有下载记录吗？", "确定", "取消"))
        { await _db.ClearDownloadRecordsAsync(); DownloadHistoryItems.Clear(); }
    }

    [RelayCommand] private async Task SearchDownloadsAsync()
    {
        if (string.IsNullOrWhiteSpace(DownloadSearchText))
            await LoadDownloadHistoryAsync();
        else
            DownloadHistoryItems = new ObservableCollection<DownloadRecord>(await _db.SearchDownloadRecordsAsync(DownloadSearchText));
    }

    [RelayCommand] private async Task PlayFromHistoryAsync(PlayHistory h)
    {
        if (h == null) return;
        await Shell.Current.GoToAsync("PlayerPage", new Dictionary<string, object>
        { { "VideoUrl", h.PlayUrl }, { "MovieTitle", h.MovieTitle }, { "EpisodeName", h.EpisodeName },
          { "IsLocal", h.IsLocal }, { "MovieId", h.MovieId }, { "EpisodeId", h.EpisodeId },
          { "SourceName", h.SourceName }, { "PosterUrl", h.PosterUrl } });
    }

    [RelayCommand] private async Task PlayDownloadAsync(DownloadRecord r)
    {
        if (r == null) return;
        if (r.Status != "Completed" || string.IsNullOrEmpty(r.LocalFilePath))
        { await Shell.Current.DisplayAlert("提示", "该下载尚未完成，无法播放。", "确定"); return; }
        await Shell.Current.GoToAsync("PlayerPage", new Dictionary<string, object>
        { { "VideoUrl", r.LocalFilePath }, { "MovieTitle", r.MovieTitle }, { "EpisodeName", r.EpisodeName },
          { "IsLocal", true }, { "MovieId", r.MovieId }, { "EpisodeId", r.EpisodeId },
          { "SourceName", r.SourceName }, { "PosterUrl", r.PosterUrl } });
    }

    private async Task<List<PlayHistory>> GetFilteredPlayHistoriesAsync()
    {
        var now = DateTime.Now;
        return SelectedTimeFilter switch
        {
            "Day" => await _db.GetPlayHistoriesByDateAsync(now),
            "Month" => await _db.GetPlayHistoriesByMonthAsync(now.Year, now.Month),
            "Year" => await _db.GetPlayHistoriesByYearAsync(now.Year),
            _ => await _db.GetPlayHistoriesAsync()
        };
    }

    private async Task<List<DownloadRecord>> GetFilteredDownloadRecordsAsync()
    {
        var now = DateTime.Now;
        return SelectedTimeFilter switch
        {
            "Day" => await _db.GetDownloadRecordsByDateAsync(now),
            "Month" => await _db.GetDownloadRecordsByMonthAsync(now.Year, now.Month),
            "Year" => await _db.GetDownloadRecordsByYearAsync(now.Year),
            _ => await _db.GetDownloadRecordsAsync()
        };
    }
}
