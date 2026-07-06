using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Bulk;

/// <summary>
/// Factory methods that produce streaming <see cref="IFlowSink{T}"/> writers for
/// the <c>EFCore.Bulk</c> extension — the sink counterpart to the eager
/// <see cref="BulkSave"/> delegates. A sink is the terminal of a
/// <see cref="FlowSource{A}"/> bulk-load
/// (<c>source.Compile().Into(BulkSink.Insert&lt;T, TContext&gt;(factory))</c>),
/// writing one batch per <c>BulkInsertAsync</c> inside a single transaction so
/// the load is O(batch) in memory and all-or-nothing on failure.
/// </summary>
/// <example>
/// <code>
/// var sink = BulkSink.Insert&lt;MyEntity, MyDbContext&gt;(_factory);
/// await source.Compile().Into(sink).Run();
/// </code>
/// </example>
public static class BulkSink
{
  /// <summary>
  /// Bulk-insert a stream into the target table one batch at a time inside a
  /// single transaction. Does not modify or remove existing data. Mirrors
  /// <see cref="BulkSave.Insert{T, TContext}"/> for the streaming path.
  /// </summary>
  /// <typeparam name="T">The entity type.</typeparam>
  /// <typeparam name="TContext">The DbContext type.</typeparam>
  /// <param name="contextFactory">
  /// EF Core context factory; a fresh context is created when the sink opens and
  /// disposed when it is disposed — the concurrent-pipeline pattern.
  /// </param>
  /// <param name="options">Optional bulk operation configuration.</param>
  /// <returns>An <see cref="IFlowSink{T}"/> to pass to <see cref="FlowSourceCompiler{A}.Into"/>.</returns>
  public static IFlowSink<T> Insert<T, TContext>(
    IDbContextFactory<TContext> contextFactory,
    BulkSaveOptions? options = null
  )
    where T : class
    where TContext : DbContext
  {
    ArgumentNullException.ThrowIfNull(contextFactory);
    return new EFCoreBulkSink<T, TContext>(() => contextFactory.CreateDbContext(), options);
  }

  /// <summary>
  /// Bulk-insert a stream into the target table using a typed factory delegate.
  /// Useful when constructing contexts manually or in tests.
  /// </summary>
  /// <typeparam name="T">The entity type.</typeparam>
  /// <typeparam name="TContext">The DbContext type.</typeparam>
  /// <param name="contextFactory">Delegate producing a fresh DbContext when the sink opens.</param>
  /// <param name="options">Optional bulk operation configuration.</param>
  /// <returns>An <see cref="IFlowSink{T}"/> to pass to <see cref="FlowSourceCompiler{A}.Into"/>.</returns>
  public static IFlowSink<T> Insert<T, TContext>(
    Func<TContext> contextFactory,
    BulkSaveOptions? options = null
  )
    where T : class
    where TContext : DbContext
  {
    ArgumentNullException.ThrowIfNull(contextFactory);
    return new EFCoreBulkSink<T, TContext>(contextFactory, options);
  }
}
