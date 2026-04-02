using Flowthru.Abstractions;
using Flowthru.Data;
using Flowthru.Data.Storage;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Data;

public static partial class EFCoreCatalogEntries
{
  public static partial class Enumerable
  {
    /// <summary>
    /// Creates an Entity Framework Core catalog entry for database-backed collections.
    /// </summary>
    /// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="context">DbContext instance (caller owns lifecycle)</param>
    /// <param name="allowEmptyData">If true, empty tables pass validation (default: false)</param>
    /// <returns>Catalog entry for EFCore database storage</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> Read/write entities from relational databases using EF Core
    /// </para>
    /// <para>
    /// <strong>DbContext Lifecycle:</strong> Caller provides DbContext and manages its lifecycle.
    /// Use this overload when DbContext comes from DI container or is shared across operations.
    /// </para>
    /// <para>
    /// <strong>Read-Only Entries:</strong>
    /// To create a read-only catalog entry, apply a constraint:
    /// <c>.Constrain(traits => traits with { CanWrite = false })</c>
    /// </para>
    /// <para>
    /// <strong>Empty Data Validation:</strong>
    /// By default (allowEmptyData: false), empty tables fail pre-flight validation.
    /// Set allowEmptyData: true for tables that may legitimately be empty.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In catalog
    /// public static partial class DataCatalog
    /// {
    ///   public static ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt; Companies(DbContext db) =>
    ///     CatalogEntries.Enumerable.EFCore&lt;Company&gt;("companies", db);
    /// }
    ///
    /// // In pipeline
    /// var pipeline = new FlowBuilder("CompanyPipeline")
    ///   .AddStep("load_companies", catalog => new LoadCompaniesStep(
    ///     outputs: catalog.Companies(db)
    ///   ))
    ///   .Build();
    /// </code>
    /// </example>
    public static Item<IEnumerable<T>> EFCore<T>(
      string label,
      DbContext context,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
    )
      where T : class
    {
      var storage = new EFCoreStorageAdapter<T>(context, allowEmptyData, queryCustomizer, saveFunc);
      return new Item<IEnumerable<T>>(label, storage);
    }

    /// <summary>
    /// Creates an Entity Framework Core catalog entry with a DbContext factory.
    /// </summary>
    /// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="contextFactory">Factory function to create DbContext instances per operation</param>
    /// <param name="allowEmptyData">If true, empty tables pass validation (default: false)</param>
    /// <returns>Catalog entry for EFCore database storage</returns>
    /// <remarks>
    /// <para>
    /// <strong>Use Case:</strong> When DbContext should be created fresh for each Load/Save operation
    /// </para>
    /// <para>
    /// <strong>DbContext Lifecycle:</strong> Adapter creates DbContext via factory and disposes it
    /// after each operation. Use this overload for scoped DbContext patterns.
    /// </para>
    /// <para>
    /// <strong>Read-Only Entries:</strong>
    /// To create a read-only catalog entry, apply a constraint:
    /// <c>.Constrain(traits => traits with { CanWrite = false })</c>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In catalog
    /// public static partial class DataCatalog
    /// {
    ///   private static AppDbContext CreateDbContext() =>
    ///     new AppDbContext(new DbContextOptionsBuilder&lt;AppDbContext&gt;()
    ///       .UseSqlServer(connectionString)
    ///       .Options);
    ///
    ///   public static ICatalogEntry&lt;IEnumerable&lt;Company&gt;&gt; Companies() =>
    ///     CatalogEntries.Enumerable.EFCore&lt;Company&gt;("companies", CreateDbContext);
    /// }
    /// </code>
    /// </example>
    public static Item<IEnumerable<T>> EFCore<T>(
      string label,
      Func<DbContext> contextFactory,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
    )
      where T : class
    {
      var storage = new EFCoreStorageAdapter<T>(
        contextFactory,
        allowEmptyData,
        queryCustomizer,
        saveFunc
      );
      return new Item<IEnumerable<T>>(label, storage);
    }

    /// <summary>
    /// Creates an EFCore catalog entry with a typed DbContext factory.
    /// The concrete <typeparamref name="TContext"/> flows through to the save delegate,
    /// eliminating any downcast inside the delegate body.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TContext">Concrete DbContext type</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="contextFactory">Typed factory; called per Load/Save operation</param>
    /// <param name="allowEmptyData">If true, empty tables pass validation (default: false)</param>
    /// <param name="queryCustomizer">Optional query transformation applied before materialization (e.g. Include, Where, OrderBy)</param>
    /// <param name="saveFunc">Optional save delegate receiving the concrete <typeparamref name="TContext"/>.
    /// Defaults to RemoveRange + AddRange when null.</param>
    /// <returns>Catalog entry for EFCore database storage</returns>
    public static Item<IEnumerable<T>> EFCore<T, TContext>(
      string label,
      Func<TContext> contextFactory,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
    )
      where T : class
      where TContext : DbContext
    {
      Func<DbContext> baseFactory = () => contextFactory();
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? baseSaveFunc =
        saveFunc != null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;
      var storage = new EFCoreStorageAdapter<T>(
        baseFactory,
        allowEmptyData,
        queryCustomizer,
        baseSaveFunc
      );
      return new Item<IEnumerable<T>>(label, storage);
    }

    /// <summary>
    /// Creates an EFCore catalog entry using <see cref="IDbContextFactory{TContext}"/> —
    /// the idiomatic EFCore pattern for per-operation context isolation and concurrent node safety.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TContext">Concrete DbContext type</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="contextFactory">EFCore context factory; a fresh context is created per Load/Save operation</param>
    /// <param name="allowEmptyData">If true, empty tables pass validation (default: false)</param>
    /// <param name="queryCustomizer">Optional query transformation applied before materialization</param>
    /// <param name="saveFunc">Optional save delegate receiving the concrete <typeparamref name="TContext"/>.
    /// Defaults to RemoveRange + AddRange when null.</param>
    /// <returns>Catalog entry for EFCore database storage</returns>
    public static Item<IEnumerable<T>> EFCore<T, TContext>(
      string label,
      IDbContextFactory<TContext> contextFactory,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
    )
      where T : class
      where TContext : DbContext
    {
      Func<DbContext> baseFactory = () => contextFactory.CreateDbContext();
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? baseSaveFunc =
        saveFunc != null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;
      var storage = new EFCoreStorageAdapter<T>(
        baseFactory,
        allowEmptyData,
        queryCustomizer,
        baseSaveFunc
      );
      return new Item<IEnumerable<T>>(label, storage);
    }
  }
}
