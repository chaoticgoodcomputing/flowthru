using EFCore.BulkExtensions;
using Flowthru.Extensions.EFCore.Bulk.Internal;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowthru.Extensions.EFCore.Bulk;

/// <summary>
/// A streaming <see cref="IFlowSink{T}"/> that bulk-inserts a
/// <see cref="FlowSource{A}"/> into an EF Core table <em>one batch at a time,
/// inside a single transaction</em>. Driven by
/// <see cref="FlowSourceCompiler{A}.Into"/>: a context + transaction open once
/// (<see cref="OpenAsync"/>), each arriving batch issues its own
/// <c>BulkInsertAsync</c> enlisted in that transaction
/// (<see cref="WriteBatchAsync"/>), and the transaction commits on success
/// (<see cref="CompleteAsync"/>).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why per-batch.</strong> The eager <see cref="BulkSave"/> path makes a
/// single <c>BulkInsertAsync</c> over the whole dataset; handing that call a lazy
/// <see cref="IEnumerable{T}"/> re-materialises it internally (O(dataset)),
/// defeating the memory win. This sink instead consumes the driver's
/// <see cref="BatchSize"/>-sized batches, so peak memory is O(batch) end-to-end.
/// </para>
/// <para>
/// <strong>Why one transaction.</strong> <c>EFCore.BulkExtensions</c> enlists in
/// the context's ambient transaction, so every batch lands in the transaction
/// opened at <see cref="OpenAsync"/>. If the stream fails partway,
/// <see cref="DisposeAsync"/> runs without <see cref="CompleteAsync"/> having been
/// reached and rolls the whole write back — no corrupt-but-present table — keeping
/// the adapter's <c>IsTransactional</c> claim honest.
/// </para>
/// <para>
/// <strong>Ownership.</strong> The sink owns the context and transaction it
/// creates and disposes both on every exit path. Batches passed to
/// <see cref="WriteBatchAsync"/> are consumed synchronously within the call and
/// never retained, so the driver is free to reuse its buffer.
/// </para>
/// </remarks>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class EFCoreBulkSink<T, TContext> : IFlowSink<T>
  where T : class
  where TContext : DbContext
{
  private readonly Func<TContext> _contextFactory;
  private readonly BulkSaveOptions _options;
  private readonly BulkConfig _config;

  private TContext? _context;
  private IDbContextTransaction? _transaction;
  private bool _completed;
  private int _batchesWritten;

  /// <summary>
  /// Create a sink over a typed context factory. A fresh context is created at
  /// <see cref="OpenAsync"/> and disposed at <see cref="DisposeAsync"/>.
  /// </summary>
  /// <param name="contextFactory">Factory producing a DbContext for the write.</param>
  /// <param name="options">Optional bulk operation configuration.</param>
  public EFCoreBulkSink(Func<TContext> contextFactory, BulkSaveOptions? options = null)
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _options = options ?? new BulkSaveOptions();
    _config = BulkConfigMapper.ToBulkConfig(_options);
  }

  /// <inheritdoc/>
  /// <remarks>Sourced from <see cref="BulkSaveOptions.BatchSize"/> (default 2000).</remarks>
  public int BatchSize => _options.BatchSize;

  /// <summary>
  /// Number of <see cref="WriteBatchAsync"/> calls that have run — a test/
  /// observability hook proving the write is incremental (per-batch) rather than
  /// one materialised bulk call.
  /// </summary>
  internal int BatchesWritten => _batchesWritten;

  /// <inheritdoc/>
  public async ValueTask OpenAsync(CancellationToken cancellationToken)
  {
    if (_context is not null)
      throw new InvalidOperationException("EFCoreBulkSink is already open.");

    _context = _contextFactory();

    // The bulk operations below enlist in this ambient transaction; committing
    // here is the sole path that persists the write (Dispose-without-Complete
    // rolls back).
    _transaction = await _context
      .Database.BeginTransactionAsync(cancellationToken)
      .ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public async ValueTask WriteBatchAsync(
    IReadOnlyList<T> batch,
    CancellationToken cancellationToken
  )
  {
    if (_context is null)
      throw new InvalidOperationException("EFCoreBulkSink.WriteBatchAsync called before OpenAsync.");

    // The batch is valid only for this call; BulkInsertAsync consumes it
    // synchronously, so no copy is needed.
    await _context
      .BulkInsertAsync(
        batch,
        _config,
        progress: _options.OnProgress,
        cancellationToken: cancellationToken
      )
      .ConfigureAwait(false);

    _batchesWritten++;
  }

  /// <inheritdoc/>
  public async ValueTask CompleteAsync(CancellationToken cancellationToken)
  {
    if (_transaction is null)
      throw new InvalidOperationException("EFCoreBulkSink.CompleteAsync called before OpenAsync.");

    await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    _completed = true;
  }

  /// <inheritdoc/>
  public async ValueTask DisposeAsync()
  {
    if (_transaction is not null)
    {
      // Reached only when CompleteAsync did not commit — abort the whole write.
      // Roll back uncancellably: the reason we are disposing is often that the
      // ambient token was cancelled, and the rollback must still run.
      if (!_completed)
      {
        try
        {
          await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
          // Best-effort: the transaction may already be aborted by the provider.
        }
      }

      await _transaction.DisposeAsync().ConfigureAwait(false);
      _transaction = null;
    }

    if (_context is not null)
    {
      await _context.DisposeAsync().ConfigureAwait(false);
      _context = null;
    }
  }
}
