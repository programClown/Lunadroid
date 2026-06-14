using SQLite;
using Lunadroid.Core.Models;

namespace Lunadroid.Core.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseService(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
    }

    #region Initialization

    public async Task InitializeAsync()
    {
        await _database.CreateTableAsync<MovieSource>();
        await _database.CreateTableAsync<Movie>();
        await _database.CreateTableAsync<MovieEpisode>();
        await _database.CreateTableAsync<SearchHistory>();
        await _database.CreateTableAsync<PlayHistory>();
        await _database.CreateTableAsync<DownloadRecord>();
    }

    #endregion

    #region MovieSource CRUD

    public async Task<List<MovieSource>> GetMovieSourcesAsync()
    {
        return await _database.Table<MovieSource>().ToListAsync();
    }

    public async Task<List<MovieSource>> GetEnabledMovieSourcesAsync()
    {
        return await _database.Table<MovieSource>().Where(s => s.IsEnabled).ToListAsync();
    }

    public async Task<MovieSource?> GetMovieSourceAsync(int id)
    {
        return await _database.Table<MovieSource>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<MovieSource?> GetMovieSourceByIdAsync(int id)
    {
        return await _database.Table<MovieSource>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> AddMovieSourceAsync(MovieSource source)
    {
        return await _database.InsertAsync(source);
    }

    public async Task<int> UpdateMovieSourceAsync(MovieSource source)
    {
        return await _database.UpdateAsync(source);
    }

    public async Task<int> DeleteMovieSourceAsync(MovieSource source)
    {
        return await _database.DeleteAsync(source);
    }

    public async Task<int> DeleteMovieSourceByIdAsync(int id)
    {
        return await _database.DeleteAsync<MovieSource>(id);
    }

    public async Task<int> ClearMovieSourcesAsync()
    {
        return await _database.DeleteAllAsync<MovieSource>();
    }

    #endregion

    #region Movie CRUD

    public async Task<List<Movie>> GetMoviesAsync()
    {
        return await _database.Table<Movie>().ToListAsync();
    }

    public async Task<Movie?> GetMovieAsync(string id)
    {
        return await _database.Table<Movie>().Where(m => m.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> AddMovieAsync(Movie movie)
    {
        return await _database.InsertAsync(movie);
    }

    public async Task<int> UpdateMovieAsync(Movie movie)
    {
        return await _database.UpdateAsync(movie);
    }

    public async Task<int> DeleteMovieAsync(Movie movie)
    {
        return await _database.DeleteAsync(movie);
    }

    public async Task<int> InsertOrUpdateMovieAsync(Movie movie)
    {
        var existing = await _database.Table<Movie>().Where(m => m.Id == movie.Id).FirstOrDefaultAsync();
        if (existing != null)
            return await _database.UpdateAsync(movie);
        return await _database.InsertAsync(movie);
    }

    public async Task<int> DeleteMovieByIdAsync(string id)
    {
        return await _database.DeleteAsync<Movie>(id);
    }

    public async Task<int> ClearMoviesAsync()
    {
        return await _database.DeleteAllAsync<Movie>();
    }

    #endregion

    #region MovieEpisode CRUD

    public async Task<List<MovieEpisode>> GetMovieEpisodesAsync()
    {
        return await _database.Table<MovieEpisode>().ToListAsync();
    }

    public async Task<List<MovieEpisode>> GetMovieEpisodesByMovieIdAsync(string movieId)
    {
        return await _database.Table<MovieEpisode>()
            .Where(e => e.MovieId == movieId)
            .OrderBy(e => e.EpisodeIndex)
            .ToListAsync();
    }

    public async Task<MovieEpisode?> GetMovieEpisodeAsync(int id)
    {
        return await _database.Table<MovieEpisode>().Where(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> AddMovieEpisodeAsync(MovieEpisode episode)
    {
        return await _database.InsertAsync(episode);
    }

    public async Task InsertEpisodesAsync(List<MovieEpisode> episodes)
    {
        await _database.InsertAllAsync(episodes);
    }

    public async Task<int> UpdateMovieEpisodeAsync(MovieEpisode episode)
    {
        return await _database.UpdateAsync(episode);
    }

    public async Task<int> DeleteMovieEpisodeAsync(MovieEpisode episode)
    {
        return await _database.DeleteAsync(episode);
    }

    public async Task<int> DeleteMovieEpisodeByIdAsync(int id)
    {
        return await _database.DeleteAsync<MovieEpisode>(id);
    }

    public async Task<int> DeleteEpisodesByMovieIdAsync(string movieId)
    {
        return await _database.Table<MovieEpisode>().DeleteAsync(e => e.MovieId == movieId);
    }

    public async Task<int> ClearMovieEpisodesAsync()
    {
        return await _database.DeleteAllAsync<MovieEpisode>();
    }

    #endregion

    #region SearchHistory CRUD

    public async Task<List<SearchHistory>> GetSearchHistoriesAsync()
    {
        return await _database.Table<SearchHistory>()
            .OrderByDescending(h => h.SearchedAt)
            .ToListAsync();
    }

    public async Task<SearchHistory?> GetSearchHistoryAsync(int id)
    {
        return await _database.Table<SearchHistory>().Where(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task AddSearchHistoryAsync(string keyword)
    {
        var trimmedKeyword = keyword.Trim();
        var existing = await _database.Table<SearchHistory>()
            .Where(h => h.Keyword == trimmedKeyword)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.SearchedAt = DateTime.Now;
            await _database.UpdateAsync(existing);
        }
        else
        {
            await _database.InsertAsync(new SearchHistory
            {
                Keyword = trimmedKeyword,
                SearchedAt = DateTime.Now
            });
        }
    }

    public async Task<int> DeleteSearchHistoryAsync(SearchHistory history)
    {
        return await _database.DeleteAsync(history);
    }

    public async Task<int> DeleteSearchHistoryByIdAsync(int id)
    {
        return await _database.DeleteAsync<SearchHistory>(id);
    }

    public async Task<int> ClearSearchHistoriesAsync()
    {
        return await _database.DeleteAllAsync<SearchHistory>();
    }

    #endregion

    #region PlayHistory CRUD

    public async Task<List<PlayHistory>> GetPlayHistoriesAsync()
    {
        return await _database.Table<PlayHistory>()
            .OrderByDescending(h => h.LastWatchedAt)
            .ToListAsync();
    }

    public async Task<PlayHistory?> GetPlayHistoryAsync(int id)
    {
        return await _database.Table<PlayHistory>().Where(h => h.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<PlayHistory>> GetPlayHistoriesByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return await _database.Table<PlayHistory>()
            .Where(h => h.LastWatchedAt >= start && h.LastWatchedAt < end)
            .OrderByDescending(h => h.LastWatchedAt)
            .ToListAsync();
    }

    public async Task<List<PlayHistory>> GetPlayHistoriesByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _database.Table<PlayHistory>()
            .Where(h => h.LastWatchedAt >= start && h.LastWatchedAt < end)
            .OrderByDescending(h => h.LastWatchedAt)
            .ToListAsync();
    }

    public async Task<List<PlayHistory>> GetPlayHistoriesByYearAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        return await _database.Table<PlayHistory>()
            .Where(h => h.LastWatchedAt >= start && h.LastWatchedAt < end)
            .OrderByDescending(h => h.LastWatchedAt)
            .ToListAsync();
    }

    public async Task AddOrUpdatePlayHistoryAsync(PlayHistory history)
    {
        var existing = await _database.Table<PlayHistory>()
            .Where(h => h.MovieId == history.MovieId && h.EpisodeId == history.EpisodeId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.MovieTitle = history.MovieTitle;
            existing.PosterUrl = history.PosterUrl;
            existing.EpisodeName = history.EpisodeName;
            existing.PlayUrl = history.PlayUrl;
            existing.SourceName = history.SourceName;
            existing.ProgressSeconds = history.ProgressSeconds;
            existing.DurationSeconds = history.DurationSeconds;
            existing.IsLocal = history.IsLocal;
            existing.LastWatchedAt = DateTime.Now;
            await _database.UpdateAsync(existing);
        }
        else
        {
            history.LastWatchedAt = DateTime.Now;
            await _database.InsertAsync(history);
        }
    }

    public async Task<int> DeletePlayHistoryAsync(PlayHistory history)
    {
        return await _database.DeleteAsync(history);
    }

    public async Task<int> DeletePlayHistoryByIdAsync(int id)
    {
        return await _database.DeleteAsync<PlayHistory>(id);
    }

    public async Task<int> ClearPlayHistoriesAsync()
    {
        return await _database.DeleteAllAsync<PlayHistory>();
    }

    #endregion

    #region DownloadRecord CRUD

    public async Task<List<DownloadRecord>> GetDownloadRecordsAsync()
    {
        return await _database.Table<DownloadRecord>()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<DownloadRecord?> GetDownloadRecordAsync(int id)
    {
        return await _database.Table<DownloadRecord>().Where(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<DownloadRecord>> GetDownloadRecordsByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return await _database.Table<DownloadRecord>()
            .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DownloadRecord>> GetDownloadRecordsByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _database.Table<DownloadRecord>()
            .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DownloadRecord>> GetDownloadRecordsByYearAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        return await _database.Table<DownloadRecord>()
            .Where(r => r.CreatedAt >= start && r.CreatedAt < end)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<DownloadRecord>> SearchDownloadRecordsAsync(string keyword)
    {
        var trimmed = keyword.Trim();
        return await _database.Table<DownloadRecord>()
            .Where(r => r.MovieTitle.Contains(trimmed) || r.EpisodeName.Contains(trimmed) || r.SourceName.Contains(trimmed))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> AddDownloadRecordAsync(DownloadRecord record)
    {
        return await _database.InsertAsync(record);
    }

    public async Task<int> InsertDownloadRecordAsync(DownloadRecord record)
    {
        return await _database.InsertAsync(record);
    }

    public async Task<int> UpdateDownloadRecordAsync(DownloadRecord record)
    {
        return await _database.UpdateAsync(record);
    }

    public async Task<int> DeleteDownloadRecordAsync(DownloadRecord record)
    {
        return await _database.DeleteAsync(record);
    }

    public async Task<int> DeleteDownloadRecordByIdAsync(int id)
    {
        return await _database.DeleteAsync<DownloadRecord>(id);
    }

    public async Task<int> ClearDownloadRecordsAsync()
    {
        return await _database.DeleteAllAsync<DownloadRecord>();
    }

    #endregion

    #region Global Operations

    public async Task CloseAsync()
    {
        await _database.CloseAsync();
    }

    public async Task ClearAllDataAsync()
    {
        await _database.DeleteAllAsync<MovieSource>();
        await _database.DeleteAllAsync<Movie>();
        await _database.DeleteAllAsync<MovieEpisode>();
        await _database.DeleteAllAsync<SearchHistory>();
        await _database.DeleteAllAsync<PlayHistory>();
        await _database.DeleteAllAsync<DownloadRecord>();
    }

    #endregion
}
