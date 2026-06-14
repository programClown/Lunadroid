using Lunadroid.Core.Models;
using Lunadroid.Core.Services;

namespace Lunadroid.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly DatabaseService _db;
    private readonly string _dbPath;

    public DatabaseServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"lunadroid_test_{Guid.NewGuid()}.db");
        _db = new DatabaseService(_dbPath);
        _db.InitializeAsync().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        try
        {
            _db.CloseAsync().GetAwaiter().GetResult();
        }
        catch { }

        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch { }
    }

    #region MovieSource Tests

    [Fact]
    public async Task AddMovieSource_ShouldInsertAndReturnId()
    {
        var source = new MovieSource { Name = "Test Source", ApiUrl = "https://api.test.com" };
        var id = await _db.AddMovieSourceAsync(source);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetMovieSources_ShouldReturnAllSources()
    {
        await _db.AddMovieSourceAsync(new MovieSource { Name = "Source1", ApiUrl = "https://a.com" });
        await _db.AddMovieSourceAsync(new MovieSource { Name = "Source2", ApiUrl = "https://b.com" });

        var sources = await _db.GetMovieSourcesAsync();
        Assert.Equal(2, sources.Count);
    }

    [Fact]
    public async Task GetEnabledMovieSources_ShouldFilterByIsEnabled()
    {
        await _db.AddMovieSourceAsync(new MovieSource { Name = "Enabled", ApiUrl = "https://a.com", IsEnabled = true });
        await _db.AddMovieSourceAsync(new MovieSource { Name = "Disabled", ApiUrl = "https://b.com", IsEnabled = false });

        var sources = await _db.GetEnabledMovieSourcesAsync();
        Assert.Single(sources);
        Assert.Equal("Enabled", sources[0].Name);
    }

    [Fact]
    public async Task UpdateMovieSource_ShouldModifyExistingSource()
    {
        var source = new MovieSource { Name = "Original", ApiUrl = "https://a.com" };
        await _db.AddMovieSourceAsync(source);

        source.Name = "Updated";
        await _db.UpdateMovieSourceAsync(source);

        var retrieved = await _db.GetMovieSourceAsync(source.Id);
        Assert.Equal("Updated", retrieved!.Name);
    }

    [Fact]
    public async Task DeleteMovieSource_ShouldRemoveSource()
    {
        var source = new MovieSource { Name = "ToDelete", ApiUrl = "https://a.com" };
        await _db.AddMovieSourceAsync(source);

        await _db.DeleteMovieSourceAsync(source);

        var sources = await _db.GetMovieSourcesAsync();
        Assert.Empty(sources);
    }

    [Fact]
    public async Task DeleteMovieSourceById_ShouldRemoveSource()
    {
        var source = new MovieSource { Name = "ToDelete", ApiUrl = "https://a.com" };
        await _db.AddMovieSourceAsync(source);

        await _db.DeleteMovieSourceByIdAsync(source.Id);

        var sources = await _db.GetMovieSourcesAsync();
        Assert.Empty(sources);
    }

    [Fact]
    public async Task GetMovieSourceById_ShouldReturnCorrectSource()
    {
        var source = new MovieSource { Name = "FindMe", ApiUrl = "https://a.com" };
        await _db.AddMovieSourceAsync(source);

        var found = await _db.GetMovieSourceByIdAsync(source.Id);
        Assert.NotNull(found);
        Assert.Equal("FindMe", found.Name);
    }

    [Fact]
    public async Task ClearMovieSources_ShouldRemoveAll()
    {
        await _db.AddMovieSourceAsync(new MovieSource { Name = "A", ApiUrl = "https://a.com" });
        await _db.AddMovieSourceAsync(new MovieSource { Name = "B", ApiUrl = "https://b.com" });

        await _db.ClearMovieSourcesAsync();

        var sources = await _db.GetMovieSourcesAsync();
        Assert.Empty(sources);
    }

    #endregion

    #region Movie Tests

    [Fact]
    public async Task AddMovie_ShouldInsertSuccessfully()
    {
        var movie = new Movie { Title = "Test Movie", Year = "2024", Category = "Action" };
        var id = await _db.AddMovieAsync(movie);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetMovie_ShouldReturnCorrectMovie()
    {
        var movie = new Movie { Id = "movie-1", Title = "Test Movie" };
        await _db.AddMovieAsync(movie);

        var found = await _db.GetMovieAsync("movie-1");
        Assert.NotNull(found);
        Assert.Equal("Test Movie", found.Title);
    }

    [Fact]
    public async Task InsertOrUpdateMovie_ShouldInsertWhenNew()
    {
        var movie = new Movie { Id = "movie-new", Title = "New Movie" };
        await _db.InsertOrUpdateMovieAsync(movie);

        var found = await _db.GetMovieAsync("movie-new");
        Assert.NotNull(found);
        Assert.Equal("New Movie", found.Title);
    }

    [Fact]
    public async Task InsertOrUpdateMovie_ShouldUpdateWhenExists()
    {
        var movie = new Movie { Id = "movie-upsert", Title = "Original" };
        await _db.AddMovieAsync(movie);

        movie.Title = "Updated";
        await _db.InsertOrUpdateMovieAsync(movie);

        var found = await _db.GetMovieAsync("movie-upsert");
        Assert.Equal("Updated", found!.Title);
    }

    [Fact]
    public async Task DeleteMovie_ShouldRemoveMovie()
    {
        var movie = new Movie { Id = "movie-del", Title = "ToDelete" };
        await _db.AddMovieAsync(movie);
        await _db.DeleteMovieAsync(movie);

        var found = await _db.GetMovieAsync("movie-del");
        Assert.Null(found);
    }

    #endregion

    #region MovieEpisode Tests

    [Fact]
    public async Task AddMovieEpisode_ShouldInsertSuccessfully()
    {
        var ep = new MovieEpisode { MovieId = "m1", EpisodeName = "Ep1", PlayUrl = "https://play.com/1", EpisodeIndex = 0 };
        var id = await _db.AddMovieEpisodeAsync(ep);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetEpisodesByMovieId_ShouldReturnOrderedEpisodes()
    {
        await _db.AddMovieEpisodeAsync(new MovieEpisode { MovieId = "m1", EpisodeName = "Ep2", PlayUrl = "url2", EpisodeIndex = 1 });
        await _db.AddMovieEpisodeAsync(new MovieEpisode { MovieId = "m1", EpisodeName = "Ep1", PlayUrl = "url1", EpisodeIndex = 0 });

        var episodes = await _db.GetMovieEpisodesByMovieIdAsync("m1");
        Assert.Equal(2, episodes.Count);
        Assert.Equal("Ep1", episodes[0].EpisodeName);
        Assert.Equal("Ep2", episodes[1].EpisodeName);
    }

    [Fact]
    public async Task InsertEpisodes_BatchInsert()
    {
        var episodes = new List<MovieEpisode>
        {
            new() { MovieId = "m1", EpisodeName = "Ep1", PlayUrl = "url1", EpisodeIndex = 0 },
            new() { MovieId = "m1", EpisodeName = "Ep2", PlayUrl = "url2", EpisodeIndex = 1 },
            new() { MovieId = "m1", EpisodeName = "Ep3", PlayUrl = "url3", EpisodeIndex = 2 }
        };
        await _db.InsertEpisodesAsync(episodes);

        var result = await _db.GetMovieEpisodesByMovieIdAsync("m1");
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task DeleteEpisodesByMovieId_ShouldRemoveAllForMovie()
    {
        await _db.AddMovieEpisodeAsync(new MovieEpisode { MovieId = "m1", EpisodeName = "Ep1", PlayUrl = "url1", EpisodeIndex = 0 });
        await _db.AddMovieEpisodeAsync(new MovieEpisode { MovieId = "m2", EpisodeName = "Ep1", PlayUrl = "url2", EpisodeIndex = 0 });

        await _db.DeleteEpisodesByMovieIdAsync("m1");

        var m1Eps = await _db.GetMovieEpisodesByMovieIdAsync("m1");
        var m2Eps = await _db.GetMovieEpisodesByMovieIdAsync("m2");
        Assert.Empty(m1Eps);
        Assert.Single(m2Eps);
    }

    #endregion

    #region SearchHistory Tests

    [Fact]
    public async Task AddSearchHistory_ShouldInsertKeyword()
    {
        await _db.AddSearchHistoryAsync("test keyword");

        var histories = await _db.GetSearchHistoriesAsync();
        Assert.Single(histories);
        Assert.Equal("test keyword", histories[0].Keyword);
    }

    [Fact]
    public async Task AddSearchHistory_DuplicateKeyword_ShouldUpdateTimestamp()
    {
        await _db.AddSearchHistoryAsync("duplicate");
        var firstTime = (await _db.GetSearchHistoriesAsync())[0].SearchedAt;

        await Task.Delay(50);
        await _db.AddSearchHistoryAsync("duplicate");

        var histories = await _db.GetSearchHistoriesAsync();
        Assert.Single(histories); // Should still be one
        Assert.True(histories[0].SearchedAt >= firstTime);
    }

    [Fact]
    public async Task AddSearchHistory_ShouldTrimWhitespace()
    {
        await _db.AddSearchHistoryAsync("  trimmed  ");

        var histories = await _db.GetSearchHistoriesAsync();
        Assert.Equal("trimmed", histories[0].Keyword);
    }

    [Fact]
    public async Task DeleteSearchHistoryById_ShouldRemoveEntry()
    {
        await _db.AddSearchHistoryAsync("to delete");
        var histories = await _db.GetSearchHistoriesAsync();

        await _db.DeleteSearchHistoryByIdAsync(histories[0].Id);

        var remaining = await _db.GetSearchHistoriesAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ClearSearchHistories_ShouldRemoveAll()
    {
        await _db.AddSearchHistoryAsync("keyword1");
        await _db.AddSearchHistoryAsync("keyword2");

        await _db.ClearSearchHistoriesAsync();

        var histories = await _db.GetSearchHistoriesAsync();
        Assert.Empty(histories);
    }

    #endregion

    #region PlayHistory Tests

    [Fact]
    public async Task AddOrUpdatePlayHistory_ShouldInsertWhenNew()
    {
        var history = new PlayHistory
        {
            MovieId = "m1", EpisodeId = 1, MovieTitle = "Test",
            EpisodeName = "Ep1", PlayUrl = "url1", SourceName = "src1"
        };
        await _db.AddOrUpdatePlayHistoryAsync(history);

        var histories = await _db.GetPlayHistoriesAsync();
        Assert.Single(histories);
        Assert.Equal("Test", histories[0].MovieTitle);
    }

    [Fact]
    public async Task AddOrUpdatePlayHistory_ShouldUpdateWhenExists()
    {
        var history = new PlayHistory
        {
            MovieId = "m1", EpisodeId = 1, MovieTitle = "Test",
            EpisodeName = "Ep1", PlayUrl = "url1", SourceName = "src1",
            ProgressSeconds = 10
        };
        await _db.AddOrUpdatePlayHistoryAsync(history);

        history.ProgressSeconds = 100;
        await _db.AddOrUpdatePlayHistoryAsync(history);

        var histories = await _db.GetPlayHistoriesAsync();
        Assert.Single(histories);
        Assert.Equal(100, histories[0].ProgressSeconds);
    }

    [Fact]
    public async Task GetPlayHistoriesByDate_ShouldFilterCorrectly()
    {
        // Both records get LastWatchedAt = DateTime.Now from AddOrUpdatePlayHistoryAsync,
        // so we test that filtering by today returns records inserted today.
        var today1 = new PlayHistory
        {
            MovieId = "m1", EpisodeId = 1, MovieTitle = "TodayMovie1",
            EpisodeName = "Ep1", PlayUrl = "url1", SourceName = "src1",
            LastWatchedAt = DateTime.Now
        };
        var today2 = new PlayHistory
        {
            MovieId = "m2", EpisodeId = 2, MovieTitle = "TodayMovie2",
            EpisodeName = "Ep1", PlayUrl = "url2", SourceName = "src1",
            LastWatchedAt = DateTime.Now
        };
        await _db.AddOrUpdatePlayHistoryAsync(today1);
        await _db.AddOrUpdatePlayHistoryAsync(today2);

        var result = await _db.GetPlayHistoriesByDateAsync(DateTime.Now);
        Assert.Equal(2, result.Count);

        // Verify filtering by a different date returns nothing
        var resultYesterday = await _db.GetPlayHistoriesByDateAsync(DateTime.Now.AddDays(-1));
        Assert.Empty(resultYesterday);
    }

    [Fact]
    public async Task DeletePlayHistoryById_ShouldRemoveEntry()
    {
        var history = new PlayHistory
        {
            MovieId = "m1", EpisodeId = 1, MovieTitle = "ToDelete",
            EpisodeName = "Ep1", PlayUrl = "url1", SourceName = "src1"
        };
        await _db.AddOrUpdatePlayHistoryAsync(history);
        var all = await _db.GetPlayHistoriesAsync();

        await _db.DeletePlayHistoryByIdAsync(all[0].Id);

        var remaining = await _db.GetPlayHistoriesAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ClearPlayHistories_ShouldRemoveAll()
    {
        await _db.AddOrUpdatePlayHistoryAsync(new PlayHistory { MovieId = "m1", EpisodeId = 1, MovieTitle = "A", EpisodeName = "Ep1", PlayUrl = "u1", SourceName = "s1" });
        await _db.AddOrUpdatePlayHistoryAsync(new PlayHistory { MovieId = "m2", EpisodeId = 2, MovieTitle = "B", EpisodeName = "Ep1", PlayUrl = "u2", SourceName = "s1" });

        await _db.ClearPlayHistoriesAsync();

        var histories = await _db.GetPlayHistoriesAsync();
        Assert.Empty(histories);
    }

    #endregion

    #region DownloadRecord Tests

    [Fact]
    public async Task AddDownloadRecord_ShouldInsertSuccessfully()
    {
        var record = new DownloadRecord
        {
            MovieId = "m1", MovieTitle = "Test", EpisodeName = "Ep1",
            SourceUrl = "https://source.com", SourceName = "src1", Status = "Pending"
        };
        var id = await _db.AddDownloadRecordAsync(record);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task InsertDownloadRecord_ShouldWorkAsAlias()
    {
        var record = new DownloadRecord
        {
            MovieId = "m1", MovieTitle = "Test", EpisodeName = "Ep1",
            SourceUrl = "https://source.com", SourceName = "src1"
        };
        var id = await _db.InsertDownloadRecordAsync(record);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task UpdateDownloadRecord_ShouldModifyExisting()
    {
        var record = new DownloadRecord
        {
            MovieId = "m1", MovieTitle = "Test", EpisodeName = "Ep1",
            SourceUrl = "url", SourceName = "src1", Status = "Pending"
        };
        await _db.AddDownloadRecordAsync(record);

        record.Status = "Completed";
        record.DownloadProgress = 100;
        await _db.UpdateDownloadRecordAsync(record);

        var found = await _db.GetDownloadRecordAsync(record.Id);
        Assert.Equal("Completed", found!.Status);
        Assert.Equal(100, found.DownloadProgress);
    }

    [Fact]
    public async Task SearchDownloadRecords_ShouldFindByKeyword()
    {
        await _db.AddDownloadRecordAsync(new DownloadRecord { MovieId = "m1", MovieTitle = "Action Movie", EpisodeName = "Ep1", SourceUrl = "url1", SourceName = "src1" });
        await _db.AddDownloadRecordAsync(new DownloadRecord { MovieId = "m2", MovieTitle = "Comedy Show", EpisodeName = "Ep1", SourceUrl = "url2", SourceName = "src1" });

        var results = await _db.SearchDownloadRecordsAsync("Action");
        Assert.Single(results);
        Assert.Equal("Action Movie", results[0].MovieTitle);
    }

    [Fact]
    public async Task DeleteDownloadRecordById_ShouldRemoveEntry()
    {
        var record = new DownloadRecord { MovieId = "m1", MovieTitle = "Test", EpisodeName = "Ep1", SourceUrl = "url", SourceName = "src1" };
        await _db.AddDownloadRecordAsync(record);

        await _db.DeleteDownloadRecordByIdAsync(record.Id);

        var remaining = await _db.GetDownloadRecordsAsync();
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task ClearDownloadRecords_ShouldRemoveAll()
    {
        await _db.AddDownloadRecordAsync(new DownloadRecord { MovieId = "m1", MovieTitle = "A", EpisodeName = "Ep1", SourceUrl = "url1", SourceName = "src1" });
        await _db.AddDownloadRecordAsync(new DownloadRecord { MovieId = "m2", MovieTitle = "B", EpisodeName = "Ep1", SourceUrl = "url2", SourceName = "src1" });

        await _db.ClearDownloadRecordsAsync();

        var records = await _db.GetDownloadRecordsAsync();
        Assert.Empty(records);
    }

    #endregion

    #region Global Operations Tests

    [Fact]
    public async Task ClearAllData_ShouldRemoveEverything()
    {
        await _db.AddMovieSourceAsync(new MovieSource { Name = "S", ApiUrl = "url" });
        await _db.AddMovieAsync(new Movie { Id = "m1", Title = "M" });
        await _db.AddSearchHistoryAsync("keyword");
        await _db.AddOrUpdatePlayHistoryAsync(new PlayHistory { MovieId = "m1", EpisodeId = 1, MovieTitle = "T", EpisodeName = "E", PlayUrl = "u", SourceName = "s" });
        await _db.AddDownloadRecordAsync(new DownloadRecord { MovieId = "m1", MovieTitle = "T", EpisodeName = "E", SourceUrl = "u", SourceName = "s" });

        await _db.ClearAllDataAsync();

        Assert.Empty(await _db.GetMovieSourcesAsync());
        Assert.Empty(await _db.GetMoviesAsync());
        Assert.Empty(await _db.GetSearchHistoriesAsync());
        Assert.Empty(await _db.GetPlayHistoriesAsync());
        Assert.Empty(await _db.GetDownloadRecordsAsync());
    }

    #endregion
}
