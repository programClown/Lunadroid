using CommunityToolkit.Mvvm.ComponentModel;
using Lunadroid.App.Services;
using Lunadroid.Core.Models;

namespace Lunadroid.App.ViewModels;

[QueryProperty(nameof(VideoUrl), "VideoUrl")]
[QueryProperty(nameof(MovieTitle), "MovieTitle")]
[QueryProperty(nameof(EpisodeName), "EpisodeName")]
[QueryProperty(nameof(IsLocal), "IsLocal")]
[QueryProperty(nameof(MovieId), "MovieId")]
[QueryProperty(nameof(EpisodeId), "EpisodeId")]
[QueryProperty(nameof(SourceName), "SourceName")]
[QueryProperty(nameof(PosterUrl), "PosterUrl")]
public partial class PlayerViewModel : ObservableObject
{
    [ObservableProperty] private string _videoUrl = string.Empty;
    [ObservableProperty] private string _movieTitle = string.Empty;
    [ObservableProperty] private string _episodeName = string.Empty;
    [ObservableProperty] private bool _isLocal;
    [ObservableProperty] private string _movieId = string.Empty;
    [ObservableProperty] private int _episodeId;
    [ObservableProperty] private string _sourceName = string.Empty;
    [ObservableProperty] private string _posterUrl = string.Empty;
    [ObservableProperty] private double _currentPosition;
    [ObservableProperty] private double _duration;

    public async Task SavePlayHistoryAsync()
    {
        if (string.IsNullOrEmpty(VideoUrl)) return;
        var history = new PlayHistory
        {
            MovieId = MovieId, MovieTitle = MovieTitle, PosterUrl = PosterUrl,
            EpisodeId = EpisodeId, EpisodeName = EpisodeName, PlayUrl = VideoUrl,
            SourceName = SourceName, ProgressSeconds = CurrentPosition,
            DurationSeconds = Duration, IsLocal = IsLocal
        };
        await AppServices.Database.AddOrUpdatePlayHistoryAsync(history);
    }
}
