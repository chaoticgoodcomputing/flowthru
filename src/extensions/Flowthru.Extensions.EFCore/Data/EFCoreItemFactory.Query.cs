using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Extensions.EFCore.Data;

public static partial class EFCoreItemFactory
{
  /// <summary>
  /// Factory methods for <c>IItem&lt;IEnumerable&lt;T&gt;&gt;</c> entries backed by a deferred
  /// <see cref="DbQuery{T}"/> handle.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Entries created by this factory return a <see cref="DbQuery{T}"/> when loaded — no rows are
  /// fetched from the database until a step iterates the value or calls
  /// <see cref="DbQuery{T}.ToListAsync"/>. This makes the entries behaviorally lazy: pre-flight
  /// only probes table existence; step bodies execute the query on demand.
  /// </para>
  /// <para>
  /// The outer catalog type is <c>IItem&lt;IEnumerable&lt;T&gt;&gt;</c>, identical to
  /// <see cref="Enumerable"/> entries, so changing a catalog entry from
  /// <c>EFCoreItemFactory.Enumerable.EFCore</c> to <c>EFCoreItemFactory.Query.EFCore</c>
  /// defers reads without requiring any step code changes.
  /// </para>
  /// <para>
  /// <strong>Save behaviour:</strong> Steps that return a <see cref="DbQuery{T}"/> to a query
  /// catalog entry trigger a server-side fused INSERT-FROM-SELECT when source and destination
  /// share the same <see cref="DbScope"/>. Steps that return a plain <c>IEnumerable&lt;T&gt;</c>
  /// (e.g. preprocessing steps) use the standard RemoveRange + AddRange path.
  /// </para>
  /// <para>
  /// Compare with <see cref="Enumerable"/>: those factories eagerly materialise the full dataset
  /// inside the catalog layer. Use <c>Query</c> factory entries when the dataset is large and
  /// step-level filtering should avoid pulling unnecessary rows, or when the general principle
  /// of pushing the materialisation decision to the step is preferred.
  /// </para>
  /// </remarks>
  public static class Query
  {
    // ── Injected DbContext ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a deferred EFCore catalog entry using an injected <see cref="DbContext"/>.
    /// The caller owns the context lifecycle.
    /// </summary>
    /// <typeparam name="T">Entity type (must be a class configured in the DbContext).</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution.</param>
    /// <param name="context">DbContext instance; caller manages lifecycle.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty table passes validation.</param>
    /// <param name="queryCustomizer">Optional query transformation applied before the handle is returned.</param>
    /// <param name="saveFunc">Optional save delegate. Defaults to RemoveRange + AddRange when <see langword="null"/>.</param>
    /// <param name="scope">
    /// Database scope used for the fused save path.
    /// Defaults to <see cref="DbScope.Inferred"/> keyed on <paramref name="context"/>.
    /// </param>
    public static Item<IEnumerable<T>> EFCore<T>(
      string label,
      DbContext context,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
      DbScope? scope = null
    )
      where T : class
    {
      var storage = new DbQueryStorageAdapter<T>(
        context,
        allowEmptyData,
        queryCustomizer,
        saveFunc,
        scope
      );
      return new Item<IEnumerable<T>>(label, storage);
    }

    // ── Untyped factory ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates a deferred EFCore catalog entry using an untyped <see cref="DbContext"/> factory.
    /// A fresh context is created and disposed per operation.
    /// </summary>
    /// <typeparam name="T">Entity type (must be a class configured in the DbContext).</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution.</param>
    /// <param name="contextFactory">Factory that creates a new <see cref="DbContext"/> per operation.</param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty table passes validation.</param>
    /// <param name="queryCustomizer">Optional query transformation applied before the handle is returned.</param>
    /// <param name="saveFunc">Optional save delegate. Defaults to RemoveRange + AddRange when <see langword="null"/>.</param>
    /// <param name="scope">
    /// Database scope used for the fused save path.
    /// Defaults to <see cref="DbScope.Inferred"/> keyed on <paramref name="contextFactory"/>.
    /// </param>
    public static Item<IEnumerable<T>> EFCore<T>(
      string label,
      Func<DbContext> contextFactory,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
      DbScope? scope = null
    )
      where T : class
    {
      var storage = new DbQueryStorageAdapter<T>(
        contextFactory,
        allowEmptyData,
        queryCustomizer,
        saveFunc,
        scope
      );
      return new Item<IEnumerable<T>>(label, storage);
    }

    // ── IDbContextFactory<TContext> ───────────────────────────────────────────

    /// <summary>
    /// Creates a deferred EFCore catalog entry using <see cref="IDbContextFactory{TContext}"/> —
    /// the idiomatic EFCore pattern for per-operation context isolation and concurrent step safety.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TContext">Concrete DbContext type.</typeparam>
    /// <param name="label">Unique catalog label for DAG resolution.</param>
    /// <param name="contextFactory">
    /// EFCore context factory; a fresh context is created per Load/Save operation.
    /// </param>
    /// <param name="allowEmptyData">If <c>true</c>, an empty table passes validation.</param>
    /// <param name="queryCustomizer">Optional query transformation applied before the handle is returned.</param>
    /// <param name="saveFunc">
    /// Optional save delegate receiving the concrete <typeparamref name="TContext"/>.
    /// Defaults to RemoveRange + AddRange when <see langword="null"/>.
    /// </param>
    /// <param name="scope">
    /// Database scope used for the fused save path.
    /// Defaults to <see cref="DbScope.Inferred"/> keyed on <paramref name="contextFactory"/>.
    /// </param>
    public static Item<IEnumerable<T>> EFCore<T, TContext>(
      string label,
      IDbContextFactory<TContext> contextFactory,
      bool allowEmptyData = false,
      Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
      Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
      DbScope? scope = null
    )
      where T : class
      where TContext : DbContext
    {
      Func<DbContext> baseFactory = () => contextFactory.CreateDbContext();
      Func<DbContext, IEnumerable<T>, CancellationToken, Task>? baseSaveFunc =
        saveFunc != null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

      var storage = new DbQueryStorageAdapter<T>(
        baseFactory,
        allowEmptyData,
        queryCustomizer,
        baseSaveFunc,
        scope ?? DbScope.Inferred(contextFactory)
      );
      return new Item<IEnumerable<T>>(label, storage);
    }
  }
}
