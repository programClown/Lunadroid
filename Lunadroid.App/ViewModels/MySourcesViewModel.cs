using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

public partial class MySourcesViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly AppConfigService _config;
    private readonly MovieApiService _api;

    [ObservableProperty] private ObservableCollection<MovieSource> _sources = new();
    [ObservableProperty] private bool _isCheckingNetwork;
    [ObservableProperty] private bool _isImportingFromCloud;
    [ObservableProperty] private bool _isLoading;

    public MySourcesViewModel(DatabaseService db, AppConfigService config, MovieApiService api)
    { _db = db; _config = config; _api = api; }

    [RelayCommand] private async Task LoadSourcesAsync()
    { Sources = new ObservableCollection<MovieSource>(await _db.GetMovieSourcesAsync()); }

    [RelayCommand] private async Task ToggleSourceAsync(MovieSource s)
    { s.IsEnabled = !s.IsEnabled; await _db.UpdateMovieSourceAsync(s); }

    [RelayCommand] private async Task DeleteSourceAsync(MovieSource s)
    {
        if (await Shell.Current.DisplayAlert("确认删除", $"确定要删除片源 \"{s.Name}\" 吗？", "确定", "取消"))
        { await _db.DeleteMovieSourceByIdAsync(s.Id); Sources.Remove(s); }
    }

    [RelayCommand] private async Task AddSourceAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("添加片源", "请输入片源名称:", "确定", "取消");
        if (string.IsNullOrWhiteSpace(name)) return;
        var apiUrl = await Shell.Current.DisplayPromptAsync("添加片源", "请输入API地址:", "确定", "取消", placeholder: "https://example.com/api.php/provide/vod/");
        if (string.IsNullOrWhiteSpace(apiUrl)) return;
        var baseUrl = await Shell.Current.DisplayPromptAsync("添加片源", "请输入Base URL (可选):", "确定", "取消");
        var isAdult = await Shell.Current.DisplayAlert("添加片源", "是否包含成人内容？", "是", "否");
        var source = new MovieSource { Name = name.Trim(), ApiUrl = apiUrl.Trim(), BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "" : baseUrl.Trim(), IsAdult = isAdult, IsEnabled = true };
        await _db.AddMovieSourceAsync(source);
        Sources.Add(source);
    }

    [RelayCommand] private async Task ImportFromCloudAsync()
    {
        IsImportingFromCloud = true;
        try
        {
            var url = _config.Config.CloudSourceUrl;
            if (string.IsNullOrWhiteSpace(url)) { await Shell.Current.DisplayAlert("提示", "未配置云端片源URL。", "确定"); return; }
            var cloudSources = await _api.FetchCloudSourcesAsync(url);
            if (cloudSources == null || cloudSources.Count == 0) { await Shell.Current.DisplayAlert("提示", "未找到可用的云端片源。", "确定"); return; }
            var names = cloudSources.Select(s => s.Name).ToArray();
            var selected = await Shell.Current.DisplayActionSheet("选择要导入的片源", "取消", null, names);
            if (selected == null || selected == "取消") return;
            var cs = cloudSources.FirstOrDefault(s => s.Name == selected);
            if (cs == null) return;
            var existing = await _db.GetMovieSourcesAsync();
            if (existing.Any(e => e.ApiUrl.TrimEnd('/') == cs.Api.TrimEnd('/')))
            { await Shell.Current.DisplayAlert("提示", $"片源 \"{cs.Name}\" 已存在。", "确定"); return; }
            var ns = new MovieSource { Name = cs.Name, ApiUrl = cs.Api, BaseUrl = cs.Url, IsAdult = cs.Adult, IsEnabled = true };
            await _db.AddMovieSourceAsync(ns);
            Sources.Add(ns);
            await Shell.Current.DisplayAlert("成功", $"已导入片源: {ns.Name}", "确定");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlert("错误", $"导入失败: {ex.Message}", "确定"); }
        finally { IsImportingFromCloud = false; }
    }

    [RelayCommand] private async Task CheckNetworkAsync()
    {
        IsCheckingNetwork = true;
        try
        {
            foreach (var s in Sources)
            {
                var (ok, ms) = await _api.CheckSourceAccessibilityAsync(s.ApiUrl);
                s.IsAccessible = ok; s.AccessLatencyMs = ms;
                await _db.UpdateMovieSourceAsync(s);
            }
            await LoadSourcesCommand.ExecuteAsync(null);
            await Shell.Current.DisplayAlert("完成", "网络检查已完成。", "确定");
        }
        catch (Exception ex) { await Shell.Current.DisplayAlert("错误", $"检查失败: {ex.Message}", "确定"); }
        finally { IsCheckingNetwork = false; }
    }
}
