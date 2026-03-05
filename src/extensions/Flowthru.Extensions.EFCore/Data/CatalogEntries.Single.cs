using Flowthru.Abstractions;
using Flowthru.Data;
using Flowthru.Data.Storage;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Data;

public static partial class EFCoreCatalogEntries
{
  public static partial class Single
  {
    /// <summary>
    /// Creates an Entity Framework Core catalog entry for single database-backed entities.
    /// </summary>
    /// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="context">DbContext instance (caller owns lifecycle)</param>
    /// <param name="readOnly">If true, prevents Save operations</param>
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
    /// <strong>Capabilities:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ISeedable: true if table exists and contains exactly one row</item>
    /// <item>IReadOnly: configurable via readOnly parameter</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In catalog
    /// public ICatalogEntry&lt;ModelMetrics&gt; Metrics(DbContext db) =>
    ///   CatalogEntries.Single.EFCore&lt;ModelMetrics&gt;("metrics", db);
    ///
    /// // In pipeline
    /// var pipeline = new PipelineBuilder("MetricsPipeline")
    ///   .AddNode("save_metrics", catalog => new SaveMetricsNode(
    ///     outputs: catalog.Metrics(db)
    ///   ))
    ///   .Build();
    /// </code>
    /// </example>
    public static ICatalogEntry<T> EFCore<T>(string label, DbContext context, bool readOnly = false)
      where T : class
    {
      var adapter = new EFCoreSingleStorageAdapter<T>(context, ownsContext: false, readOnly);
      return new CatalogEntry<T>(label, adapter);
    }

    /// <summary>
    /// Creates an Entity Framework Core catalog entry for single database-backed entities using a factory.
    /// </summary>
    /// <typeparam name="T">Entity type (must be a class configured in DbContext)</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution</param>
    /// <param name="contextFactory">Factory that creates DbContext instances (adapter owns lifecycle)</param>
    /// <param name="readOnly">If true, prevents Save operations</param>
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
    /// <strong>Capabilities:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>ISeedable: true if table exists and contains exactly one row</item>
    /// <item>IReadOnly: configurable via readOnly parameter</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In catalog with factory
    /// private readonly IServiceProvider _serviceProvider;
    ///
    /// public ICatalogEntry&lt;ModelMetrics&gt; Metrics =>
    ///   CatalogEntries.Single.EFCore&lt;ModelMetrics&gt;(
    ///     "metrics",
    ///     () => _serviceProvider.GetRequiredService&lt;MyDbContext&gt;()
    ///   );
    /// </code>
    /// </example>
    public static ICatalogEntry<T> EFCore<T>(
      string label,
      Func<DbContext> contextFactory,
      bool readOnly = false
    )
      where T : class
    {
      var adapter = new EFCoreSingleStorageAdapter<T>(contextFactory, readOnly);
      return new CatalogEntry<T>(label, adapter);
    }
  }
}
