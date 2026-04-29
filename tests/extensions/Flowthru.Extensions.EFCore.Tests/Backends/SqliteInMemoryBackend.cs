using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Backends;

/// <summary>
/// In-memory SQLite backend. Fast (no Docker), suitable for local development and
/// the default tier of CI runs. Cannot detect provider-specific bugs that only
/// manifest against PostgreSQL or other production-class databases — those are
/// covered by <see cref="PostgresContainerBackend"/>.
/// </summary>
public sealed class SqliteInMemoryBackend : IDbBackend
{
  private SqliteConnection? _connection;

  public string DisplayName => "SQLite (in-memory)";

  public async Task<DbContextOptions<TestDbContext>> StartAsync()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    return new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;
  }

  public async ValueTask DisposeAsync()
  {
    if (_connection is not null)
    {
      await _connection.DisposeAsync();
      _connection = null;
    }
  }
}
