using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Direct exercises of <see cref="EFCoreStorageAdapter{T}"/> over a
/// SQLite backend. Validates load / save / round-trip / inspection /
/// read-only-fail-fast behavior on the new FP shape.
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreStorageAdapterTests
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
      try { File.Delete(_dbPath); }
      catch { /* best effort */ }
    }
  }

  // ── Round-trip ───────────────────────────────────────────────────────

  [Test]
  public async Task SaveLoad_RoundTrips()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("items", _factory);

    var input = new[]
    {
      new TestEntity { Id = 1, Name = "Alice", Value = 1.5 },
      new TestEntity { Id = 2, Name = "Bob",   Value = 2.5 },
    };

    var save = await item.Save(input).Run();
    Assert.That(save, Is.InstanceOf<EffResult<FlowUnit>.Success>());

    var load = await item.Load().Run();
    var rows = ((EffResult<IEnumerable<TestEntity>>.Success)load).Value.ToList();
    Assert.That(rows, Has.Count.EqualTo(2));
    Assert.That(rows.OrderBy(r => r.Id).Select(r => r.Name),
      Is.EqualTo(new[] { "Alice", "Bob" }));
  }

  [Test]
  public async Task DefaultSave_ReplacesExistingRows()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("items", _factory);

    await item.Save(new[]
    {
      new TestEntity { Id = 99, Name = "stale", Value = 9.9 },
    }).Run();

    await item.Save(new[]
    {
      new TestEntity { Id = 1, Name = "fresh", Value = 1.0 },
    }).Run();

    var load = await item.Load().Run();
    var rows = ((EffResult<IEnumerable<TestEntity>>.Success)load).Value.ToList();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Id, Is.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("fresh"));
  }

  [Test]
  public async Task QueryCustomizer_FiltersAndOrders()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
      "items",
      _factory,
      queryCustomizer: q => q.Where(e => e.Value >= 2.0).OrderBy(e => e.Id)
    );

    await item.Save(new[]
    {
      new TestEntity { Id = 1, Name = "low",  Value = 1.0 },
      new TestEntity { Id = 2, Name = "med",  Value = 2.5 },
      new TestEntity { Id = 3, Name = "high", Value = 5.0 },
    }).Run();

    var load = await item.Load().Run();
    var rows = ((EffResult<IEnumerable<TestEntity>>.Success)load).Value.ToList();
    Assert.That(rows.Select(r => r.Name), Is.EqualTo(new[] { "med", "high" }),
      "queryCustomizer should apply Where + OrderBy before materialisation.");
  }

  [Test]
  public async Task SaveFunc_OverridesDefaultStrategy()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
      "items",
      _factory,
      saveFunc: async (ctx, data, ct) =>
      {
        // Custom save strategy: append rather than replace.
        await ctx.Set<TestEntity>().AddRangeAsync(data, ct).ConfigureAwait(false);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
      }
    );

    await item.Save(new[] { new TestEntity { Id = 1, Name = "a", Value = 1.0 } }).Run();
    await item.Save(new[] { new TestEntity { Id = 2, Name = "b", Value = 2.0 } }).Run();

    var load = await item.Load().Run();
    var rows = ((EffResult<IEnumerable<TestEntity>>.Success)load).Value.ToList();
    Assert.That(rows, Has.Count.EqualTo(2),
      "Custom append-style saveFunc should preserve both rows across saves.");
  }

  // ── Inspection ───────────────────────────────────────────────────────

  [Test]
  public async Task InspectShallow_EmptyTable_FailsByDefault()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("items", _factory);

    var inspect = await item.InspectShallow(10).Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;

    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors.Any(e => e.ErrorType == ValidationErrorType.EmptyDataset),
      Is.True,
      "Empty tables should surface as EmptyDataset by default."
    );
  }

  [Test]
  public async Task InspectShallow_AllowEmptyData_PassesOnEmpty()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
      "items", _factory, allowEmptyData: true
    );

    var inspect = await item.InspectShallow(10).Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;
    Assert.That(validation.IsValid, Is.True,
      "allowEmptyData: true should pass empty tables through pre-flight.");
  }

  [Test]
  public async Task InspectShallow_PopulatedTable_Succeeds()
  {
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("items", _factory);
    await item.Save(new[] { new TestEntity { Id = 1, Name = "x", Value = 1.0 } }).Run();

    var inspect = await item.InspectShallow(5).Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;
    Assert.That(validation.IsValid, Is.True);
  }

  // Note: the legacy <c>Item.Constrain(traits =&gt; traits with { CanWrite = false })</c>
  // surface is a Core-side carryover — the new IItem<T> interface
  // doesn't expose Constrain yet. Once it returns, a read-only
  // constraint test belongs here.

  // ── Configuration validation (pre-flight at adapter-construction) ────

  [Test]
  public void Constructor_EntityNotConfigured_ThrowsAtConstruction()
  {
    // Use a DbContext type that doesn't include UnconfiguredEntity in its model.
    Assert.That(
      () => ItemFactory.Enumerable.EFCore<UnconfiguredEntity, TestDbContext>(
        "unconfigured", _factory
      ),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contains("not configured in DbContext"),
      "The adapter should fail at construction time when its entity type isn't "
      + "in the DbContext's model — pre-flight cost surfaces at catalog wire-up, "
      + "not at first Load/Save."
    );
  }

  /// <summary>Entity intentionally NOT configured in TestDbContext.OnModelCreating.</summary>
  [Flowthru.Data.Schema.FlowthruSchema]
  public partial record UnconfiguredEntity
  {
    public required int Id { get; init; }
  }
}
