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
    /// var pipeline = new PipelineBuilder("CompanyPipeline")
    ///   .AddNode("load_companies", catalog => new LoadCompaniesNode(
    ///     outputs: catalog.Companies(db)
    ///   ))
    ///   .Build();
    /// </code>
    /// </example>
    public static CatalogEntry<IEnumerable<T>> EFCore<T>(
      string label,
      DbContext context,
      bool allowEmptyData = false
    )
      where T : class
    {
      var storage = new EFCoreStorageAdapter<T>(context, allowEmptyData);
      return new CatalogEntry<IEnumerable<T>>(label, storage);
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
    public static CatalogEntry<IEnumerable<T>> EFCore<T>(
      string label,
      Func<DbContext> contextFactory,
      bool allowEmptyData = false
    )
      where T : class
    {
      var storage = new EFCoreStorageAdapter<T>(contextFactory, allowEmptyData);
      return new CatalogEntry<IEnumerable<T>>(label, storage);
    }
  }
}
