using EFCore.BulkExtensions;
using Flowthru.Extensions.EFCore.Bulk.Internal;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk;

/// <summary>
/// Factory methods that produce <c>saveFunc</c> delegates for use with
/// <c>EFCoreItemFactory.Enumerable.EFCore</c>. Each method returns a
/// <c>Func&lt;TContext, IEnumerable&lt;T&gt;, CancellationToken, Task&gt;</c>
/// compatible with the existing catalog item factory signature.
/// </summary>
/// <example>
/// <code>
/// // In a catalog definition:
/// public IItem&lt;IEnumerable&lt;MyEntity&gt;&gt; OutputEntities =&gt;
///     CreateItem(() =&gt; EFCoreItemFactory.Enumerable.EFCore&lt;MyEntity, MyDbContext&gt;(
///         label: "OutputEntities",
///         contextFactory: _factory,
///         saveFunc: BulkSave.TruncateAndInsert&lt;MyEntity, MyDbContext&gt;()));
/// </code>
/// </example>
public static class BulkSave
{
  /// <summary>
  /// Bulk insert rows. Does not modify or remove existing data.
  /// Uses the provider's fastest bulk-load path (e.g. Npgsql binary COPY for PostgreSQL).
  /// </summary>
  /// <typeparam name="T">The entity type.</typeparam>
  /// <typeparam name="TContext">The DbContext type.</typeparam>
  /// <param name="options">Optional bulk operation configuration.</param>
  /// <returns>A <c>saveFunc</c> delegate for use with <c>EFCoreItemFactory</c>.</returns>
  public static Func<TContext, IEnumerable<T>, CancellationToken, Task> Insert<T, TContext>(
    BulkSaveOptions? options = null
  )
    where T : class
    where TContext : DbContext
  {
    return async (db, data, ct) =>
    {
      var config = BulkConfigMapper.ToBulkConfig(options);
      await db.BulkInsertAsync(data, config, progress: options?.OnProgress, cancellationToken: ct);
    };
  }

  /// <summary>
  /// Truncate the target table, then bulk insert all rows.
  /// This is a full-replacement strategy equivalent to the common pattern of
  /// <c>TRUNCATE TABLE ... ; INSERT ...</c> but using the provider's bulk-load path.
  /// </summary>
  /// <typeparam name="T">The entity type.</typeparam>
  /// <typeparam name="TContext">The DbContext type.</typeparam>
  /// <param name="options">Optional bulk operation configuration.</param>
  /// <returns>A <c>saveFunc</c> delegate for use with <c>EFCoreItemFactory</c>.</returns>
  public static Func<TContext, IEnumerable<T>, CancellationToken, Task> TruncateAndInsert<
    T,
    TContext
  >(BulkSaveOptions? options = null)
    where T : class
    where TContext : DbContext
  {
    return async (db, data, ct) =>
    {
      await db.TruncateAsync<T>(cancellationToken: ct);
      var config = BulkConfigMapper.ToBulkConfig(options);
      await db.BulkInsertAsync(data, config, progress: options?.OnProgress, cancellationToken: ct);
    };
  }

  /// <summary>
  /// Bulk upsert: insert new rows and update existing rows matched by primary key.
  /// </summary>
  /// <typeparam name="T">The entity type.</typeparam>
  /// <typeparam name="TContext">The DbContext type.</typeparam>
  /// <param name="options">Optional bulk operation configuration.</param>
  /// <returns>A <c>saveFunc</c> delegate for use with <c>EFCoreItemFactory</c>.</returns>
  public static Func<TContext, IEnumerable<T>, CancellationToken, Task> InsertOrUpdate<T, TContext>(
    BulkSaveOptions? options = null
  )
    where T : class
    where TContext : DbContext
  {
    return async (db, data, ct) =>
    {
      var config = BulkConfigMapper.ToBulkConfig(options);
      await db.BulkInsertOrUpdateAsync(
        data,
        config,
        progress: options?.OnProgress,
        cancellationToken: ct
      );
    };
  }

  /// <summary>
  /// Full sync: insert new rows, update existing rows, and delete rows not present
  /// in the input data. Matched by primary key.
  /// </summary>
  /// <typeparam name="T">The entity type.</typeparam>
  /// <typeparam name="TContext">The DbContext type.</typeparam>
  /// <param name="options">Optional bulk operation configuration.</param>
  /// <returns>A <c>saveFunc</c> delegate for use with <c>EFCoreItemFactory</c>.</returns>
  public static Func<TContext, IEnumerable<T>, CancellationToken, Task> InsertOrUpdateOrDelete<
    T,
    TContext
  >(BulkSaveOptions? options = null)
    where T : class
    where TContext : DbContext
  {
    return async (db, data, ct) =>
    {
      var config = BulkConfigMapper.ToBulkConfig(options);
      await db.BulkInsertOrUpdateOrDeleteAsync(
        data,
        config,
        progress: options?.OnProgress,
        cancellationToken: ct
      );
    };
  }
}
