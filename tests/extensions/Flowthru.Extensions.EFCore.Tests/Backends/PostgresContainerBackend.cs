using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Flowthru.Extensions.EFCore.Tests.Backends;

/// <summary>
/// Real-PostgreSQL backend backed by a Testcontainers-managed Docker container.
/// </summary>
/// <remarks>
/// <para>
/// Cold-start latency is roughly 5–10 seconds; <c>[OneTimeSetUp]</c> on the
/// conformance fixture amortizes that across every test method in the fixture.
/// Conformance subclasses tag this backend with <c>Category = "Integration"</c>
/// on the <c>[TestFixture(typeof(PostgresContainerBackend), Category = "Integration")]</c>
/// declaration so the fast tier (<c>nx run affected -t test</c>) skips it; the
/// integration tier runs it explicitly via <c>nx test:integration</c>.
/// </para>
/// <para>
/// Each backend instance starts a fresh container (no <c>WithReuse</c>) so test
/// isolation matches the SQLite in-memory shape: every fixture instance gets a
/// brand-new database. Fresh containers cost ~5s each but keep failure modes
/// predictable. If startup time becomes painful, switch to <c>WithReuse(true)</c>
/// and clean schemas between tests instead.
/// </para>
/// </remarks>
public sealed class PostgresContainerBackend : IDbBackend
{
  private PostgreSqlContainer? _container;

  public string DisplayName => "PostgreSQL (Testcontainers)";

  public async Task<DbContextOptions<TestDbContext>> StartAsync()
  {
    _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    await _container.StartAsync();

    return new DbContextOptionsBuilder<TestDbContext>()
      .UseNpgsql(_container.GetConnectionString())
      .Options;
  }

  public async ValueTask DisposeAsync()
  {
    if (_container is not null)
    {
      await _container.DisposeAsync();
      _container = null;
    }
  }
}
