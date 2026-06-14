using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lunadroid.App.Services;
using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.App.ViewModels;

[QueryProperty(nameof(MovieData), "MovieData")]
public partial class MovieDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private MovieSearchResult? _movieData;

    [ObservableProperty]
    private MovieDetailResult? _detail;

    [ObservableProperty]
    private ObservableCollection<MovieEpisode> _episodes = new();

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadDetailAsync()
    {
        if (MovieData == null) return;
        IsLoading = true;
        try
        {
            var source = await AppServices.Database.GetMovieSourceByIdAsync(MovieData.SourceId);
            if (source == null) return;
            var result = await AppServices.MovieApi.GetDetailAsync(source, MovieData.DetailUrl);
            if (result == null) return;
            Detail = result;

            // Save movie to DB
            var movie = new Movie
            {
                Id = MovieData.Id, Title = result.Title, PosterUrl = result.PosterUrl,
                Rating = result.Rating, SourceName = MovieData.SourceName, SourceId = MovieData.SourceId,
                Description = result.Description, Year = result.Year, Category = result.Category,
                DetailUrl = MovieData.DetailUrl
            };
            await AppServices.Database.InsertOrUpdateMovieAsync(movie);

            // Save episodes
            await AppServices.Database.DeleteEpisodesByMovieIdAsync(movie.Id);
            var eps = result.Episodes.Select((e, i) => new MovieEpisode
            {
                MovieId = movie.Id, EpisodeName = e.Name, PlayUrl = e.PlayUrl, EpisodeIndex = i
            }).ToList();
            if (eps.Count > 0) await AppServices.Database.InsertEpisodesAsync(eps);
            Episodes = new ObservableCollection<MovieEpisode>(eps);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("错误", $"加载详情失败: {ex.Message}", "确定");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PlayEpisodeAsync(MovieEpisode ep)
    {
        if (ep == null) return;
        await Shell.Current.GoToAsync(nameof(Pages.PlayerPage), new Dictionary<string, object>
        {
            { "VideoUrl", ep.PlayUrl }, { "MovieTitle", MovieData?.Title ?? "" },
            { "EpisodeName", ep.EpisodeName }, { "IsLocal", false },
            { "MovieId", MovieData?.Id ?? "" }, { "EpisodeId", ep.Id },
            { "SourceName", MovieData?.SourceName ?? "" }, { "PosterUrl", MovieData?.PosterUrl ?? "" }
        });
    }

    [RelayCommand]
    private async Task DownloadEpisodeAsync(MovieEpisode ep)
    {
        if (ep == null || MovieData == null) return;
        var downloadDir = Path.Combine(FileSystem.AppDataDirectory, "downloads");
        var fileName = $"{MovieData.Title}_{ep.EpisodeName}".Replace(" ", "_");

        var record = new DownloadRecord
        {
            MovieId = MovieData.Id, MovieTitle = MovieData.Title, PosterUrl = MovieData.PosterUrl,
            EpisodeId = ep.Id, EpisodeName = ep.EpisodeName, SourceUrl = ep.PlayUrl,
            SourceName = MovieData.SourceName, Status = "Downloading"
        };
        var recordId = await AppServices.Database.InsertDownloadRecordAsync(record);

        try
        {
            var hls = AppServices.HlsDownload;
            hls.ProgressChanged += p => { record.DownloadProgress = p; _ = AppServices.Database.UpdateDownloadRecordAsync(record); };
            var localPath = await hls.DownloadHlsAsync(ep.PlayUrl, downloadDir, fileName);
            var fi = new FileInfo(localPath);
            record.LocalFilePath = localPath;
            record.FileSizeBytes = fi.Length;
            record.Status = "Completed";
            record.CompletedAt = DateTime.Now;
            record.DownloadProgress = 100;
        }
        catch { record.Status = "Failed"; }
        finally { await AppServices.Database.UpdateDownloadRecordAsync(record); }
    }
}
