using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowthru.Data.Storage.EFCore.Npgsql.Internal;

/// <summary>
/// The streaming-rung sink behind an Npgsql adapter's
/// <see cref="ISupportsStreamingSink{TRow}"/> capability: batches land via
/// plain EF Core (<c>AddRange</c> + <c>SaveChangesAsync</c>, change
/// tracker cleared per batch so memory stays O(batch)) inside a single
/// transaction. Mirrors the <c>EFCoreBulkSink</c> transaction discipline —
/// commit only in <see cref="CompleteAsync"/>, roll back on disposal
/// without completion — while deliberately avoiding
/// <c>EFCore.BulkExtensions</c> (its dual license is tracked in #129).
/// The Replace import mode empties the table inside the same transaction
/// before the first batch, so replace-vs-append semantics are identical
/// across the native and streaming rungs.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
internal sealed class NpgsqlStreamingSink<T> : IFlowSink<T>
  where T : class
{
  private readonly Func<DbContext> _contextFactory;
  private readonly NpgsqlBulkImportMode _importMode;
  private readonly string _truncateSql;

  private DbContext? _context;
  private IDbContextTransaction? _transaction;
  private bool _completed;

  internal NpgsqlStreamingSink(
    Func<DbContext> contextFactory,
    NpgsqlBulkImportMode importMode,
    string truncateSql,
    int batchSize
  )
  {
    _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    _importMode = importMode;
    _truncateSql = truncateSql ?? throw new ArgumentNullException(nameof(truncateSql));
    BatchSize = Math.Max(1, batchSize);
  }

  /// <inheritdoc/>
  public int BatchSize { get; }

  /// <inheritdoc/>
  public async ValueTask OpenAsync(CancellationToken cancellationToken)
  {
    if (_context is not null)
    {
      throw new InvalidOperationException("NpgsqlStreamingSink is already open.");
    }

    _context = _contextFactory();
    _transaction = await _context
      .Database.BeginTransactionAsync(cancellationToken)
      .ConfigureAwait(false);

    if (_importMode == NpgsqlBulkImportMode.Replace)
    {
      // Same TRUNCATE, same in-transaction placement as the native
      // rung's Replace path — a failed stream rolls the old rows back.
#pragma warning disable EF1002 // SQL is built from EF-model-resolved, quoted identifiers — no user input.
      await _context.Database
        .ExecuteSqlRawAsync(_truncateSql, cancellationToken)
        .ConfigureAwait(false);
#pragma warning restore EF1002
    }
  }

  /// <inheritdoc/>
  public async ValueTask WriteBatchAsync(IReadOnlyList<T> batch, CancellationToken cancellationToken)
  {
    if (_context is null)
    {
      throw new InvalidOperationException("NpgsqlStreamingSink.WriteBatchAsync called before OpenAsync.");
    }

    _context.Set<T>().AddRange(batch);
    await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    // Detach the saved entities so tracked state doesn't grow O(dataset).
    _context.ChangeTracker.Clear();
  }

  /// <inheritdoc/>
  public async ValueTask CompleteAsync(CancellationToken cancellationToken)
  {
    if (_transaction is null)
    {
      throw new InvalidOperationException("NpgsqlStreamingSink.CompleteAsync called before OpenAsync.");
    }

    await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    _completed = true;
  }

  /// <inheritdoc/>
  public async ValueTask DisposeAsync()
  {
    if (_transaction is not null)
    {
      if (!_completed)
      {
        // Uncancellable rollback: disposal often runs because the
        // ambient token fired, and the rollback must still happen.
        try { await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
        catch { /* the provider may have already aborted the transaction */ }
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
