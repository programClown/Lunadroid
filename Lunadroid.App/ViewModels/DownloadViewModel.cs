using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;
using M3U8Download;
using UraniumUI.Extensions;
using Environment = Android.OS.Environment;

namespace Lunadroid.App.ViewModels;

public partial class DownloadViewModel : BaseViewModel
{
    private readonly Dictionary<int, CancellationTokenSource> _cancellationTokenSources = new();
    private readonly DatabaseService _databaseService;
    private readonly HlsDownloadService _hlsDownloadService;
    [ObservableProperty] private string _downloadName = string.Empty;

    [ObservableProperty] private string _downloadUrl = string.Empty;
    [ObservableProperty] private bool _hasDownloadedItems;
    [ObservableProperty] private bool _hasDownloadingItems;

    public DownloadViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _hlsDownloadService = new HlsDownloadService();
        Title = "下载管理";
    }

    public ObservableCollection<MediaDownload> DownloadingItems { get; } = [];
    public ObservableCollection<MediaDownload> DownloadedItems { get; } = [];

    public async Task LoadDownloadsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        await Task.Run(async () =>
        {
            var downloadManager = new DownloadManager();
            downloadManager.ExternalPath = Path.Combine(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments)!.AbsolutePath, "m3u8");
            await downloadManager.DownloadAsync("https://vod.360zyx.vip/20250708/7T2xjBRd/index.m3u8", Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDocuments)!.AbsolutePath, "曼达洛人");
        }).ConfigureAwait(false);
        
        try
        {
            var downloadingList = await _databaseService.GetDownloadingRecordsAsync();
            var downloadedList = await _databaseService.GetDownloadRecordsAsync();

            DownloadingItems.Clear();
            foreach (var item in downloadingList)
            {
                DownloadingItems.Add(item);
            }

            DownloadedItems.Clear();
            foreach (var item in downloadedList)
            {
                DownloadedItems.Add(item);
            }

            HasDownloadingItems = DownloadingItems.Count > 0;
            HasDownloadedItems = DownloadedItems.Count > 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl)) return;

        var name = string.IsNullOrWhiteSpace(DownloadName) ? $"视频_{DateTime.Now:yyyyMMdd_HHmmss}" : DownloadName;
        await AddAndStartDownloadAsync(string.Empty, name, string.Empty, DownloadUrl);

        DownloadUrl = string.Empty;
        DownloadName = string.Empty;
    }

    public async Task AddAndStartDownloadAsync(string source, string name, string episode, string url)
    {
        var publicDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMovies);
        var saveDir = Path.Combine(publicDir!.AbsolutePath, "Lunadroid");
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        }

        var fileName = $"{name}_{episode}.ts".Replace(' ', '_');
        if (string.IsNullOrEmpty(episode))
        {
            fileName = $"{name}.ts".Replace(' ', '_');
        }

        var record = new MediaDownload
        {
            Source = source,
            Name = name,
            Episode = episode,
            Url = url,
            LocalPath = Path.Combine(saveDir, fileName),
            IsDownloaded = false,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        await _databaseService.AddDownloadRecordAsync(record);

        DownloadingItems.Insert(0, record);
        HasDownloadingItems = true;

        _ = DoDownloadAsync(record, saveDir, fileName);
    }

    private async Task DoDownloadAsync(MediaDownload record, string saveDir, string fileName)
    {
        var cts = new CancellationTokenSource();
        _cancellationTokenSources[record.Id] = cts;

        try
        {
            UpdateRecordStatus(record, "下载中");

            _hlsDownloadService.ProgressChanged = progress =>
            {
                MainThread.BeginInvokeOnMainThread(() => { record.UpdateTime = DateTime.Now; });
            };

            _hlsDownloadService.StatusChanged = status =>
            {
                MainThread.BeginInvokeOnMainThread(() => { record.UpdateTime = DateTime.Now; });
            };

            var outputPath = await _hlsDownloadService.DownloadHlsAsync(
                record.Url!, saveDir, fileName, cts.Token);

            record.IsDownloaded = true;
            record.LocalPath = outputPath;
            record.UpdateTime = DateTime.Now;
            await _databaseService.UpdateDownloadRecordAsync(record);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                DownloadingItems.Remove(record);
                DownloadedItems.Insert(0, record);
                HasDownloadingItems = DownloadingItems.Count > 0;
                HasDownloadedItems = DownloadedItems.Count > 0;
            });
        }
        catch (OperationCanceledException)
        {
            UpdateRecordStatus(record, "已取消");
        }
        catch (Exception ex)
        {
            UpdateRecordStatus(record, $"失败: {ex.Message}");
        }
        finally
        {
            _cancellationTokenSources.Remove(record.Id);
        }
    }

    private void UpdateRecordStatus(MediaDownload record, string status)
    {
        MainThread.BeginInvokeOnMainThread(() => { record.UpdateTime = DateTime.Now; });
        _ = _databaseService.UpdateDownloadRecordAsync(record);
    }

    [RelayCommand]
    private void CancelDownload(MediaDownload? item)
    {
        if (item == null) return;
        if (_cancellationTokenSources.TryGetValue(item.Id, out var cts))
        {
            cts.Cancel();
        }
    }

    [RelayCommand]
    private async Task DeleteDownloadAsync(MediaDownload? item)
    {
        if (item == null) return;

        if (_cancellationTokenSources.TryGetValue(item.Id, out var cts))
        {
            cts.Cancel();
            _cancellationTokenSources.Remove(item.Id);
        }

        if (!string.IsNullOrEmpty(item.LocalPath) && File.Exists(item.LocalPath))
        {
            try
            {
                File.Delete(item.LocalPath);
            }
            catch
            {
            }
        }

        await _databaseService.DeleteDownloadRecordAsync(item);

        DownloadingItems.Remove(item);
        DownloadedItems.Remove(item);
        HasDownloadingItems = DownloadingItems.Count > 0;
        HasDownloadedItems = DownloadedItems.Count > 0;
    }

    [RelayCommand]
    private async Task PlayDownloadedAsync(MediaDownload? item)
    {
        if (item == null || string.IsNullOrEmpty(item.LocalPath) || !File.Exists(item.LocalPath)) return;

        var localPath = item.LocalPath;
        await Shell.Current.GoToAsync($"PlayerPage?Local={Uri.EscapeDataString(localPath)}");
    }
}