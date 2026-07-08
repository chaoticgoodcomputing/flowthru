using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Flowthru.Data.Storage.EFCore.Npgsql.Internal;

/// <summary>
/// The <see cref="BulkImportChannel"/> behind an Npgsql adapter's
/// bulk-import capability: a raw binary <c>COPY ... FROM STDIN</c> stream
/// running inside a dedicated transaction on a dedicated connection.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Commit/abort discipline.</strong> <see cref="CompleteAsync"/>
/// is the <em>only</em> code path that commits: it finishes the COPY
/// (disposing the Npgsql copy stream sends <c>CopyDone</c>, which is where
/// the server validates the payload and raises constraint or format
/// errors) and then commits the transaction. Disposal without a completed
/// <see cref="CompleteAsync"/> cancels the COPY (<c>CopyFail</c>) and
/// rolls the transaction back — uncancellably, since the abort usually
/// runs <em>because</em> the ambient token fired. Even if the explicit
/// rollback fails, closing the connection without a commit makes
/// PostgreSQL abort the transaction server-side, so no partial payload
/// (nor the Replace mode's <c>TRUNCATE</c>) can ever become visible.
/// </para>
/// <para>
/// <strong>Ownership.</strong> The channel owns the DbContext (and with
/// it the connection), the transaction, and the copy stream, and releases
/// all three on every exit path.
/// </para>
/// </remarks>
internal sealed class NpgsqlRawCopyImportChannel : BulkImportChannel
{
  private readonly NpgsqlRawCopyStream _copy;
  private readonly NpgsqlTransaction _transaction;
  private readonly DbContext _context;

  private bool _copyFinished;
  private bool _completed;
  private bool _disposed;

  internal NpgsqlRawCopyImportChannel(
    NpgsqlRawCopyStream copy,
    NpgsqlTransaction transaction,
    DbContext context
  )
  {
    _copy = copy ?? throw new ArgumentNullException(nameof(copy));
    _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    _context = context ?? throw new ArgumentNullException(nameof(context));
  }

  // ── BulkImportChannel ─────────────────────────────────────────────────

  /// <inheritdoc/>
  public override async ValueTask CompleteAsync(CancellationToken cancellationToken)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (_completed)
    {
      throw new InvalidOperationException("The import channel is already completed.");
    }

    // Finish the COPY first: Npgsql sends CopyDone on disposal, and the
    // server's verdict on the payload (constraint violations, malformed
    // binary data) surfaces here — before anything can commit.
    await _copy.DisposeAsync().ConfigureAwait(false);
    _copyFinished = true;

    await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    _completed = true;
  }

  // ── Stream (write-only) ───────────────────────────────────────────────

  public override bool CanRead => false;
  public override bool CanSeek => false;
  public override bool CanWrite => !_disposed && !_copyFinished;
  public override long Length => throw new NotSupportedException();
  public override long Position
  {
    get => throw new NotSupportedException();
    set => throw new NotSupportedException();
  }

  public override void Write(byte[] buffer, int offset, int count) =>
    _copy.Write(buffer, offset, count);

  public override void Write(ReadOnlySpan<byte> buffer) => _copy.Write(buffer);

  public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
    _copy.WriteAsync(buffer, offset, count, cancellationToken);

  public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
    _copy.WriteAsync(buffer, cancellationToken);

  public override void Flush() => _copy.Flush();

  public override Task FlushAsync(CancellationToken cancellationToken) =>
    _copy.FlushAsync(cancellationToken);

  public override int Read(byte[] buffer, int offset, int count) =>
    throw new NotSupportedException("The bulk-import channel is write-only.");

  public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

  public override void SetLength(long value) => throw new NotSupportedException();

  // ── Disposal ──────────────────────────────────────────────────────────

  public override async ValueTask DisposeAsync()
  {
    if (_disposed) return;
    _disposed = true;

    try
    {
      if (!_completed)
      {
        await AbortAsync().ConfigureAwait(false);
      }
    }
    finally
    {
      try { await _transaction.DisposeAsync().ConfigureAwait(false); }
      catch { /* connection teardown below still aborts server-side */ }
      await _context.DisposeAsync().ConfigureAwait(false);
    }

    await base.DisposeAsync().ConfigureAwait(false);
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && !_disposed)
    {
      // Route the sync path through the async teardown so both paths
      // share the same abort discipline.
      DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    base.Dispose(disposing);
  }

  /// <summary>
  /// The abort path: cancel the in-flight COPY (<c>CopyFail</c>) and roll
  /// the transaction back. Every step is best-effort and uncancellable —
  /// the last-resort guarantee is the connection closing without a
  /// commit, which aborts the transaction server-side.
  /// </summary>
  private async ValueTask AbortAsync()
  {
    if (!_copyFinished)
    {
      _copyFinished = true;
      try { await _copy.CancelAsync().ConfigureAwait(false); }
      catch { /* the operation may already be broken; rollback below still applies */ }
      try { await _copy.DisposeAsync().ConfigureAwait(false); }
      catch { /* post-cancel disposal noise */ }
    }

    try { await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
    catch { /* the provider may have already aborted the transaction */ }
  }
}
