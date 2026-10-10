using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.UnitTests.TestDoubles;

/// <summary>
/// A real, empty database for query tests: the production model on in-memory SQLite, so that
/// the LINQ really is translated to SQL. It lives as long as this object, which owns the one
/// connection that keeps the in-memory database alive.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<SupportDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection.Open();

        _options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    /// <summary>A new context on the same database. Dispose it when done.</summary>
    public SupportDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
