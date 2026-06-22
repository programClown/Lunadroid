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
        catch
        {
        }

        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
        }
    }
}