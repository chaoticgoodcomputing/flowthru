using Flowthru.Data.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests.Fixtures;

/// <summary>Simple flat entity used by the storage-adapter tests.</summary>
[FlowthruSchema]
public partial record TestEntity
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required double Value { get; init; }
}

/// <summary>Single-row entity for <see cref="EFCoreSingleStorageAdapter{T}"/> tests.</summary>
[FlowthruSchema]
public partial record TestSingletonEntity
{
  public required int Id { get; init; }
  public required string Description { get; init; }
}

/// <summary>
/// Entity with an <c>UpdatedAt</c> timestamp column — used by the
/// <see cref="EFCoreFingerprintingStorageAdapter{T}"/> tests to
/// exercise the cache-plan opt-in
/// (<c>EFCoreStorageAdapter.WithFingerprintColumn(t =&gt; t.UpdatedAt)</c>).
/// </summary>
[FlowthruSchema]
public partial record TimestampedEntity
{
  public required int Id { get; init; }
  public required string Name { get; init; }
  public required DateTime UpdatedAt { get; init; }
}

/// <summary>
/// SQLite-backed test context. Two DbSets — one for the collection
/// adapter, one for the single-entity adapter.
/// </summary>
public sealed class TestDbContext : DbContext
{
  public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

  public DbSet<TestEntity> Items => Set<TestEntity>();
  public DbSet<TestSingletonEntity> Singleton => Set<TestSingletonEntity>();
  public DbSet<TimestampedEntity> TimestampedItems => Set<TimestampedEntity>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
    modelBuilder.Entity<TestSingletonEntity>().HasKey(e => e.Id);
    modelBuilder.Entity<TimestampedEntity>().HasKey(e => e.Id);
  }
}

/// <summary>Minimal <see cref="IDbContextFactory{TContext}"/> over a fixed options object.</summary>
internal sealed class TestSqliteFactory : IDbContextFactory<TestDbContext>
{
  private readonly DbContextOptions<TestDbContext> _options;
  public TestSqliteFactory(DbContextOptions<TestDbContext> options) => _options = options;
  public TestDbContext CreateDbContext() => new(_options);
}

/// <summary>
/// Builds a fresh <see cref="IDbContextFactory{TContext}"/> over a
/// unique on-disk SQLite file. Each test gets an isolated database
/// file so tests don't interfere with one another.
/// </summary>
public static class TestDbContextFactoryBuilder
{
  public static (IDbContextFactory<TestDbContext> Factory, string DbPath) Build()
  {
    var dbPath = Path.Combine(Path.GetTempPath(), $"flowthru-efcore-{Guid.NewGuid():N}.db");
    var options = new DbContextOptionsBuilder<TestDbContext>()
      .UseSqlite($"Data Source={dbPath}")
      .Options;

    IDbContextFactory<TestDbContext> factory = new TestSqliteFactory(options);
    using (var ctx = factory.CreateDbContext())
    {
      ctx.Database.EnsureCreated();
    }
    return (factory, dbPath);
  }
}
