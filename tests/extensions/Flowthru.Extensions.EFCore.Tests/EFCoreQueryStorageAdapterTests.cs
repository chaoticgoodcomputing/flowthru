using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Exercises for <see cref="EFCoreQueryStorageAdapter{T}"/> — deferred
/// load semantics, LINQ composition over the handle, the fused
/// <c>INSERT-FROM-SELECT</c> save path, and the materialised fallback
/// when scopes differ.
/// </summary>
[TestFixture]
[Category("EFCore")]
[Category("Query")]
public class EFCoreQueryStorageAdapterTests
{
  private IDbContextFactory<TestDbContext> _factory = null!;
  private string _dbPath = null!;

  [SetUp]
  public void SetUp()
  {
    (_factory, _dbPath) = TestDbContextFactoryBuilder.Build();
  }

  [TearDown]
  public void TearDown()
  {
    if (File.Exists(_dbPath))
    {
      try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
  }

  private async Task SeedAsync(params TestEntity[] entities)
  {
    using var ctx = _factory.CreateDbContext();
    ctx.Items.AddRange(entities);
    await ctx.SaveChangesAsync();
  }

  // ── Deferred load semantics ──────────────────────────────────────────

  [Test]
  public async Task Load_ReturnsDbQueryHandle_NotMaterialised()
  {
    await SeedAsync(
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 }
    );

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var load = await item.Load().Run();
    var value = ((EffResult<IEnumerable<TestEntity>>.Success)load).Value;

    Assert.That(value, Is.InstanceOf<DbQuery<TestEntity>>(),
      "Load() must return the deferred handle, not a materialised list.");
  }

  [Test]
  public async Task Load_ThenToListAsync_FetchesRows()
  {
    await SeedAsync(
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 }
    );

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var load = await item.Load().Run();
    var query = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)load).Value;

    var rows = await query.ToListAsync();
    Assert.That(rows.OrderBy(r => r.Id).Select(r => r.Name), Is.EqualTo(new[] { "a", "b" }));
  }

  [Test]
  public async Task DbQuery_LinqComposition_FiltersRows()
  {
    await SeedAsync(
      new TestEntity { Id = 1, Name = "low",  Value = 1.0 },
      new TestEntity { Id = 2, Name = "med",  Value = 2.5 },
      new TestEntity { Id = 3, Name = "high", Value = 5.0 }
    );

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var load = await item.Load().Run();
    var query = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)load).Value;

    var rows = await query.Where(e => e.Value >= 2.0).OrderBy(e => e.Id).ToListAsync();
    Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[] { "med", "high" }));
  }

  [Test]
  public async Task DbQuery_TakeAndSkip_PageRows()
  {
    await SeedAsync(
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 },
      new TestEntity { Id = 3, Name = "c", Value = 3.0 },
      new TestEntity { Id = 4, Name = "d", Value = 4.0 }
    );

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var load = await item.Load().Run();
    var query = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)load).Value;

    var page = await query.OrderBy(e => e.Id).Skip(1).Take(2).ToListAsync();
    Assert.That(page.Select(r => r.Name), Is.EqualTo(new[] { "b", "c" }));
  }

  // ── QueryCustomizer at adapter level ─────────────────────────────────

  [Test]
  public async Task QueryCustomizer_AppliedBeforeStepCompositions()
  {
    await SeedAsync(
      new TestEntity { Id = 1, Name = "low",  Value = 1.0 },
      new TestEntity { Id = 2, Name = "med",  Value = 2.5 },
      new TestEntity { Id = 3, Name = "high", Value = 5.0 }
    );

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>(
      "q",
      _factory,
      queryCustomizer: q => q.Where(e => e.Value >= 2.0)
    );

    var load = await item.Load().Run();
    var query = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)load).Value;

    var rows = await query.OrderBy(e => e.Id).ToListAsync();
    Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[] { "med", "high" }));
  }

  // ── Fused INSERT-FROM-SELECT ─────────────────────────────────────────

  [Test]
  public async Task Save_WithMatchingScope_UsesFusedInsertFromSelect()
  {
    // Source and destination share the same factory (same DbScope) but
    // are different DbSets — Items → Singleton. Trigger the fused path.
    await SeedAsync(
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 }
    );

    var source = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>(
      "items", _factory
    );

    // Build a destination over a different table with a compatible
    // shape via projection.
    var destination = new EFCoreQueryStorageAdapter<TestSingletonEntity>(
      () => _factory.CreateDbContext(),
      allowEmptyData: true,
      scope: DbScope.Inferred(_factory)
    );

    var loaded = await source.Load().Run();
    var sourceQuery = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)loaded).Value;

    var projected = sourceQuery.Project<TestSingletonEntity>(ctx =>
      ctx.Set<TestEntity>().Select(e => new TestSingletonEntity
      {
        Id = e.Id,
        Description = e.Name,
      })
    );

    var saveResult = await destination.Save(projected).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Fused INSERT-FROM-SELECT path must succeed.");

    using var ctx = _factory.CreateDbContext();
    var rows = await ctx.Singleton.OrderBy(e => e.Id).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.Select(r => r.Description), Is.EqualTo(new[] { "a", "b" }));
  }

  [Test]
  public async Task Save_PlainEnumerable_UsesMaterialisedPath()
  {
    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);

    var input = new[]
    {
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 },
    };

    var saveResult = await item.Save(input).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    using var ctx = _factory.CreateDbContext();
    var rows = await ctx.Items.OrderBy(e => e.Id).ToListAsync();
    Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[] { "a", "b" }));
  }

  [Test]
  public async Task Save_SelfReferential_FallsBackToMaterialised()
  {
    // SELECT references the destination table itself — fused path
    // would receive 0 rows after the DELETE; adapter must fall back
    // to RemoveRange + AddRange.
    await SeedAsync(
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 }
    );

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var loaded = await item.Load().Run();
    var query = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)loaded).Value;

    var saveResult = await item.Save(query).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Self-referential save should silently fall back to materialised path, not 0-row out.");

    using var ctx = _factory.CreateDbContext();
    var rows = await ctx.Items.OrderBy(e => e.Id).ToListAsync();
    Assert.That(rows, Has.Count.EqualTo(2),
      "Materialised fallback must preserve all rows — 0-row truncation is the bug we're avoiding.");
  }

  // ── Round-trip + Exists ──────────────────────────────────────────────

  [Test]
  public async Task RoundTrip_ViaCatalogItem()
  {
    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);

    var saved = await item.Save(new[]
    {
      new TestEntity { Id = 1, Name = "x", Value = 1.5 },
    }).Run();
    Assert.That(saved, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var load = await item.Load().Run();
    var query = (DbQuery<TestEntity>)((EffResult<IEnumerable<TestEntity>>.Success)load).Value;
    var rows = await query.ToListAsync();
    Assert.That(rows.Single().Name, Is.EqualTo("x"));
  }

  [Test]
  public async Task Exists_TrueWhenRowsPresent()
  {
    await SeedAsync(new TestEntity { Id = 1, Name = "a", Value = 1.0 });

    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var existsResult = await item.Exists().Run();
    var exists = ((EffResult<bool>.Success)existsResult).Value;
    Assert.That(exists, Is.True);
  }

  [Test]
  public async Task Exists_FalseWhenEmpty()
  {
    var item = ItemFactory.Enumerable.EFCoreQuery<TestEntity, TestDbContext>("q", _factory);
    var existsResult = await item.Exists().Run();
    var exists = ((EffResult<bool>.Success)existsResult).Value;
    Assert.That(exists, Is.False);
  }
}
