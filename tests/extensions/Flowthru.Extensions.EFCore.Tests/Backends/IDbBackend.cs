using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Backends;

/// <summary>
/// Abstraction over a database backend used by EFCore conformance subclasses.
/// </summary>
/// <remarks>
/// <para>
/// Each concrete backend brings up an isolated database and returns
/// <see cref="DbContextOptions{TestDbContext}"/> wired to it. Conformance subclasses
/// declare <c>[TestFixture(typeof(SqliteInMemoryBackend))]</c> +
/// <c>[TestFixture(typeof(PostgresContainerBackend), Category = "Integration")]</c>
/// (or whichever subset is relevant) and NUnit instantiates the fixture once per
/// declared backend, exercising the same conformance contract against each.
/// </para>
/// <para>
/// The pattern was motivated by commit
/// <c>0cb460d9ef05ce13aadb0726fa10c8de4d850b0a</c> — a Postgres-only false-positive
/// in <c>EFCoreShapeValidator</c> that in-memory SQLite tests could not surface.
/// Per-backend conformance closes the gap: any provider-specific divergence
/// surfaces as a single backend's tests failing rather than a production incident.
/// </para>
/// </remarks>
public interface IDbBackend : IAsyncDisposable
{
  /// <summary>
  /// Brings up the backend (in-memory connection, container, etc.) and returns
  /// EF Core options bound to it. The schema is <em>not</em> created here; callers
  /// should call <c>EnsureCreatedAsync</c> on the returned context options as part
  /// of their per-fixture setup.
  /// </summary>
  Task<DbContextOptions<TestDbContext>> StartAsync();

  /// <summary>
  /// Human-readable name for diagnostics and test reports (e.g. "SQLite (in-memory)").
  /// </summary>
  string DisplayName { get; }
}
