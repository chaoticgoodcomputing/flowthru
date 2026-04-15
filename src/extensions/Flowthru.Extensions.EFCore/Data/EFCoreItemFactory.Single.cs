using Flowthru.Core.Abstractions;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Data;

/// <summary>
/// Factory methods for creating <see cref="Item{T}"/> instances with Entity Framework Core storage adapters.
/// This partial class focuses on single-entity storage; see <see cref="Enumerable"/> for
/// collections of entities.
/// </summary>
public static partial class EFCoreItemFactory
{
    /// <summary>
    /// Factory methods for creating single-entity <see cref="Item{T}"/> instances with Entity Framework Core storage adapters.
    /// </summary>
    public static partial class Single
    {
        /// <summary>
        /// Creates an Entity Framework Core catalog entry for single database-backed entities.
        /// </summary>
        /// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
        /// <param name="label">Unique catalog label for DAG resolution</param>
        /// <param name="context">DbContext instance (caller owns lifecycle)</param>
        /// <param name="allowEmptyData">If true, empty tables pass validation (default: false)</param>
        /// <param name="queryCustomizer">Optional function to customize the query for the entity type</param>
        /// <param name="saveFunc">Optional function to customize the save operation</param>
        /// <returns>Catalog entry for EFCore single entity storage</returns>
        /// <remarks>
        /// <para>
        /// <strong>Use Case:</strong> Store single entities (models, metrics, configs) in database
        /// </para>
        /// <para>
        /// <strong>Implementation:</strong> Stores entity in a table, expects exactly one row on Load.
        /// Save replaces the single row (clear table, insert new row).
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
        /// </remarks>
        /// <example>
        /// <code>
        /// // In catalog
        /// public IItem&lt;ModelMetrics&gt; Metrics(DbContext db) =>
        ///   ItemFactory.Single.EFCore&lt;ModelMetrics&gt;("metrics", db);
        ///
        /// // In pipeline
        /// var pipeline = new FlowBuilder("MetricsPipeline")
        ///   .AddStep("save_metrics", catalog => new SaveMetricsStep(
        ///     outputs: catalog.Metrics(db)
        ///   ))
        ///   .Build();
        /// </code>
        /// </example>
        public static Item<T> EFCore<T>(
          string label,
          DbContext context,
          bool allowEmptyData = false,
          Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
          Func<DbContext, T, CancellationToken, Task>? saveFunc = null
        )
          where T : class
        {
            var adapter = new EFCoreSingleStorageAdapter<T>(
              context,
              ownsContext: false,
              allowEmptyData,
              queryCustomizer,
              saveFunc
            );
            return new Item<T>(label, adapter);
        }

        /// <summary>
        /// Creates an Entity Framework Core catalog entry for single database-backed entities using a factory.
        /// </summary>
        /// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
        /// <param name="label">Unique catalog label for DAG resolution</param>
        /// <param name="contextFactory">Factory that creates DbContext instances (adapter owns lifecycle)</param>
        /// <param name="allowEmptyData">If true, empty tables pass validation (default: false)</param>
        /// <param name="queryCustomizer">Optional function to customize the query for the entity type</param>
        /// <param name="saveFunc">Optional function to customize the save operation</param>
        /// <returns>Catalog entry for EFCore single entity storage</returns>
        /// <remarks>
        /// <para>
        /// <strong>Use Case:</strong> Store single entities when you want adapter to manage DbContext lifecycle
        /// </para>
        /// <para>
        /// <strong>DbContext Lifecycle:</strong> Adapter creates and disposes DbContext per operation.
        /// Use this overload when operations should be isolated or when DbContext is expensive to keep alive.
        /// </para>
        /// <para>
        /// <strong>Read-Only Entries:</strong>
        /// To create a read-only catalog entry, apply a constraint:
        /// <c>.Constrain(traits => traits with { CanWrite = false })</c>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In catalog with factory
        /// private readonly IServiceProvider _serviceProvider;
        ///
        /// public IItem&lt;ModelMetrics&gt; Metrics =>
        ///   ItemFactory.Single.EFCore&lt;ModelMetrics&gt;(
        ///     "metrics",
        ///     () => _serviceProvider.GetRequiredService&lt;MyDbContext&gt;()
        ///   );
        /// </code>
        /// </example>
        public static Item<T> EFCore<T>(
          string label,
          Func<DbContext> contextFactory,
          bool allowEmptyData = false,
          Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
          Func<DbContext, T, CancellationToken, Task>? saveFunc = null
        )
          where T : class
        {
            var adapter = new EFCoreSingleStorageAdapter<T>(
              contextFactory,
              allowEmptyData,
              queryCustomizer,
              saveFunc
            );
            return new Item<T>(label, adapter);
        }

