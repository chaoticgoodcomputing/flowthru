using Flowthru.Data.Storage;
using Flowthru.Data.Storage.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Catalog;

/// <summary>
/// EF Core item-builder extensions on <see cref="ItemAnchor{T}"/>.
/// EFCore's loose <c>where T : class</c> constraint can't disambiguate
/// singleton vs collection via the receiver type alone (interfaces
/// satisfy <c>class</c>), so we expose three differently-named
/// extensions matching the SQL semantics:
/// <list type="bullet">
///   <item><c>.EFCoreEntity&lt;TContext&gt;()</c> — single-row table.</item>
///   <item><c>.EFCoreTable&lt;TContext&gt;()</c> — multi-row table; eager.</item>
///   <item><c>.EFCoreQuery&lt;TContext&gt;()</c> — multi-row table; deferred (returns a <see cref="DbQuery{T}"/> handle).</item>
/// </list>
/// </summary>
public static class EFCoreExtensions
{
  /// <summary>Build a single-row EF Core entity catalog item.</summary>
  public static EFCoreEntityBuilder<T, TContext> EFCoreEntity<T, TContext>(
    this ItemAnchor<T> anchor
  )
    where T : class
    where TContext : DbContext =>
    new(anchor.Label);

  /// <summary>Build a multi-row EF Core table catalog item (eager).</summary>
  public static EFCoreTableBuilder<TRow, TContext> EFCoreTable<TRow, TContext>(
    this ItemAnchor<IEnumerable<TRow>> anchor
  )
    where TRow : class
    where TContext : DbContext =>
    new(anchor.Label);

  /// <summary>
  /// Build a deferred multi-row EF Core query catalog item — the
  /// step receives a <see cref="DbQuery{T}"/> handle and decides
  /// when to materialise.
  /// </summary>
  public static EFCoreQueryBuilder<TRow, TContext> EFCoreQuery<TRow, TContext>(
    this ItemAnchor<IEnumerable<TRow>> anchor
  )
    where TRow : class
    where TContext : DbContext =>
    new(anchor.Label);
}

/// <summary>Tier-1 builder for a single-row EF Core entity catalog item.</summary>
public sealed class EFCoreEntityBuilder<T, TContext>
  where T : class
  where TContext : DbContext
{
  private readonly string _label;
  private Func<DbContext>? _contextProvider;
  private DbContext? _injectedContext;
  private bool _allowEmptyData;
  private Func<IQueryable<T>, IQueryable<T>>? _queryCustomizer;
  private Func<TContext, T, CancellationToken, Task>? _saveFunc;

  internal EFCoreEntityBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Use an <see cref="IDbContextFactory{TContext}"/> for per-operation context isolation.</summary>
  public EFCoreEntityBuilder<T, TContext> WithContextFactory(IDbContextFactory<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory.CreateDbContext();
    return this;
  }

  /// <summary>Use a typed factory delegate. Useful in tests.</summary>
  public EFCoreEntityBuilder<T, TContext> WithContextFactory(Func<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory();
    return this;
  }

  /// <summary>Inject an externally-owned <see cref="DbContext"/> (caller manages lifetime).</summary>
  public EFCoreEntityBuilder<T, TContext> WithContext(DbContext context)
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    return this;
  }

  /// <summary>Allow the table to be empty during pre-flight inspection.</summary>
  public EFCoreEntityBuilder<T, TContext> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Optional query transformation (Include / Where / OrderBy / AsNoTracking).</summary>
  public EFCoreEntityBuilder<T, TContext> WithQuery(Func<IQueryable<T>, IQueryable<T>> queryCustomizer)
  {
    _queryCustomizer = queryCustomizer ?? throw new ArgumentNullException(nameof(queryCustomizer));
    return this;
  }

  /// <summary>
  /// Optional save delegate. Defaults to
  /// <c>RemoveRange(existing) + Add(new) + SaveChanges</c> (single-row replace).
  /// </summary>
  public EFCoreEntityBuilder<T, TContext> WithSave(Func<TContext, T, CancellationToken, Task> saveFunc)
  {
    _saveFunc = saveFunc ?? throw new ArgumentNullException(nameof(saveFunc));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<T> Build()
  {
    Func<DbContext, T, CancellationToken, Task>? baseSaveFunc =
      _saveFunc is not null ? (db, data, ct) => _saveFunc((TContext)db, data, ct) : null;

    EFCoreSingleStorageAdapter<T> adapter;
    if (_injectedContext is not null)
    {
      adapter = new EFCoreSingleStorageAdapter<T>(
        _injectedContext, ownsContext: false, _allowEmptyData, _queryCustomizer, baseSaveFunc
      );
    }
    else if (_contextProvider is not null)
    {
      adapter = new EFCoreSingleStorageAdapter<T>(
        _contextProvider, _allowEmptyData, _queryCustomizer, baseSaveFunc
      );
    }
    else
    {
      throw new InvalidOperationException(
        $"EFCoreEntity item '{_label}' requires WithContextFactory(...) or WithContext(...) before Build()."
      );
    }

    return new Item<T>(_label, adapter);
  }
}

