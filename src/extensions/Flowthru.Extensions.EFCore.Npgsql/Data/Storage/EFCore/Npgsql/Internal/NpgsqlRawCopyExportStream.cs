using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Flowthru.Data.Storage.EFCore.Npgsql.Internal;

/// <summary>
/// The read side of an Npgsql adapter's bulk-export capability: a raw
/// binary <c>COPY ... TO STDOUT</c> stream on a dedicated connection.
/// Reading to end-of-stream completes the export cleanly; disposal before
/// end-of-stream is treated as an abort and cancels the in-flight COPY so
/// the connection tears down without waiting for the remaining payload.
/// The stream owns the DbContext (and with it the connection) and
/// releases it on every exit path.
/// </summary>
internal sealed class NpgsqlRawCopyExportStream : Stream
{
  private readonly NpgsqlRawCopyStream _copy;
  private readonly DbContext _context;

  private bool _drained;
  private bool _disposed;

  internal NpgsqlRawCopyExportStream(NpgsqlRawCopyStream copy, DbContext context)
  {
    _copy = copy ?? throw new ArgumentNullException(nameof(copy));
    _context = context ?? throw new ArgumentNullException(nameof(context));
  }

  public override bool CanRead => !_disposed;
  public override bool CanSeek => false;
  public override bool CanWrite => false;
  public override long Length => throw new NotSupportedException();
  public override long Position
  {
    get => throw new NotSupportedException();
    set => throw new NotSupportedException();
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    var read = _copy.Read(buffer, offset, count);
    if (read == 0) _drained = true;
    return read;
  }

  public override async Task<int> ReadAsync(
    byte[] buffer, int offset, int count, CancellationToken cancellationToken
  )
  {
    var read = await _copy.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
    if (read == 0) _drained = true;
    return read;
  }

  public override async ValueTask<int> ReadAsync(
    Memory<byte> buffer, CancellationToken cancellationToken = default
  )
  {
    var read = await _copy.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
    if (read == 0) _drained = true;
    return read;
  }

  public override void Flush() { /* read-only stream — nothing to flush */ }

  public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

  public override void SetLength(long value) => throw new NotSupportedException();

  public override void Write(byte[] buffer, int offset, int count) =>
    throw new NotSupportedException("The bulk-export stream is read-only.");

  public override async ValueTask DisposeAsync()
  {
    if (_disposed) return;
    _disposed = true;

    try
    {
      if (!_drained)
      {
        // Aborting mid-export: cancel the server-side COPY rather than
        // draining the rest of a potentially enormous payload.
        try { await _copy.CancelAsync().ConfigureAwait(false); }
        catch { /* the operation may already be broken */ }
      }

      try { await _copy.DisposeAsync().ConfigureAwait(false); }
      catch { /* post-cancel disposal noise; connection teardown follows */ }
    }
    finally
    {
      await _context.DisposeAsync().ConfigureAwait(false);
    }

    await base.DisposeAsync().ConfigureAwait(false);
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && !_disposed)
    {
      DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    base.Dispose(disposing);
  }
}
