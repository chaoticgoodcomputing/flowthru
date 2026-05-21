using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Tests.Kits.Prelude;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Backends;

/// <summary>
/// Backend for <see cref="EFCoreResourceLaws{TBackend}"/> targeting a
/// per-call temporary SQLite database file. Cheap to construct, no
/// external service dependencies — runs on every PR via the default
/// <c>nx run affected -t test</c> flow.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="CreateResource"/> call allocates a unique
/// <c>.db</c> path under the system temp directory and wraps
/// <see cref="EFCoreLifecycleExtensions.EphemeralDatabase"/> against
/// it. The acquire path runs <c>EnsureCreatedAsync</c>; the release
/// path runs <c>EnsureDeletedAsync</c>.
/// </para>
/// <para>
/// <strong>Re-entrancy.</strong> Each call generates an independent
/// GUID-keyed path and its own <see cref="IDbContextFactory{TContext}"/>,
/// so concurrent calls produce fully disjoint state. The
/// <c>_pendingSeed</c> / <c>_lastPath</c> fields serve sequential
/// EphemeralResourceLaws scenarios only (SeedLeftoverState/ResourceExists);
/// the re-entrancy law touches neither.
/// </para>
/// </remarks>
public sealed class SqliteFileBackend : IEphemeralResourceBackend<BackendScope<DbScope>>
{
  private string? _pendingSeed;
  private string? _lastPath;

  public string ExternalStateIdentifier(BackendScope<DbScope> scope) => scope.ExternalStateId;

  public FlowResource<BackendScope<DbScope>> CreateResource(bool preserveOnFailure)
  {
    // Honour a pre-seeded leftover path so AcquireWipesLeftoverStateLaw
    // exercises the wipe path; otherwise allocate fresh.
    var path = Interlocked.Exchange(ref _pendingSeed, null) ?? GenerateFreshPath();
    Interlocked.Exchange(ref _lastPath, path);

    var factory = BuildFactory(path);
    var inner = factory.EphemeralDatabase(path, opts => opts.PreserveOnFailure = preserveOnFailure);

    return FlowResource.Make<BackendScope<DbScope>>(
      acquire: inner.Acquire.Map(scope => new BackendScope<DbScope>(scope, path)),
      release: (wrapped, err) => inner.Release(wrapped.Inner, err)
    );
  }

  public Task<bool> ResourceExists()
  {
    var path = _lastPath;
    return Task.FromResult(path is not null && File.Exists(path));
  }

  public async Task SeedLeftoverState()
  {
    // Build a fresh file with the schema present, then queue its path
    // as the next CreateResource()'s target. The next acquire's
    // EnsureDeletedAsync / EnsureCreatedAsync pair wipes it.
    var path = GenerateFreshPath();
    var factory = BuildFactory(path);
    await using (var ctx = await factory.CreateDbContextAsync())
    {
      await ctx.Database.EnsureCreatedAsync();
    }
    Interlocked.Exchange(ref _lastPath, path);
    Interlocked.Exchange(ref _pendingSeed, path);
  }

  public Task<IPeerStateProbe?> CreatePeerState()
  {
    // A sibling SQLite file independent of any resource the backend
    // produces. The probe asserts release didn't accidentally drop it.
    var peerPath = GenerateFreshPath(suffix: "peer");
    var peerFactory = BuildFactory(peerPath);
    using (var ctx = peerFactory.CreateDbContext())
    {
      ctx.Database.EnsureCreated();
    }
    return Task.FromResult<IPeerStateProbe?>(new SqlitePeerProbe(peerPath));
  }

  public Task Cleanup()
  {
    // Drop the most recent (and any pending-seed) file. Failed paths
    // are silently ignored — best-effort teardown.
    foreach (var path in new[] { _lastPath, _pendingSeed })
    {
      if (path is not null && File.Exists(path))
      {
        try { File.Delete(path); }
        catch { /* best effort */ }
      }
    }
    _lastPath = null;
    _pendingSeed = null;
    return Task.CompletedTask;
  }

  private static string GenerateFreshPath(string suffix = "main") =>
    Path.Combine(Path.GetTempPath(), $"flowthru-laws-sqlite-{suffix}-{Guid.NewGuid():N}.db");

  private static IDbContextFactory<TestDbContext> BuildFactory(string path)
  {
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={path}")
      .Options;
    return new TestSqliteFactory(options);
  }

  private sealed class SqlitePeerProbe(string path) : IPeerStateProbe
  {
    public Task<bool> StillExists() => Task.FromResult(File.Exists(path));

    public ValueTask DisposeAsync()
    {
      if (File.Exists(path))
      {
        try { File.Delete(path); }
        catch { /* best effort */ }
      }
      return ValueTask.CompletedTask;
    }
  }
}
