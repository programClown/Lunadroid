using Lunadroid.Core.Models;
using SQLite;

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
        await _database.CreateTableAsync<ApiSource>();
        await _database.CreateTableAsync<MediaDownload>();
        await _database.CreateTableAsync<PlayHistory>();
    }

    #endregion

    #region ApiSource CRUD

    public async Task<List<ApiSource>> GetApiSourcesAsync()
    {
        return await _database.Table<ApiSource>().ToListAsync();
    }

    public async Task<List<ApiSource>> GetEnabledApiSourcesAsync()
    {
        return await _database.Table<ApiSource>().Where(s => s.IsEnabled).ToListAsync();
    }

    public async Task<ApiSource?> GetApiSourceAsync(int id)
    {
        return await _database.Table<ApiSource>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<ApiSource?> GetApiSourceByIdAsync(int id)
    {
        return await _database.Table<ApiSource>().Where(s => s.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> AddApiSourceAsync(ApiSource source)
    {
        return await _database.InsertAsync(source);
    }

    public async Task<int> UpdateApiSourceAsync(ApiSource source)
    {
        return await _database.UpdateAsync(source);
    }

    public async Task<int> DeleteApiSourceAsync(ApiSource source)
    {
        return await _database.DeleteAsync(source);
    }

    public async Task<int> DeleteApiSourceByIdAsync(int id)
    {
        return await _database.DeleteAsync<ApiSource>(id);
    }

    public async Task<int> ClearApiSourcesAsync()
    {
        return await _database.DeleteAllAsync<ApiSource>();
    }

    #endregion

    #region PlayHistory CRUD

    public async Task<List<PlayHistory>> GetPlayHistoriesAsync()
    {
        return await _database.Table<PlayHistory>()
            .OrderByDescending(h => h.UpdateTime)
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
            .Where(h => h.UpdateTime >= start && h.UpdateTime < end)
            .OrderByDescending(h => h.UpdateTime)
            .ToListAsync();
    }

    public async Task<List<PlayHistory>> GetPlayHistoriesByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _database.Table<PlayHistory>()
            .Where(h => h.UpdateTime >= start && h.UpdateTime < end)
            .OrderByDescending(h => h.UpdateTime)
            .ToListAsync();
    }

    public async Task<List<PlayHistory>> GetPlayHistoriesByYearAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        return await _database.Table<PlayHistory>()
            .Where(h => h.UpdateTime >= start && h.UpdateTime < end)
            .OrderByDescending(h => h.UpdateTime)
            .ToListAsync();
    }

    public async Task AddOrUpdatePlayHistoryAsync(PlayHistory history)
    {
        var existing = await _database.Table<PlayHistory>()
            .Where(h => h.VodId == history.VodId && h.Name == history.Name)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.PlaybackPosition = history.PlaybackPosition;
            existing.UpdateTime = DateTime.Now;
            await _database.UpdateAsync(existing);
        }
        else
        {
            history.UpdateTime = DateTime.Now;
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

    #region MediaDownload CRUD

    public async Task<List<MediaDownload>> GetDownloadRecordsAsync()
    {
        return await _database.Table<MediaDownload>()
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<MediaDownload?> GetDownloadRecordAsync(int id)
    {
        return await _database.Table<MediaDownload>().Where(r => r.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<MediaDownload>> GetDownloadRecordsByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        return await _database.Table<MediaDownload>()
            .Where(r => r.CreateTime >= start && r.CreateTime < end)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MediaDownload>> GetDownloadRecordsByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        return await _database.Table<MediaDownload>()
            .Where(r => r.CreateTime >= start && r.CreateTime < end)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MediaDownload>> GetDownloadRecordsByYearAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = start.AddYears(1);
        return await _database.Table<MediaDownload>()
            .Where(r => r.CreateTime >= start && r.CreateTime < end)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<List<MediaDownload>> SearchDownloadRecordsAsync(string keyword)
    {
        var trimmed = keyword.Trim();
        return await _database.Table<MediaDownload>()
            .Where(r => r.Name.Contains(trimmed) || r.Source.Contains(trimmed) ||
                        r.Url.Contains(trimmed))
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<int> AddDownloadRecordAsync(MediaDownload record)
    {
        return await _database.InsertAsync(record);
    }

    public async Task<int> InsertDownloadRecordAsync(MediaDownload record)
    {
        return await _database.InsertAsync(record);
    }

    public async Task<int> UpdateDownloadRecordAsync(MediaDownload record)
    {
        return await _database.UpdateAsync(record);
    }

    public async Task<int> DeleteDownloadRecordAsync(MediaDownload record)
    {
        return await _database.DeleteAsync(record);
    }

    public async Task<int> DeleteDownloadRecordByIdAsync(int id)
    {
        return await _database.DeleteAsync<MediaDownload>(id);
    }

    public async Task<int> ClearDownloadRecordsAsync()
    {
        return await _database.DeleteAllAsync<MediaDownload>();
    }

    #endregion

    #region Global Operations

    public async Task CloseAsync()
    {
        await _database.CloseAsync();
    }

    public async Task ClearAllDataAsync()
    {
        await _database.DeleteAllAsync<ApiSource>();
        await _database.DeleteAllAsync<MediaDownload>();
        await _database.DeleteAllAsync<PlayHistory>();
    }

    #endregion
}