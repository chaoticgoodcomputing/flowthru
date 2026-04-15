using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// Minimal compilation test to verify extension pattern works.
/// </summary>
public class CompilationTest
{
    /// <summary>
    /// Verifies that the partial class and extension method patterns compile correctly, allowing EFCoreItemFactory.Enumerable.EFCore to be used as intended.
    /// </summary>
    public void PartialClassExtensionWorks()
    {
        // This code should compile if extension pattern is working
        DbContext? context = null;

        // Extension method from Flowthru.Extensions.EFCore
        var entry = EFCoreItemFactory.Enumerable.EFCore<TestEntity>("test", context!);

        // Verify it returns the correct type
        var _ = entry as IItem<IEnumerable<TestEntity>>;
    }

    /// <summary>
    /// Verifies that the typed context factory overloads compile correctly, allowing for type-safe DbContext factories without casts in delegates. This ensures that the generic type parameters flow through the factory and delegates as intended.
    /// </summary>
    public void TypedContextFactoryOverloadsWork()
    {
        Func<TestDbContext> typedFactory = () => null!;

        // Func<TContext> overload — no cast of DbContext in save delegate
        var entryTypedFactory = EFCoreItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
          "test",
          typedFactory
        );
        var _ = entryTypedFactory as IItem<IEnumerable<TestEntity>>;

        // Func<TContext> with typed save delegate — TContext flows to delegate, no cast needed
        var entryWithSaveFunc = EFCoreItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
          "test",
          typedFactory,
          saveFunc: (db, data, ct) => Task.CompletedTask
        );

        // Func<TContext> with query customizer
        var entryWithCustomizer = EFCoreItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
          "test",
          typedFactory,
          queryCustomizer: q => q.Where(e => e.Id > 0)
        );
    }

    /// <summary>
    /// Verifies that the IDbContextFactory overloads compile correctly, allowing for the idiomatic EFCore pattern of using IDbContextFactory for per-operation context creation. This ensures that both the factory and the optional save delegate with typed context compile as intended.
    /// </summary>
    public void DbContextFactoryOverloadsWork()
    {
        IDbContextFactory<TestDbContext> factory = new TestDbContextFactory();

        // IDbContextFactory<TContext> overload — idiomatic EFCore concurrency pattern
        var entryFactory = EFCoreItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
          "test",
          factory
        );
        var _ = entryFactory as IItem<IEnumerable<TestEntity>>;

        // IDbContextFactory<TContext> with typed save delegate
        var entryWithSaveFunc = EFCoreItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>(
          "test",
          factory,
          saveFunc: (db, data, ct) => Task.CompletedTask
        );
    }

    /// <summary>
    /// Verifies that the single-entity EFCore item factory overloads compile correctly, allowing for both the typed context factory and IDbContextFactory patterns to be used with EFCoreSingleStorageAdapter. This ensures that the extension methods for single-entity storage compile and return the correct types as intended.
    /// The single-entity storage adapter has similar overloads to the enumerable version, so this test ensures that both sets of overloads work correctly in parallel.
    /// Note: This test focuses on compilation; runtime behavior (e.g. actual database operations) is not verified here.
    ///
    /// </summary>
    public void SingleTypedContextOverloadsWork()
    {
        Func<TestDbContext> typedFactory = () => null!;
        IDbContextFactory<TestDbContext> factory = new TestDbContextFactory();

        // Single Func<TContext> with typed save delegate
        var entryTyped = EFCoreItemFactory.Single.EFCore<TestEntity, TestDbContext>(
          "test",
          typedFactory,
          saveFunc: (db, data, ct) => Task.CompletedTask
        );
        var _ = entryTyped as IItem<TestEntity>;

        // Single IDbContextFactory<TContext>
        var entryFactory = EFCoreItemFactory.Single.EFCore<TestEntity, TestDbContext>("test", factory);
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
