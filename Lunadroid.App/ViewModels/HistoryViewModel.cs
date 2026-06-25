using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;
using System.Collections.ObjectModel;

namespace Lunadroid.App.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    [ObservableProperty] private bool _hasLocalHistories;

    [ObservableProperty] private bool _hasOnlineHistories;

    public HistoryViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "播放历史";
    }

    public ObservableCollection<PlayHistory> OnlineHistories { get; } = [];
    public ObservableCollection<PlayHistory> LocalHistories { get; } = [];

    public async Task LoadHistoriesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            var onlineList = await _databaseService.GetOnlinePlayHistoriesAsync();
            var localList = await _databaseService.GetLocalPlayHistoriesAsync();

            OnlineHistories.Clear();
            foreach (var item in onlineList)
            {
                OnlineHistories.Add(item);
            }

            LocalHistories.Clear();
            foreach (var item in localList)
            {
                LocalHistories.Add(item);
            }

            HasOnlineHistories = OnlineHistories.Count > 0;
            HasLocalHistories = LocalHistories.Count > 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteHistoryAsync(PlayHistory history)
    {
        if (history == null) return;
        await _databaseService.DeletePlayHistoryAsync(history);
        OnlineHistories.Remove(history);
        LocalHistories.Remove(history);
        HasOnlineHistories = OnlineHistories.Count > 0;
        HasLocalHistories = LocalHistories.Count > 0;
    }

    [RelayCommand]
    private async Task ClearOnlineHistoriesAsync()
    {
        var onlineList = await _databaseService.GetOnlinePlayHistoriesAsync();
        foreach (var item in onlineList)
        {
            await _databaseService.DeletePlayHistoryAsync(item);
        }
        OnlineHistories.Clear();
        HasOnlineHistories = false;
    }

    [RelayCommand]
    private async Task ClearLocalHistoriesAsync()
    {
        var localList = await _databaseService.GetLocalPlayHistoriesAsync();
        foreach (var item in localList)
        {
            await _databaseService.DeletePlayHistoryAsync(item);
        }
        LocalHistories.Clear();
        HasLocalHistories = false;
    }
}