using Flowthru.Core.Effects;
using Flowthru.Extensions.EFCore.Lifecycle;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Lifecycle;

/// <summary>
/// Verifies the <see cref="EFCoreResources.EphemeralDatabase{TContext}"/>
/// lifecycle: acquire creates a fresh database, release drops it, and the
/// <c>PreserveOnFailure</c> option keeps the database when the flow body
/// throws.
/// </summary>
/// <remarks>
/// Uses a real SQLite file (not <c>:memory:</c>) so the tests can assert on
/// filesystem state — the whole point of the ephemeral lifecycle is that no
/// stale file is left behind after a normal run.
/// </remarks>
[TestFixture]
[Category("EFCore")]
public class EphemeralDatabaseTests
{
  private string _tempDir = null!;
  private string _dbPath = null!;
  private IDbContextFactory<TestDbContext> _factory = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-eph-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
    _dbPath = Path.Combine(_tempDir, "ephemeral.db");

    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={_dbPath}")
      .Options;
    _factory = new TestDbContextFactory(options);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
    {
      try
      {
        Directory.Delete(_tempDir, recursive: true);
      }
      catch
      {
        // Best-effort cleanup; SQLite may still hold a handle on Windows.
      }
    }
  }

  [Test]
  public async Task Acquire_CreatesDatabaseFile()
  {
    var resource = EFCoreResources.EphemeralDatabase(_factory, _dbPath);

    Assert.That(File.Exists(_dbPath), Is.False, "Pre-condition: file should not exist before acquire.");

    await ((IFlowResource)resource).AcquireUntyped().Run();

    Assert.That(File.Exists(_dbPath), Is.True, "Acquire should have created the database file.");

    // Release the resource so the file gets cleaned up.
    await ((IFlowResource)resource).ReleaseUntyped(scope: null, bodyException: null).Run();
  }

  [Test]
  public async Task Use_CreatesAndDropsDatabaseAroundBody()
  {
    var resource = EFCoreResources.EphemeralDatabase(_factory, _dbPath);
    var observedDuringBody = false;

    await resource
      .Use(_ =>
        FlowIO.Lift(() =>
        {
          observedDuringBody = File.Exists(_dbPath);
          return FlowUnit.Default;
        })
      )
      .Run();

    Assert.That(observedDuringBody, Is.True, "Body should observe the database file present.");
    Assert.That(File.Exists(_dbPath), Is.False, "Release should have dropped the file.");
  }

  [Test]
  public void Use_BodyThrows_DefaultBehavior_DropsDatabase()
  {
    var resource = EFCoreResources.EphemeralDatabase(_factory, _dbPath);
    var thrown = Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await resource
        .Use<FlowUnit>(_ => FlowIO.Fail<FlowUnit>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.That(thrown!.Message, Is.EqualTo("body-failed"));
    Assert.That(File.Exists(_dbPath), Is.False, "Default release drops the database even on body failure.");
  }

  [Test]
  public void Use_BodyThrows_PreserveOnFailure_KeepsDatabase()
  {
    var resource = EFCoreResources.EphemeralDatabase(
      _factory,
      _dbPath,
      o => o.PreserveOnFailure = true
    );

    Assert.ThrowsAsync<InvalidOperationException>(async () =>
      await resource
        .Use<FlowUnit>(_ => FlowIO.Fail<FlowUnit>(new InvalidOperationException("body-failed")))
        .Run()
    );

    Assert.That(File.Exists(_dbPath), Is.True, "PreserveOnFailure should keep the database for inspection.");
  }

  [Test]
  public async Task Acquire_IsIdempotent_ResetsExistingDatabase()
  {
    // Simulate a leftover staging database from a prior preserve-on-failure run.
    await using (var ctx = await _factory.CreateDbContextAsync())
    {
      await ctx.Database.EnsureCreatedAsync();
      ctx.TestEntities.Add(new TestEntity { Id = 99, Name = "leftover" });
      await ctx.SaveChangesAsync();
    }

    Assert.That(File.Exists(_dbPath), Is.True, "Pre-condition: leftover file present.");

    var resource = EFCoreResources.EphemeralDatabase(_factory, _dbPath);
    int rowCountAfterAcquire;

    try
    {
      await ((IFlowResource)resource).AcquireUntyped().Run();
      await using var ctx = await _factory.CreateDbContextAsync();
      rowCountAfterAcquire = await ctx.TestEntities.CountAsync();
    }
    finally
    {
      await ((IFlowResource)resource).ReleaseUntyped(scope: null, bodyException: null).Run();
    }

    Assert.That(
      rowCountAfterAcquire,
      Is.EqualTo(0),
      "Acquire should have wiped the leftover row by recreating the schema."
    );
  }

  [Test]
  public async Task Release_ScopeIsInferredOnFactory()
  {
    var resource = EFCoreResources.EphemeralDatabase(_factory, _dbPath);
    var scope = await resource.Acquire.Run();

    try
    {
      Assert.That(scope, Is.Not.Null);
    }
    finally
    {
      await resource.Release(scope, null).Run();
    }
  }
}
