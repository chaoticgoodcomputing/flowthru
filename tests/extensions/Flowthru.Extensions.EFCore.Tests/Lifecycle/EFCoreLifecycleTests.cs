using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Lifecycle;

/// <summary>
/// End-to-end exercises for the catalog-attachable
/// <see cref="EFCoreLifecycleExtensions.EphemeralDatabase"/> resource.
/// SQLite-backed for speed and isolation; the same resource shape
/// applies to PostgreSQL / SQL Server.
/// </summary>
[TestFixture]
[Category("EFCore")]
[Category("Lifecycle")]
public class EFCoreLifecycleTests
{
  private string _dbPath = null!;
  private IDbContextFactory<TestDbContext> _factory = null!;

  [SetUp]
  public void SetUp()
  {
    _dbPath = Path.Combine(Path.GetTempPath(), $"flowthru-efcore-life-{Guid.NewGuid():N}.db");
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={_dbPath}")
      .Options;
    _factory = new TestSqliteFactory(options);
    // Note: lifecycle tests deliberately do NOT pre-create the db —
    // the EphemeralDatabase resource handles that on Acquire.
  }

  [TearDown]
  public void TearDown()
  {
    if (File.Exists(_dbPath))
    {
      try { File.Delete(_dbPath); }
      catch { /* best effort */ }
    }
  }

  [Test]
  public async Task EphemeralDatabase_AcquireCreatesAndReleaseDeletes()
  {
    var resource = _factory.EphemeralDatabase(_dbPath);

    Assert.That(File.Exists(_dbPath), Is.False, "Precondition: db file absent.");

    var result = await resource.Use(scope =>
      FlowIO.LiftAsync(_ =>
      {
        // Inside the body the database exists and the schema is created.
        Assert.That(File.Exists(_dbPath), Is.True,
          "Acquire should have created the database file.");
        return Task.FromResult(scope);
      }, source: "test-body")
    ).Run();

    Assert.That(result, Is.InstanceOf<EffResult<DbScope>.Success>());
    Assert.That(File.Exists(_dbPath), Is.False,
      "Successful runs always release; the db file should be gone.");
  }

  [Test]
  public async Task EphemeralDatabase_BodyFailure_StillReleases()
  {
    var resource = _factory.EphemeralDatabase(_dbPath);

    // Body fails — release should still drop the database, default behavior.
    await resource.Use<FlowUnit>(_ =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        "test-body",
        new InvalidOperationException("intentional failure")
      ))
    ).Run();

    Assert.That(File.Exists(_dbPath), Is.False,
      "Default release runs even on body failure — bracket guarantee.");
  }

  [Test]
  public async Task EphemeralDatabase_PreserveOnFailure_KeepsDatabase()
  {
    var resource = _factory.EphemeralDatabase(_dbPath, opt => opt.PreserveOnFailure = true);

    await resource.Use<FlowUnit>(_ =>
      FlowIO.Fail<FlowUnit>(new RuntimeError.External(
        "test-body",
        new InvalidOperationException("intentional failure")
      ))
    ).Run();

    Assert.That(File.Exists(_dbPath), Is.True,
      "PreserveOnFailure: when the body errors, the db should be kept for inspection.");
  }

  [Test]
  public async Task EphemeralDatabase_AcquireIsIdempotent()
  {
    // Pre-seed a stale database (simulating a leftover from a
    // PreserveOnFailure run). Acquire should drop it and create fresh.
    using (var ctx = await _factory.CreateDbContextAsync())
    {
      await ctx.Database.EnsureCreatedAsync();
      ctx.Items.Add(new TestEntity { Id = 99, Name = "stale", Value = 9.9 });
      await ctx.SaveChangesAsync();
    }

    var resource = _factory.EphemeralDatabase(_dbPath);
    await resource.Use(_ =>
      FlowIO.LiftAsync(async _ =>
      {
        // After acquire, the stale row should be gone.
        await using var ctx = await _factory.CreateDbContextAsync();
        var rows = await ctx.Items.ToListAsync();
        Assert.That(rows, Is.Empty,
          "Acquire should have wiped the stale row by dropping + recreating the db.");
        return FlowUnit.Default;
      }, source: "test-body")
    ).Run();
  }

  [Test]
  public void EphemeralDatabase_ScopeIdentity_IsReferenceBasedOnFactory()
  {
    var scope1 = DbScope.Inferred(_factory);
    var scope2 = DbScope.Inferred(_factory);
    var (otherFactory, otherDbPath) = TestDbContextFactoryBuilder.Build();
    try
    {
      var scope3 = DbScope.Inferred(otherFactory);

      Assert.That(scope1, Is.EqualTo(scope2),
        "Two scopes built off the same factory reference should be equal.");
      Assert.That(scope1, Is.Not.EqualTo(scope3),
        "Scopes built off different factory references should not be equal.");
    }
    finally
    {
      if (File.Exists(otherDbPath)) File.Delete(otherDbPath);
    }
  }
}
