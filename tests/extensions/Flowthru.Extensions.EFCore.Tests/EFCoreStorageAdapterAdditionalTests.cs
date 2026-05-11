using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Coverage-fill for the surfaces of <see cref="EFCoreStorageAdapter{T}"/>
/// not exercised by <see cref="EFCoreStorageAdapterTests"/>:
/// <list type="bullet">
///   <item><c>Exists</c> on populated and empty tables.</item>
///   <item><c>IHasEfficientCount.GetCountAsync</c>.</item>
///   <item><c>InspectDeep</c> and <c>InspectTarget</c>.</item>
///   <item>The injected-DbContext constructor (caller-owned lifetime).</item>
///   <item>Read-only constraint short-circuit on <c>Save</c>.</item>
///   <item>The two model-validation guards beyond entity-not-configured
///     (no primary key + array-typed key).</item>
/// </list>
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreStorageAdapterAdditionalTests
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

  // ── Exists ──────────────────────────────────────────────────────────

  [Test]
  public async Task Exists_EmptyTable_IsFalse()
  {
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => _factory.CreateDbContext());
    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.False);
  }

  [Test]
  public async Task Exists_PopulatedTable_IsTrue()
  {
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => _factory.CreateDbContext());
    await adapter.Save(new[] { new TestEntity { Id = 1, Name = "x", Value = 1.0 } }).Run();
    var result = await adapter.Exists().Run();
    Assert.That(((EffResult<bool>.Success)result).Value, Is.True);
  }

  // ── GetCountAsync (IHasEfficientCount) ──────────────────────────────

  [Test]
  public async Task GetCountAsync_ReportsRowCount()
  {
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => _factory.CreateDbContext());
    await adapter.Save(new[]
    {
      new TestEntity { Id = 1, Name = "a", Value = 1.0 },
      new TestEntity { Id = 2, Name = "b", Value = 2.0 },
      new TestEntity { Id = 3, Name = "c", Value = 3.0 },
    }).Run();

    var hasCount = (IHasEfficientCount)adapter;
    var result = await hasCount.GetCountAsync().Run();
    Assert.That(((EffResult<int>.Success)result).Value, Is.EqualTo(3));
  }

  // ── InspectDeep + InspectTarget ─────────────────────────────────────

  [Test]
  public async Task InspectDeep_PopulatedTable_Succeeds()
  {
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => _factory.CreateDbContext());
    await adapter.Save(new[] { new TestEntity { Id = 1, Name = "x", Value = 1.0 } }).Run();
    var inspect = await adapter.InspectDeep().Run();
    Assert.That(
      ((EffResult<ValidationResult>.Success)inspect).Value.IsValid,
      Is.True
    );
  }

  [Test]
  public async Task InspectDeep_EmptyTable_FailsByDefault()
  {
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => _factory.CreateDbContext());
    var inspect = await adapter.InspectDeep().Run();
    var validation = ((EffResult<ValidationResult>.Success)inspect).Value;
    Assert.That(validation.IsValid, Is.False);
    Assert.That(
      validation.Errors.Any(e => e.ErrorType == ValidationErrorType.EmptyDataset),
      Is.True
    );
  }

  [Test]
  public async Task InspectTarget_ExistingTable_Succeeds()
  {
    // InspectTarget probes the write target (table existence + shape).
    // It should pass on an empty but configured table.
    var adapter = new EFCoreStorageAdapter<TestEntity>(() => _factory.CreateDbContext());
    var inspect = await adapter.InspectTarget().Run();
    Assert.That(
      ((EffResult<ValidationResult>.Success)inspect).Value.IsValid,
      Is.True
    );
  }

  // ── Injected-context constructor (caller-owned) ─────────────────────

  [Test]
  public async Task InjectedContext_RoundTripsAndCallerOwnsLifetime()
  {
    using var ctx = _factory.CreateDbContext();
    var adapter = new EFCoreStorageAdapter<TestEntity>(ctx);

    await adapter.Save(new[] { new TestEntity { Id = 7, Name = "injected", Value = 7.0 } }).Run();
    var load = await adapter.Load().Run();
    var rows = ((EffResult<IEnumerable<TestEntity>>.Success)load).Value.ToList();
    Assert.That(rows, Has.Count.EqualTo(1));
    Assert.That(rows[0].Name, Is.EqualTo("injected"));

    // Caller still owns the context — no ObjectDisposedException after the run.
    Assert.That(() => ctx.Set<TestEntity>().Count(), Throws.Nothing);
  }

  // ── Read-only adapter — Save short-circuits to a typed failure ──────

  [Test]
  public async Task Save_OnReadOnlyConstrainedItem_FailsWithExternalError()
  {
    // Constrain narrows the underlying adapter's traits — CanWrite=false
    // makes Save fail with a typed RuntimeError instead of touching the DB.
    var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("items", _factory);
    var readOnlyItem = item.Constrain(traits => traits with { CanWrite = false });

    var save = await readOnlyItem.Save(new[]
    {
      new TestEntity { Id = 1, Name = "x", Value = 1.0 },
    }).Run();

    Assert.That(save, Is.InstanceOf<EffResult<FlowUnit>.Failure>());
  }

  // ── Constructor argument validation ─────────────────────────────────

  [Test]
  public void Constructor_NullInjectedContext_Throws()
  {
    Assert.That(
      () => new EFCoreStorageAdapter<TestEntity>(context: (DbContext)null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void Constructor_NullFactory_Throws()
  {
    Assert.That(
      () => new EFCoreStorageAdapter<TestEntity>(contextFactory: (Func<DbContext>)null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  // ── Model-validation guards (beyond entity-not-configured) ──────────

  [Test]
  public void Constructor_EntityWithNoPrimaryKey_Throws()
  {
    var options = new DbContextOptionsBuilder<NoKeyDbContext>()
      .UseSqlite($"Data Source=:memory:")
      .Options;
    Assert.That(
      () => new EFCoreStorageAdapter<NoKeyEntity>(() => new NoKeyDbContext(options)),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("no primary key")
    );
  }

  [Test]
  public void Constructor_EntityWithArrayKey_Throws()
  {
    var options = new DbContextOptionsBuilder<ArrayKeyDbContext>()
      .UseSqlite($"Data Source=:memory:")
      .Options;
    Assert.That(
      () => new EFCoreStorageAdapter<ArrayKeyEntity>(() => new ArrayKeyDbContext(options)),
      Throws.TypeOf<InvalidOperationException>()
        .With.Message.Contain("array")
    );
  }

  // ── Helper DbContexts for the model-validation guard tests ──────────

  public class NoKeyEntity
  {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
  }

  public class NoKeyDbContext : DbContext
  {
    public NoKeyDbContext(DbContextOptions<NoKeyDbContext> options) : base(options) { }
    public DbSet<NoKeyEntity> Entities => Set<NoKeyEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<NoKeyEntity>().HasNoKey();
    }
  }

  public class ArrayKeyEntity
  {
    public byte[] Id { get; set; } = Array.Empty<byte>();
    public string Name { get; set; } = string.Empty;
  }

  public class ArrayKeyDbContext : DbContext
  {
    public ArrayKeyDbContext(DbContextOptions<ArrayKeyDbContext> options) : base(options) { }
    public DbSet<ArrayKeyEntity> Entities => Set<ArrayKeyEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<ArrayKeyEntity>().HasKey(e => e.Id);
    }
  }
}
