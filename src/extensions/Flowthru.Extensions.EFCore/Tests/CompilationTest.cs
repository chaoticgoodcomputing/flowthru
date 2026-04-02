using Flowthru.Data;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Minimal compilation test to verify extension pattern works.
/// </summary>
public class CompilationTest
{
  public void PartialClassExtensionWorks()
  {
    // This code should compile if extension pattern is working
    DbContext? context = null;

    // Extension method from Flowthru.Extensions.EFCore
    var entry = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity>("test", context!);

    // Verify it returns the correct type
    var _ = entry as IItem<IEnumerable<TestEntity>>;
  }

  public void TypedContextFactoryOverloadsWork()
  {
    Func<TestDbContext> typedFactory = () => null!;

    // Func<TContext> overload — no cast of DbContext in save delegate
    var entryTypedFactory = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      typedFactory
    );
    var _ = entryTypedFactory as IItem<IEnumerable<TestEntity>>;

    // Func<TContext> with typed save delegate — TContext flows to delegate, no cast needed
    var entryWithSaveFunc = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      typedFactory,
      saveFunc: (db, data, ct) => Task.CompletedTask
    );

    // Func<TContext> with query customizer
    var entryWithCustomizer = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      typedFactory,
      queryCustomizer: q => q.Where(e => e.Id > 0)
    );
  }

  public void DbContextFactoryOverloadsWork()
  {
    IDbContextFactory<TestDbContext> factory = new TestDbContextFactory();

    // IDbContextFactory<TContext> overload — idiomatic EFCore concurrency pattern
    var entryFactory = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      factory
    );
    var _ = entryFactory as IItem<IEnumerable<TestEntity>>;

    // IDbContextFactory<TContext> with typed save delegate
    var entryWithSaveFunc = EFCoreCatalogEntries.Enumerable.EFCore<TestEntity, TestDbContext>(
      "test",
      factory,
      saveFunc: (db, data, ct) => Task.CompletedTask
    );
  }

  public void SingleTypedContextOverloadsWork()
  {
    Func<TestDbContext> typedFactory = () => null!;
    IDbContextFactory<TestDbContext> factory = new TestDbContextFactory();

    // Single Func<TContext> with typed save delegate
    var entryTyped = EFCoreCatalogEntries.Single.EFCore<TestEntity, TestDbContext>(
      "test",
      typedFactory,
      saveFunc: (db, data, ct) => Task.CompletedTask
    );
    var _ = entryTyped as IItem<TestEntity>;

    // Single IDbContextFactory<TContext>
    var entryFactory = EFCoreCatalogEntries.Single.EFCore<TestEntity, TestDbContext>(
      "test",
      factory
    );
  }

  private class TestEntity
  {
    public int Id { get; set; }
  }

  private class TestDbContext : DbContext { }

  private class TestDbContextFactory : IDbContextFactory<TestDbContext>
  {
    public TestDbContext CreateDbContext() => null!;
  }
}