/// <summary>Tier-1 builder for a multi-row EF Core table catalog item (eager).</summary>
public sealed class EFCoreTableBuilder<TRow, TContext>
  where TRow : class
  where TContext : DbContext
{
  private readonly string _label;
  private Func<DbContext>? _contextProvider;
  private DbContext? _injectedContext;
  private bool _allowEmptyData;
  private Func<IQueryable<TRow>, IQueryable<TRow>>? _queryCustomizer;
  private Func<TContext, IEnumerable<TRow>, CancellationToken, Task>? _saveFunc;

  internal EFCoreTableBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Use an <see cref="IDbContextFactory{TContext}"/> for per-operation context isolation.</summary>
  public EFCoreTableBuilder<TRow, TContext> WithContextFactory(IDbContextFactory<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory.CreateDbContext();
    return this;
  }

  /// <summary>Use a typed factory delegate. Useful in tests.</summary>
  public EFCoreTableBuilder<TRow, TContext> WithContextFactory(Func<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory();
    return this;
  }

  /// <summary>Inject an externally-owned <see cref="DbContext"/> (caller manages lifetime).</summary>
  public EFCoreTableBuilder<TRow, TContext> WithContext(DbContext context)
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    return this;
  }

  /// <summary>Allow the table to be empty during pre-flight inspection.</summary>
  public EFCoreTableBuilder<TRow, TContext> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Optional query transformation.</summary>
  public EFCoreTableBuilder<TRow, TContext> WithQuery(Func<IQueryable<TRow>, IQueryable<TRow>> queryCustomizer)
  {
    _queryCustomizer = queryCustomizer ?? throw new ArgumentNullException(nameof(queryCustomizer));
    return this;
  }

  /// <summary>
  /// Optional save delegate. Defaults to
  /// <c>RemoveRange(existing) + AddRange(new) + SaveChanges</c>.
  /// </summary>
  public EFCoreTableBuilder<TRow, TContext> WithSave(
    Func<TContext, IEnumerable<TRow>, CancellationToken, Task> saveFunc
  )
  {
    _saveFunc = saveFunc ?? throw new ArgumentNullException(nameof(saveFunc));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    Func<DbContext, IEnumerable<TRow>, CancellationToken, Task>? baseSaveFunc =
      _saveFunc is not null ? (db, data, ct) => _saveFunc((TContext)db, data, ct) : null;

    EFCoreStorageAdapter<TRow> adapter;
    if (_injectedContext is not null)
    {
      adapter = new EFCoreStorageAdapter<TRow>(
        _injectedContext, _allowEmptyData, _queryCustomizer, baseSaveFunc
      );
    }
    else if (_contextProvider is not null)
    {
      adapter = new EFCoreStorageAdapter<TRow>(
        _contextProvider, _allowEmptyData, _queryCustomizer, baseSaveFunc
      );
    }
    else
    {
      throw new InvalidOperationException(
        $"EFCoreTable item '{_label}' requires WithContextFactory(...) or WithContext(...) before Build()."
      );
    }

    return new Item<IEnumerable<TRow>>(_label, adapter);
  }
}

