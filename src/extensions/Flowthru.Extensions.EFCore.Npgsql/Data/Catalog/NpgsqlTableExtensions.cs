using Flowthru.Data.Storage.EFCore.Npgsql;
using Microsoft.EntityFrameworkCore;

namespace Flowthru.Data.Catalog;

/// <summary>
/// Npgsql item-builder extension on <see cref="ItemAnchor{T}"/>:
/// <c>.NpgsqlTable&lt;TRow, TContext&gt;()</c> declares a PostgreSQL
/// table item that behaves like <c>.EFCoreTable&lt;TRow, TContext&gt;()</c>
/// everywhere — and additionally pairs natively in
/// <c>AddBulkTransfer</c>: two of these items move data as a raw binary
/// <c>COPY</c> byte passthrough instead of row-at-a-time.
/// </summary>
public static class NpgsqlTableExtensions
{
  /// <summary>
  /// Build a multi-row PostgreSQL table catalog item with native
  /// bulk-transfer capability. The context must use the Npgsql provider —
  /// any other provider fails at <c>Build()</c>, because only PostgreSQL
  /// can honour the raw-COPY pairing the item claims.
  /// </summary>
  public static NpgsqlTableBuilder<TRow, TContext> NpgsqlTable<TRow, TContext>(
    this ItemAnchor<IEnumerable<TRow>> anchor
  )
    where TRow : class
    where TContext : DbContext =>
    new(anchor.Label);
}

/// <summary>
/// Tier-1 builder for a PostgreSQL table catalog item with native
/// bulk-transfer capability. Mirrors <c>EFCoreTableBuilder</c>, minus the
/// injected-context mode: bulk transfers open dedicated connections per
/// operation, so a per-operation context factory is required.
/// </summary>
public sealed class NpgsqlTableBuilder<TRow, TContext>
  where TRow : class
  where TContext : DbContext
{
  private readonly string _label;
  private Func<DbContext>? _contextProvider;
  private bool _allowEmptyData;
  private Func<IQueryable<TRow>, IQueryable<TRow>>? _queryCustomizer;
  private Func<TContext, IEnumerable<TRow>, CancellationToken, Task>? _saveFunc;
  private NpgsqlBulkImportMode _importMode = NpgsqlBulkImportMode.Replace;
  private int _streamingBatchSize = 2000;

  internal NpgsqlTableBuilder(string label)
  {
    _label = label;
  }

  /// <summary>Use an <see cref="IDbContextFactory{TContext}"/> for per-operation context isolation.</summary>
  public NpgsqlTableBuilder<TRow, TContext> WithContextFactory(IDbContextFactory<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory.CreateDbContext();
    return this;
  }

  /// <summary>Use a typed factory delegate. Useful in tests.</summary>
  public NpgsqlTableBuilder<TRow, TContext> WithContextFactory(Func<TContext> contextFactory)
  {
    if (contextFactory is null) throw new ArgumentNullException(nameof(contextFactory));
    _contextProvider = () => contextFactory();
    return this;
  }

  /// <summary>Allow the table to be empty during pre-flight inspection.</summary>
  public NpgsqlTableBuilder<TRow, TContext> AllowEmpty()
  {
    _allowEmptyData = true;
    return this;
  }

  /// <summary>Optional query transformation applied to reads (eager and streaming).</summary>
  public NpgsqlTableBuilder<TRow, TContext> WithQuery(Func<IQueryable<TRow>, IQueryable<TRow>> queryCustomizer)
  {
    _queryCustomizer = queryCustomizer ?? throw new ArgumentNullException(nameof(queryCustomizer));
    return this;
  }

  /// <summary>
  /// Optional eager-save delegate. Defaults to
  /// <c>RemoveRange(existing) + AddRange(new) + SaveChanges</c>. Applies
  /// to ordinary step writes only — bulk transfers land through the COPY
  /// channel or the streaming sink and honour
  /// <see cref="WithImportMode"/> instead.
  /// </summary>
  public NpgsqlTableBuilder<TRow, TContext> WithSave(
    Func<TContext, IEnumerable<TRow>, CancellationToken, Task> saveFunc
  )
  {
    _saveFunc = saveFunc ?? throw new ArgumentNullException(nameof(saveFunc));
    return this;
  }

  /// <summary>
  /// What a bulk transfer does to rows already in this table:
  /// <see cref="NpgsqlBulkImportMode.Replace"/> (default — transactional
  /// <c>TRUNCATE</c> + load, the target becomes an exact copy of the
  /// source) or <see cref="NpgsqlBulkImportMode.Append"/> (keep existing
  /// rows). Applies identically to the native and streaming rungs.
  /// </summary>
  public NpgsqlTableBuilder<TRow, TContext> WithImportMode(NpgsqlBulkImportMode importMode)
  {
    _importMode = importMode;
    return this;
  }

  /// <summary>
  /// Rows per batch when this item receives a <em>streaming</em> bulk
  /// transfer (default 2000). The native COPY rung moves bytes, not
  /// rows, and ignores this.
  /// </summary>
  public NpgsqlTableBuilder<TRow, TContext> WithStreamingBatchSize(int batchSize)
  {
    if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
    _streamingBatchSize = batchSize;
    return this;
  }

  /// <summary>Materialise the <see cref="IItem{T}"/>.</summary>
  /// <exception cref="InvalidOperationException">
  /// No context factory was supplied, or the supplied context is not
  /// Npgsql-backed.
  /// </exception>
  public IItem<IEnumerable<TRow>> Build()
  {
    if (_contextProvider is null)
    {
      throw new InvalidOperationException(
        $"NpgsqlTable item '{_label}' requires WithContextFactory(...) before Build()."
      );
    }

    Func<DbContext, IEnumerable<TRow>, CancellationToken, Task>? baseSaveFunc =
      _saveFunc is not null ? (db, data, ct) => _saveFunc((TContext)db, data, ct) : null;

    var adapter = new NpgsqlCopyStorageAdapter<TRow>(
      _contextProvider,
      _importMode,
      _allowEmptyData,
      _queryCustomizer,
      baseSaveFunc,
      _streamingBatchSize
    );
    return new Item<IEnumerable<TRow>>(_label, adapter);
  }
}
