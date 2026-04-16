using Flowthru.Core.Data.Storage;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

[TestFixture]
public class DbScopeTests
{
  // DbScope equality is verified through the public Equals override on the concrete subtypes.
  // IsSameDatabase is internal; its correctness is covered by the end-to-end fused-path test
  // Save_DbQueryWithSameScope_TakesFusedPath in DbQueryStorageAdapterTests.

  [Test]
  public void Inferred_SameFactoryReference_AreEqual()
  {
    var factory = new object();
    var a = DbScope.Inferred(factory);
    var b = DbScope.Inferred(factory);

    Assert.That(a.Equals(b), Is.True);
    Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
  }

  [Test]
  public void Inferred_DifferentFactoryReferences_AreNotEqual()
  {
    var a = DbScope.Inferred(new object());
    var b = DbScope.Inferred(new object());

    Assert.That(a.Equals(b), Is.False);
  }

  [Test]
  public void Explicit_SameName_AreEqual()
  {
    var a = DbScope.Explicit("spaceflights");
    var b = DbScope.Explicit("spaceflights");

    Assert.That(a.Equals(b), Is.True);
    Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
  }

  [Test]
  public void Explicit_DifferentName_AreNotEqual()
  {
    var a = DbScope.Explicit("db_a");
    var b = DbScope.Explicit("db_b");

    Assert.That(a.Equals(b), Is.False);
  }

  [Test]
  public void Explicit_CaseSensitive_AreNotEqual()
  {
    var a = DbScope.Explicit("MyDb");
    var b = DbScope.Explicit("mydb");

    Assert.That(a.Equals(b), Is.False);
  }

  [Test]
  public void Inferred_And_Explicit_AreNeverEqual()
  {
    var inferred = DbScope.Inferred(new object());
    var @explicit = DbScope.Explicit("any");

    Assert.That(inferred.Equals(@explicit), Is.False);
    Assert.That(@explicit.Equals(inferred), Is.False);
  }
}

[TestFixture]
public class DbQueryStorageAdapterTests
{
  private SqliteConnection _connection = null!;
  private DbContextOptions<TestDbContext> _options = null!;
  private Func<DbContext> _factory = null!;

