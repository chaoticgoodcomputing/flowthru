using Flowthru.Core.Effects;
using Flowthru.Extensions.EFCore.Data;
using Flowthru.Extensions.EFCore.Lifecycle;
using Flowthru.Tests.Kits.Effects;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Lifecycle;

/// <summary>
/// SQLite implementation of <see cref="IEphemeralResourceBackend{DbScope}"/>
/// for the kit's lifecycle conformance suite. Uses a real on-disk file under
/// <c>Path.GetTempPath()</c> so resource existence can be observed by
/// checking file presence.
/// </summary>
public sealed class SqliteEphemeralDatabaseBackend : IEphemeralResourceBackend<DbScope>
{
  private readonly string _tempDir;
  private readonly string _dbPath;
  private readonly IDbContextFactory<TestDbContext> _factory;

  public SqliteEphemeralDatabaseBackend()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-kit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
    _dbPath = Path.Combine(_tempDir, "ephemeral.db");

    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={_dbPath}")
      .Options;
    _factory = new TestDbContextFactory(options);
  }

  public FlowResource<DbScope> CreateResource(bool preserveOnFailure) =>
    EFCoreResources.EphemeralDatabase(
      _factory,
      _dbPath,
      configure: o => o.PreserveOnFailure = preserveOnFailure
    );

  public Task<bool> ResourceExists() => Task.FromResult(File.Exists(_dbPath));

  public async Task SeedLeftoverState()
  {
    await using var ctx = await _factory.CreateDbContextAsync();
    await ctx.Database.EnsureCreatedAsync();
    ctx.TestEntities.Add(new TestEntity { Id = 99, Name = "leftover" });
    await ctx.SaveChangesAsync();
  }

  public async Task<IPeerStateProbe?> CreatePeerState()
  {
    var peerPath = Path.Combine(_tempDir, "peer.db");
    var peerOptions = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={peerPath}")
      .Options;

    await using (var ctx = new TestDbContext(peerOptions))
    {
      await ctx.Database.EnsureCreatedAsync();
    }

    return new PeerProbe(peerPath);
  }

  public Task Cleanup()
  {
    if (Directory.Exists(_tempDir))
    {
      try
      {
        Directory.Delete(_tempDir, recursive: true);
      }
      catch
      {
        // Best-effort. SQLite may still hold a handle on Windows; the OS
        // reclaims the temp dir eventually.
      }
    }
    return Task.CompletedTask;
  }

  private sealed class PeerProbe : IPeerStateProbe
  {
    private readonly string _peerPath;

    public PeerProbe(string peerPath) => _peerPath = peerPath;

    public Task<bool> StillExists() => Task.FromResult(File.Exists(_peerPath));

    public ValueTask DisposeAsync()
    {
      if (File.Exists(_peerPath))
      {
        try
        {
          File.Delete(_peerPath);
        }
        catch
        {
          // Best-effort.
        }
      }
      return ValueTask.CompletedTask;
    }
  }
}
