using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Extension methods that contribute EF Core smart constructors into
/// <see cref="ItemFactory.Enumerable"/> and
/// <see cref="ItemFactory.Singleton"/>. End users call them as
/// <c>ItemFactory.Enumerable.EFCore&lt;TEntity, TContext&gt;(...)</c>
/// and <c>ItemFactory.Singleton.EFCore&lt;TEntity, TContext&gt;(...)</c>
/// via a single <c>using Flowthru.Data.Catalog;</c> import.
/// </summary>
public static class EFCoreItemFactoryExtensions
{
  // ── Enumerable.EFCore<T, TContext> ───────────────────────────────────

  /// <summary>
  /// EF Core entity collection. Recommended overload — uses
  /// <see cref="IDbContextFactory{TContext}"/> for per-operation
  /// context isolation, the idiomatic pattern for concurrent pipelines.
  /// </summary>
  /// <typeparam name="T">Entity type — must be a class configured in <typeparamref name="TContext"/>.</typeparam>
  /// <typeparam name="TContext">Concrete DbContext type (flows through to the save delegate).</typeparam>
  /// <param name="factory">Factory anchor — discriminates the extension target.</param>
  /// <param name="label">Catalog label for DAG resolution.</param>
  /// <param name="contextFactory">EF Core context factory; a fresh context is created per Load/Save.</param>
  /// <param name="allowEmptyData">If <c>true</c>, an empty table passes pre-flight inspection.</param>
  /// <param name="queryCustomizer">Optional query transformation (Include / Where / OrderBy / AsNoTracking) applied before materialisation.</param>
  /// <param name="saveFunc">
  /// Optional save delegate receiving the typed context. Defaults to
  /// <c>RemoveRange + AddRange + SaveChanges</c> when null.
  /// </param>
  public static IItem<IEnumerable<T>> EFCore<T, TContext>(
    this EnumerableItemFactory factory,
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
      saveFunc is not null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

    var adapter = new EFCoreStorageAdapter<T>(
      baseFactory, allowEmptyData, queryCustomizer, baseSaveFunc
    );
    return new Item<IEnumerable<T>>(label, adapter);
  }

  /// <summary>
  /// EF Core entity collection with a typed factory delegate.
  /// Useful when constructing contexts manually or in tests.
  /// </summary>
  public static IItem<IEnumerable<T>> EFCore<T, TContext>(
    this EnumerableItemFactory factory,
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
      saveFunc is not null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

    var adapter = new EFCoreStorageAdapter<T>(
      baseFactory, allowEmptyData, queryCustomizer, baseSaveFunc
    );
    return new Item<IEnumerable<T>>(label, adapter);
  }

  /// <summary>
  /// EF Core entity collection bound to an injected DbContext.
  /// The caller owns the context's lifetime — the adapter does not
  /// dispose it. Use this when the context is shared across multiple
  /// catalog items (e.g. for cross-table transactions).
  /// </summary>
  public static IItem<IEnumerable<T>> EFCore<T>(
    this EnumerableItemFactory factory,
    string label,
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null
  )
    where T : class
  {
    var adapter = new EFCoreStorageAdapter<T>(
      context, allowEmptyData, queryCustomizer, saveFunc
    );
    return new Item<IEnumerable<T>>(label, adapter);
  }

  // ── Singleton.EFCore<T, TContext> ────────────────────────────────────

  /// <summary>
  /// Single EF Core entity (table holding exactly one row) — trained
  /// models, configuration snapshots, aggregated metrics, etc.
  /// Recommended overload using <see cref="IDbContextFactory{TContext}"/>.
  /// </summary>
  public static IItem<T> EFCore<T, TContext>(
    this SingletonItemFactory factory,
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
      saveFunc is not null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

    var adapter = new EFCoreSingleStorageAdapter<T>(
      baseFactory, allowEmptyData, queryCustomizer, baseSaveFunc
    );
    return new Item<T>(label, adapter);
  }

  /// <summary>Single EF Core entity with a typed factory delegate.</summary>
  public static IItem<T> EFCore<T, TContext>(
    this SingletonItemFactory factory,
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
      saveFunc is not null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

    var adapter = new EFCoreSingleStorageAdapter<T>(
      baseFactory, allowEmptyData, queryCustomizer, baseSaveFunc
    );
    return new Item<T>(label, adapter);
  }

  /// <summary>Single EF Core entity bound to an injected DbContext (caller-owned).</summary>
  public static IItem<T> EFCore<T>(
    this SingletonItemFactory factory,
    string label,
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, T, CancellationToken, Task>? saveFunc = null
  )
    where T : class
  {
    var adapter = new EFCoreSingleStorageAdapter<T>(
      context, ownsContext: false, allowEmptyData, queryCustomizer, saveFunc
    );
    return new Item<T>(label, adapter);
  }

  // ── Enumerable.EFCoreQuery<T, TContext> ──────────────────────────────

  /// <summary>
  /// Deferred EF Core entity collection — the catalog returns a
  /// <see cref="DbQuery{T}"/> handle (typed as
  /// <see cref="IEnumerable{T}"/>) and no rows are read from the
  /// database until a step iterates the value or calls
  /// <see cref="DbQuery{T}.ToListAsync(CancellationToken)"/>. Use when
  /// step-level filtering should avoid pulling the full table to the
  /// host, or when downstream saves should run as a fused
  /// <c>INSERT-FROM-SELECT</c>. Recommended overload — uses
  /// <see cref="IDbContextFactory{TContext}"/> for per-operation
  /// context isolation.
  /// </summary>
  public static IItem<IEnumerable<T>> EFCoreQuery<T, TContext>(
    this EnumerableItemFactory factory,
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
      saveFunc is not null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

    var adapter = new EFCoreQueryStorageAdapter<T>(
      baseFactory,
      allowEmptyData,
      queryCustomizer,
      baseSaveFunc,
      scope ?? DbScope.Inferred(contextFactory)
    );
    return new Item<IEnumerable<T>>(label, adapter);
  }

  /// <summary>
  /// Deferred EF Core entity collection with a typed factory delegate.
  /// Useful for tests and bespoke construction paths.
  /// </summary>
  public static IItem<IEnumerable<T>> EFCoreQuery<T, TContext>(
    this EnumerableItemFactory factory,
    string label,
    Func<TContext> contextFactory,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<TContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    DbScope? scope = null
  )
    where T : class
    where TContext : DbContext
  {
    Func<DbContext> baseFactory = () => contextFactory();
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? baseSaveFunc =
      saveFunc is not null ? (db, data, ct) => saveFunc((TContext)db, data, ct) : null;

    var adapter = new EFCoreQueryStorageAdapter<T>(
      baseFactory, allowEmptyData, queryCustomizer, baseSaveFunc, scope
    );
    return new Item<IEnumerable<T>>(label, adapter);
  }

  /// <summary>
  /// Deferred EF Core entity collection bound to an injected
  /// DbContext (caller-owned). Use when the DbContext is shared
  /// across multiple catalog items (e.g. cross-table transactions).
  /// </summary>
  public static IItem<IEnumerable<T>> EFCoreQuery<T>(
    this EnumerableItemFactory factory,
    string label,
    DbContext context,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    DbScope? scope = null
  )
    where T : class
  {
    var adapter = new EFCoreQueryStorageAdapter<T>(
      context, allowEmptyData, queryCustomizer, saveFunc, scope
    );
    return new Item<IEnumerable<T>>(label, adapter);
  }
}