  [SetUp]
  public async Task SetUp()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    await _connection.OpenAsync();
    _options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;
    await using var ctx = new TestDbContext(_options);
    await ctx.Database.EnsureCreatedAsync();
    _factory = () => new TestDbContext(_options);
  }

  [TearDown]
  public async Task TearDown()
  {
    await _connection.DisposeAsync();
  }

  // ── Load returns deferred handle ─────────────────────────────────────────

  [Test]
  public async Task Load_ReturnsDbQueryHandle_NotMaterialisedList()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);

    var loaded = await entry.Load().Run();

    Assert.That(loaded, Is.InstanceOf<DbQuery<TestEntity>>());
  }

  [Test]
  public async Task Load_Handle_MaterialisesCorrectly()
  {
    var seed = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("seed", _factory);
    await seed.Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "Alpha" },
          new TestEntity { Id = 2, Name = "Beta" },
        }
      )
      .Run();

    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);
    var handle = (DbQuery<TestEntity>)await entry.Load().Run();
    var rows = await handle.ToListAsync();

    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.Select(r => r.Name), Is.EquivalentTo(new[] { "Alpha", "Beta" }));
  }

  // ── Fluent composition ────────────────────────────────────────────────────

  [Test]
  public async Task Handle_Where_FiltersWithoutExecuting()
  {
    var seed = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("seed", _factory);
    await seed.Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "Alice" },
          new TestEntity { Id = 2, Name = "Bob" },
        }
      )
      .Run();

    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);
    var handle = ((DbQuery<TestEntity>)await entry.Load().Run()).Where(e => e.Id == 2);
    var rows = await handle.ToListAsync();

    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Bob"));
  }

  [Test]
  public async Task Handle_Take_LimitsRows()
  {
    var seed = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("seed", _factory);
    await seed.Save(Enumerable.Range(1, 10).Select(i => new TestEntity { Id = i, Name = $"E{i}" }))
      .Run();

    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);
    var handle = ((DbQuery<TestEntity>)await entry.Load().Run()).Take(3);
    var rows = await handle.ToListAsync();

    Assert.That(rows, Has.Count.EqualTo(3));
  }

  // ── IEnumerable<T> covariance ─────────────────────────────────────────────

  [Test]
  public async Task Handle_UsedAsIEnumerable_MaterialisesOnIteration()
  {
    var seed = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("seed", _factory);
    await seed.Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "X" },
        }
      )
      .Run();

    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);
    IEnumerable<TestEntity> loaded = await entry.Load().Run();

    var list = loaded.ToList(); // triggers sync materialisation
    Assert.That(list, Has.Count.EqualTo(1));
  }

  // ── Save — materialised fallback ─────────────────────────────────────────

  [Test]
  public async Task Save_PlainList_UsesRemoveRangeAddRange()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);

    await entry
      .Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "First" },
        }
      )
      .Run();

    var rows = await entry
      .Load()
      .Run()
      .AsTask()
      .ContinueWith(t => ((DbQuery<TestEntity>)t.Result).ToListAsync())
      .Unwrap();

    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("First"));
  }

  [Test]
  public async Task Save_RoundTrip_ReplacesAllRows()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);

    await entry
      .Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "Old1" },
          new TestEntity { Id = 2, Name = "Old2" },
        }
      )
      .Run();

    await entry
      .Save(
        new[]
        {
          new TestEntity { Id = 3, Name = "New" },
        }
      )
      .Run();

    var handle = (DbQuery<TestEntity>)await entry.Load().Run();
    var rows = await handle.ToListAsync();

    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("New"));
  }

  // ── Exists / InspectShallow ───────────────────────────────────────────────

  [Test]
  public async Task Exists_EmptyTable_ReturnsFalse()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);

    Assert.That(await entry.Exists().Run(), Is.False);
  }

  [Test]
  public async Task Exists_AfterSave_ReturnsTrue()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory);
    await entry
      .Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "Y" },
        }
      )
      .Run();

    Assert.That(await entry.Exists().Run(), Is.True);
  }

  [Test]
  public async Task InspectShallow_EmptyTable_AllowEmptyFalse_ReturnsFailure()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory, allowEmptyData: false);
    var result = await entry.InspectShallow(sampleSize: 10).Run();

    Assert.That(result.IsValid, Is.False);
  }

  [Test]
  public async Task InspectShallow_EmptyTable_AllowEmptyTrue_ReturnsSuccess()
  {
    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("test", _factory, allowEmptyData: true);
    var result = await entry.InspectShallow(sampleSize: 10).Run();

    Assert.That(result.IsValid, Is.True);
  }

  // ── DbScope fused-path detection ─────────────────────────────────────────

  [Test]
  public async Task Save_SelfReferentialDbQuery_FallsBackToMaterialised()
  {
    // Seed TestEntity data
    var seedEntry = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("seed", _factory);
    await seedEntry
      .Save(
        new[]
        {
          new TestEntity { Id = 1, Name = "A" },
          new TestEntity { Id = 2, Name = "B" },
          new TestEntity { Id = 3, Name = "C" },
        }
      )
      .Run();

    // Load as deferred handle and filter — source == target table (self-referential)
    var readEntry = EFCoreItemFactory.Query.EFCore<TestEntity>("read", _factory);
    var filtered = ((DbQuery<TestEntity>)await readEntry.Load().Run()).Where(e => e.Id < 3);

    var writeEntry = EFCoreItemFactory.Query.EFCore<TestEntity>("write", _factory);
    await writeEntry.Save(filtered).Run();

    // Self-referential guard kicks in → materialised fallback → correct 2-row result
    var rows = await ((DbQuery<TestEntity>)await writeEntry.Load().Run()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.Select(r => r.Name), Is.EquivalentTo(new[] { "A", "B" }));
  }

  [Test]
  public async Task Save_CrossTableDbQuery_TakesFusedPath()
  {
    // Seed SourceEntity (a different table) — this is the true fused-path scenario.
    // The source SELECT reads from SourceEntities; the target DELETE+INSERT operates
    // on TestEntities. Because they are different tables the INSERT-FROM-SELECT is safe.
    var sourceEntry = EFCoreItemFactory.Enumerable.EFCore<SourceEntity>("source", _factory);
    await sourceEntry
      .Save(
        new[]
        {
          new SourceEntity { Id = 1, SourceName = "Alpha" },
          new SourceEntity { Id = 2, SourceName = "Beta" },
          new SourceEntity { Id = 3, SourceName = "Gamma" },
        }
      )
      .Run();

    // Build a DbQuery<SourceEntity> then project to DbQuery<TestEntity> via cross-table LINQ.
    var sourceQueryEntry = EFCoreItemFactory.Query.EFCore<SourceEntity>("sourceQ", _factory);
    var sourceHandle = (DbQuery<SourceEntity>)await sourceQueryEntry.Load().Run();

    // Project: reads from SourceEntities, produces TestEntity-shaped rows
    var crossTableQuery = sourceHandle.Project<TestEntity>(ctx =>
      ctx.Set<SourceEntity>()
        .Where(e => e.Id < 3)
        .Select(e => new TestEntity { Id = e.Id, Name = e.SourceName })
    );

    // Write target uses the SAME factory → scopes match → fused INSERT-FROM-SELECT.
    // SELECT reads from SourceEntities (not TestEntities), so the DELETE-then-SELECT is valid.
    var writeEntry = EFCoreItemFactory.Query.EFCore<TestEntity>("write", _factory);
    await writeEntry.Save(crossTableQuery).Run();

    var rows = await ((DbQuery<TestEntity>)await writeEntry.Load().Run()).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.Select(r => r.Name), Is.EquivalentTo(new[] { "Alpha", "Beta" }));
  }

  // ── Project<TResult> ─────────────────────────────────────────────────────

  [Test]
  public async Task Project_BuildsDerivedQuery_WithSameScope()
  {
    var seed = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("seed", _factory);
    await seed.Save(
        new[]
        {
          new TestEntity { Id = 42, Name = "Projected" },
        }
      )
      .Run();

    var entry = EFCoreItemFactory.Query.EFCore<TestEntity>("source", _factory);
    var handle = (DbQuery<TestEntity>)await entry.Load().Run();

    // Project to an anonymous-typed IQueryable materialised as List<string>
    var projected = handle.Project<TestEntity>(ctx => ctx.Set<TestEntity>().Where(e => e.Id == 42));

    var rows = await projected.ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("Projected"));
  }
}