/// <summary>Tier-1 builder for a deferred EF Core query catalog item.</summary>
public sealed class EFCoreQueryBuilder<TRow, TContext>
  where TRow : class
  where TContext : DbContext
{
  private readonly string _label;
  private Func<DbContext>? _contextProvider;
  private DbContext? _injectedContext;
  private bool _allowEmptyData;
  private Func<IQueryable<TRow>, IQueryable<TRow>>? _queryCustomizer;
  private Func<TContext, IEnumerable<TRow>, CancellationToken, Task>? _saveFunc;
  private DbScope? _scope;

  internal EFCoreQueryBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Use an <see cref="IDbContextFactory{TContext}"/> for per-operation context isolation.</summary>
  public EFCoreQueryBuilder<TRow, TContext> WithContextFactory(IDbContextFactory<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory.CreateDbContext();
    _scope ??= DbScope.Inferred(contextFactory);
    return this;
  }

  /// <summary>Use a typed factory delegate. Useful in tests.</summary>
  public EFCoreQueryBuilder<TRow, TContext> WithContextFactory(Func<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory();
    return this;
  }

  /// <summary>Inject an externally-owned <see cref="DbContext"/> (caller manages lifetime).</summary>
  public EFCoreQueryBuilder<TRow, TContext> WithContext(DbContext context)
  {
    _injectedContext = context ?? throw new ArgumentNullException(nameof(context));
    return this;
  }

  /// <summary>Allow the table to be empty during pre-flight inspection.</summary>
  public EFCoreQueryBuilder<TRow, TContext> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Optional query transformation.</summary>
  public EFCoreQueryBuilder<TRow, TContext> WithQuery(Func<IQueryable<TRow>, IQueryable<TRow>> queryCustomizer)
  {
    _queryCustomizer = queryCustomizer ?? throw new ArgumentNullException(nameof(queryCustomizer));
    return this;
  }

  /// <summary>Optional save delegate.</summary>
  public EFCoreQueryBuilder<TRow, TContext> WithSave(
    Func<TContext, IEnumerable<TRow>, CancellationToken, Task> saveFunc
  )
  {
    _saveFunc = saveFunc ?? throw new ArgumentNullException(nameof(saveFunc));
    return this;
  }

  /// <summary>
  /// Optional <see cref="DbScope"/> for cross-context query routing.
  /// When omitted, inferred from the context factory.
  /// </summary>
  public EFCoreQueryBuilder<TRow, TContext> WithScope(DbScope scope)
  {
    _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  public IItem<IEnumerable<TRow>> Build()
  {
    Func<DbContext, IEnumerable<TRow>, CancellationToken, Task>? baseSaveFunc =
      _saveFunc is not null ? (db, data, ct) => _saveFunc((TContext)db, data, ct) : null;

    EFCoreQueryStorageAdapter<TRow> adapter;
    if (_injectedContext is not null)
    {
      adapter = new EFCoreQueryStorageAdapter<TRow>(
        _injectedContext, _allowEmptyData, _queryCustomizer, baseSaveFunc, _scope
      );
    }
    else if (_contextProvider is not null)
    {
      adapter = new EFCoreQueryStorageAdapter<TRow>(
        _contextProvider, _allowEmptyData, _queryCustomizer, baseSaveFunc, _scope
      );
    }
    else
    {
      throw new InvalidOperationException(
        $"EFCoreQuery item '{_label}' requires WithContextFactory(...) or WithContext(...) before Build()."
      );
    }

    return new Item<IEnumerable<TRow>>(_label, adapter);
  }
}