        /// <summary>
        /// Creates a single-entity EFCore catalog entry with a typed DbContext factory.
        /// The concrete <typeparamref name="TContext"/> flows through to the save delegate,
        /// eliminating any downcast inside the delegate body.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TContext">Concrete DbContext type</typeparam>
        /// <param name="label">Unique catalog label for DAG resolution</param>
        /// <param name="contextFactory">Typed factory; called per Load/Save operation</param>
        /// <param name="allowEmptyData">If true, an empty table passes validation (default: false)</param>
        /// <param name="queryCustomizer">Optional query transformation applied before SingleAsync</param>
        /// <param name="saveFunc">Optional save delegate receiving the concrete <typeparamref name="TContext"/>.
        /// Defaults to clear-and-insert when null.</param>
        /// <returns>Catalog entry for EFCore single entity storage</returns>
        public static Item<T> EFCore<T, TContext>(
          string label,
          Func<TContext> contextFactory,
          bool allowEmptyData = false,
          Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
          Func<TContext, T, CancellationToken, Task>? saveFunc = null
        )
          where T : class
          where TContext : DbContext
        {
            Func<DbContext> baseFactory = () => contextFactory();
            Func<DbContext, T, CancellationToken, Task>? baseSaveFunc =
              saveFunc != null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;
            var adapter = new EFCoreSingleStorageAdapter<T>(
              baseFactory,
              allowEmptyData,
              queryCustomizer,
              baseSaveFunc
            );
            return new Item<T>(label, adapter);
        }

        /// <summary>
        /// Creates a single-entity EFCore catalog entry using <see cref="IDbContextFactory{TContext}"/>.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <typeparam name="TContext">Concrete DbContext type</typeparam>
        /// <param name="label">Unique catalog label for DAG resolution</param>
        /// <param name="contextFactory">EFCore context factory; a fresh context is created per Load/Save operation</param>
        /// <param name="allowEmptyData">If true, an empty table passes validation (default: false)</param>
        /// <param name="queryCustomizer">Optional query transformation applied before SingleAsync</param>
        /// <param name="saveFunc">Optional save delegate receiving the concrete <typeparamref name="TContext"/>.
        /// Defaults to clear-and-insert when null.</param>
        /// <returns>Catalog entry for EFCore single entity storage</returns>
        public static Item<T> EFCore<T, TContext>(
          string label,
          IDbContextFactory<TContext> contextFactory,
          bool allowEmptyData = false,
          Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
          Func<TContext, T, CancellationToken, Task>? saveFunc = null
        )
          where T : class
          where TContext : DbContext
        {
            Func<DbContext> baseFactory = () => contextFactory.CreateDbContext();
            Func<DbContext, T, CancellationToken, Task>? baseSaveFunc =
              saveFunc != null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;
            var adapter = new EFCoreSingleStorageAdapter<T>(
              baseFactory,
              allowEmptyData,
              queryCustomizer,
              baseSaveFunc
            );
            return new Item<T>(label, adapter);
        }
    }
}
