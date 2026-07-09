using System.Runtime.CompilerServices;
using Flowthru.Data.Storage.EFCore.Npgsql.Internal;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Flowthru.Data.Storage.EFCore.Npgsql;

/// <summary>
/// An EF Core storage adapter for a PostgreSQL table that additionally
/// speaks Flowthru's bulk-transfer capabilities. Reads, writes, and
/// inspection behave exactly like the base EF Core item; on top of that,
/// two of these items paired in <c>AddBulkTransfer</c> negotiate the
/// <em>native</em> rung — a raw binary <c>COPY</c> byte passthrough
/// (Postgres to Postgres, no CLR rows) — and a mixed pairing still gets
/// the streaming rung, because this adapter can both stream its rows out
/// and receive rows through a transactional batch sink.
/// </summary>
/// <typeparam name="T">Entity type — must be a class configured in the supplied <see cref="DbContext"/>.</typeparam>
/// <remarks>
/// <para>
/// <strong>PostgreSQL only, checked up front.</strong> Construction
/// fails immediately when the context's provider is not Npgsql — a
/// non-PostgreSQL context cannot honour the
/// <c>postgresql/pgcopy-binary</c> pairing this adapter claims, and a
/// dishonest claim would surface as a runtime COPY failure instead of a
/// wire-up error. The probe is zero-I/O (no connection is opened).
/// </para>
/// <para>
/// <strong>Load semantics are explicit.</strong> Raw <c>COPY FROM</c>
/// appends by nature; this adapter defaults to
/// <see cref="NpgsqlBulkImportMode.Replace"/> (transactional
/// <c>TRUNCATE</c> + load) because a bulk transfer's motivating use is
/// promotion, and replace matches the EF Core item's default save
/// semantics. Pass <see cref="NpgsqlBulkImportMode.Append"/> to keep
/// existing rows. Either way the whole import — including the truncate —
/// runs in one transaction: a failed transfer rolls the target back to
/// exactly its prior state, keeping the inherited
/// <c>IsTransactional = true</c> trait honest.
/// </para>
/// <para>
/// <strong>Pairing requirements.</strong> The COPY column list is
/// resolved from the EF model (physical names, model property order), so
/// source and target must map the entity to the same column set with the
/// same PostgreSQL types. Mismatches fail the transfer at runtime — and
/// roll back — rather than corrupting data. The eager <c>Save</c> path is
/// unaffected by the import mode; it keeps the base adapter's semantics
/// (default replace, or the supplied <c>saveFunc</c>).
/// </para>
/// </remarks>
public sealed class NpgsqlCopyStorageAdapter<T>
  : IStorageAdapter<IEnumerable<T>>,
    IHasEfficientCount,
    IHasServiceDependencies,
    ISupportsBulkExport,
    ISupportsBulkImport,
    ISupportsStreamingView<T>,
    ISupportsStreamingSink<T>
  where T : class
{
  /// <summary>The provider identity this adapter pairs on: <c>"postgresql"</c>.</summary>
  public const string PostgresProvider = "postgresql";

  /// <summary>The wire-format identity this adapter pairs on: <c>"pgcopy-binary"</c>.</summary>
  public const string PgCopyBinaryWireFormat = "pgcopy-binary";

  private readonly EFCoreStorageAdapter<T> _inner;
  private readonly Func<DbContext> _contextFactory;
  private readonly NpgsqlBulkImportMode _importMode;
  private readonly Func<IQueryable<T>, IQueryable<T>>? _queryCustomizer;
  private readonly int _streamingBatchSize;
  private readonly NpgsqlCopyTarget _copyTarget;

  /// <summary>
  /// Adapter over a context factory (a fresh context per operation — the
  /// COPY channels each own a dedicated connection for the duration of a
  /// transfer, so factory mode is required).
  /// </summary>
  /// <param name="contextFactory">Factory producing an Npgsql-backed DbContext per operation.</param>
  /// <param name="importMode">
  /// What a bulk transfer does to existing target rows; default
  /// <see cref="NpgsqlBulkImportMode.Replace"/>. See the type remarks.
  /// </param>
  /// <param name="allowEmptyData">If <c>true</c>, an empty table passes pre-flight inspection.</param>
  /// <param name="queryCustomizer">Optional query transformation applied to reads (eager and streaming).</param>
  /// <param name="saveFunc">Optional eager-save delegate (defaults to the base adapter's replace).</param>
  /// <param name="streamingBatchSize">Rows per batch on the streaming rung's sink (default 2000).</param>
  /// <exception cref="InvalidOperationException">
  /// The context's provider is not Npgsql, the entity is not configured,
  /// or the entity is not mapped to a physical table.
  /// </exception>
  public NpgsqlCopyStorageAdapter(
    Func<DbContext> contextFactory,
    NpgsqlBulkImportMode importMode = NpgsqlBulkImportMode.Replace,
    bool allowEmptyData = false,
    Func<IQueryable<T>, IQueryable<T>>? queryCustomizer = null,
    Func<DbContext, IEnumerable<T>, CancellationToken, Task>? saveFunc = null,
    int streamingBatchSize = 2000
  )
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _inner = new EFCoreStorageAdapter<T>(contextFactory, allowEmptyData, queryCustomizer, saveFunc);
    _importMode = importMode;
    _queryCustomizer = queryCustomizer;
    _streamingBatchSize = streamingBatchSize;

    // Feature-detect the provider and resolve the physical COPY target
    // from the EF model. Both are metadata-only — no connection opens —
    // so a mis-wired adapter fails at construction, not at first
    // transfer.
    using var context = contextFactory();
    EnsureNpgsqlProvider(context);
    _copyTarget = NpgsqlCopyTarget.Resolve(context, typeof(T));
  }

  /// <summary>The wrapped EF Core adapter — exposed for advanced consumers and tests.</summary>
  public EFCoreStorageAdapter<T> Inner => _inner;

  /// <summary>The resolved COPY target (table, columns, statements) — exposed for tests.</summary>
  internal NpgsqlCopyTarget CopyTarget => _copyTarget;

  // ── IStorageAdapter (forwarded to the base EF Core adapter) ───────────

  /// <inheritdoc/>
  public StorageTraits Traits => _inner.Traits;

  /// <inheritdoc/>
  public IReadOnlyList<ServiceDependency> ServiceDependencies =>
    ((IHasServiceDependencies)_inner).ServiceDependencies;

  /// <inheritdoc/>
  public FlowIO<IEnumerable<T>> Load() => _inner.Load();

  /// <inheritdoc/>
  public FlowIO<FlowUnit> Save(IEnumerable<T> data) => _inner.Save(data);

  /// <inheritdoc/>
  public FlowIO<bool> Exists() => _inner.Exists();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectShallow(int sampleSize) => _inner.InspectShallow(sampleSize);

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();

  /// <inheritdoc/>
  public FlowIO<ValidationResult> InspectTarget() => _inner.InspectTarget();

  /// <inheritdoc/>
  FlowIO<int> IHasEfficientCount.GetCountAsync() => ((IHasEfficientCount)_inner).GetCountAsync();

  // ── Bulk-transfer pairing identity (zero-I/O metadata) ────────────────

  /// <inheritdoc/>
  public string BulkProvider => PostgresProvider;

  /// <inheritdoc/>
  public string BulkWireFormat => PgCopyBinaryWireFormat;

  // ── Native rung channels ──────────────────────────────────────────────

  /// <inheritdoc/>
  /// <remarks>
  /// Opens <c>COPY &lt;table&gt; (&lt;columns&gt;) TO STDOUT (FORMAT BINARY)</c>
  /// on a dedicated connection. The returned stream owns the connection
  /// and cancels the COPY if disposed before end-of-stream.
  /// </remarks>
  public FlowIO<Stream> OpenBulkExport() =>
    FlowIO.LiftAsync<Stream>(async ct =>
    {
      var context = _contextFactory();
      try
      {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        var copy = await connection
          .BeginRawBinaryCopyAsync(_copyTarget.ExportSql, ct)
          .ConfigureAwait(false);
        return new NpgsqlRawCopyExportStream(copy, context);
      }
      catch
      {
        await context.DisposeAsync().ConfigureAwait(false);
        throw;
      }
    }, source: $"NpgsqlCopyStorageAdapter.OpenBulkExport[{typeof(T).Name}]");

  /// <inheritdoc/>
  /// <remarks>
  /// Opens a dedicated connection, begins a transaction, applies the
  /// import mode (<see cref="NpgsqlBulkImportMode.Replace"/> truncates
  /// inside the transaction), and starts
  /// <c>COPY &lt;table&gt; (&lt;columns&gt;) FROM STDIN (FORMAT BINARY)</c>.
  /// The returned channel commits only on
  /// <see cref="BulkImportChannel.CompleteAsync"/>; disposal without
  /// completion cancels the COPY and rolls everything back.
  /// </remarks>
  public FlowIO<BulkImportChannel> OpenBulkImport() =>
    FlowIO.LiftAsync<BulkImportChannel>(async ct =>
    {
      var context = _contextFactory();
      NpgsqlTransaction? transaction = null;
      try
      {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync(ct).ConfigureAwait(false);
        transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        if (_importMode == NpgsqlBulkImportMode.Replace)
        {
          var truncate = new NpgsqlCommand(_copyTarget.TruncateSql, connection, transaction);
          await using (truncate.ConfigureAwait(false))
          {
            await truncate.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
          }
        }

        var copy = await connection
          .BeginRawBinaryCopyAsync(_copyTarget.ImportSql, ct)
          .ConfigureAwait(false);
        return new NpgsqlRawCopyImportChannel(copy, transaction, context);
      }
      catch
      {
        if (transaction is not null)
        {
          try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
          catch { /* connection teardown below aborts server-side */ }
          try { await transaction.DisposeAsync().ConfigureAwait(false); }
          catch { /* best-effort */ }
        }
        await context.DisposeAsync().ConfigureAwait(false);
        throw;
      }
    }, source: $"NpgsqlCopyStorageAdapter.OpenBulkImport[{typeof(T).Name}]");

  // ── Streaming rung capabilities ───────────────────────────────────────

  /// <inheritdoc/>
  public bool SupportsStreaming => true;

  /// <inheritdoc/>
  /// <remarks>
  /// Streams rows through EF Core's async cursor
  /// (<c>AsNoTracking().AsAsyncEnumerable()</c>) on a per-open context —
  /// O(batch) memory, no whole-table materialisation.
  /// </remarks>
  public FlowSource<T> OpenStreamingSource() => FlowSource.Lift<T>(StreamRows);

  /// <inheritdoc/>
  public IFlowSink<T> OpenStreamingSink() =>
    new NpgsqlStreamingSink<T>(_contextFactory, _importMode, _copyTarget.TruncateSql, _streamingBatchSize);

  // ── Internals ─────────────────────────────────────────────────────────

  private async IAsyncEnumerable<T> StreamRows([EnumeratorCancellation] CancellationToken ct)
  {
    var context = _contextFactory();
    await using var _ = context.ConfigureAwait(false);

    IQueryable<T> query = context.Set<T>().AsNoTracking();
    if (_queryCustomizer is not null) query = _queryCustomizer(query);

    await foreach (var row in query.AsAsyncEnumerable().WithCancellation(ct).ConfigureAwait(false))
    {
      yield return row;
    }
  }

  /// <summary>
  /// Fail construction when the context is not Npgsql-backed. The probe
  /// type-tests the provider-built connection object — created but never
  /// opened, so the check is zero-I/O.
  /// </summary>
  private static void EnsureNpgsqlProvider(DbContext context)
  {
    string providerName;
    try { providerName = context.Database.ProviderName ?? "unknown"; }
    catch { providerName = "unknown"; }

    System.Data.Common.DbConnection connection;
    try
    {
      connection = context.Database.GetDbConnection();
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException(
        $"NpgsqlCopyStorageAdapter<{typeof(T).Name}> requires a relational, Npgsql-backed "
        + $"DbContext, but '{context.GetType().Name}' (provider '{providerName}') exposes no "
        + "relational connection.", ex);
    }

    if (connection is not NpgsqlConnection)
    {
      throw new InvalidOperationException(
        $"NpgsqlCopyStorageAdapter<{typeof(T).Name}> requires a DbContext backed by the Npgsql "
        + $"PostgreSQL provider, but '{context.GetType().Name}' uses '{providerName}' "
        + $"(connection type '{connection.GetType().Name}'). Only PostgreSQL can honour the "
        + $"'{PostgresProvider}/{PgCopyBinaryWireFormat}' bulk-transfer pairing this adapter "
        + "claims — use the base EFCore item for other providers.");
    }
  }
}
