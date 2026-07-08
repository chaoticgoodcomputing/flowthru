using System.Buffers;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Flow;

/// <summary>
/// Execution machinery for the native bulk-transfer rung: a bounded-buffer
/// byte pump from a paired source's <see cref="ISupportsBulkExport"/>
/// channel into a target's <see cref="ISupportsBulkImport"/> channel.
/// Bytes pass through a single fixed-size buffer — no row is ever
/// materialised as a CLR object — so peak memory is O(buffer) regardless
/// of table size.
/// </summary>
/// <remarks>
/// Lifecycle discipline (mirroring <c>FlowSourceCompiler.Into</c>): both
/// channels are disposed on <em>every</em> exit path;
/// <see cref="BulkImportChannel.CompleteAsync"/> is called only after the
/// full payload was written and flushed, so a failure or cancellation
/// anywhere in the pump disposes the import channel without completion —
/// the channel's abort signal, which a transactional importer answers
/// with a rollback.
/// </remarks>
internal static class BulkTransferBytePump
{
  /// <summary>
  /// The bounded copy-buffer size, in bytes. One buffer is rented per
  /// transfer, making the pump O(1) in memory.
  /// </summary>
  internal const int BufferBytes = 81920;

  /// <summary>
  /// Run one native transfer: open the export channel, open the import
  /// channel, pump every byte across, complete the import, and dispose
  /// both channels regardless of outcome. Typed errors from either
  /// capability's open surface unchanged through the
  /// <see cref="FlowIO{A}"/> failure channel.
  /// </summary>
  /// <param name="export">The source endpoint's export capability.</param>
  /// <param name="import">The target endpoint's import capability.</param>
  /// <param name="source">Diagnostic label for errors thrown inside the pump.</param>
  internal static FlowIO<FlowUnit> Transfer(
    ISupportsBulkExport export,
    ISupportsBulkImport import,
    string source
  ) =>
    FlowIO.LiftAsync(async ct =>
      {
        Stream? exportStream = null;
        BulkImportChannel? importChannel = null;
        try
        {
          exportStream = Unwrap(await export.OpenBulkExport().Run(ct).ConfigureAwait(false));
          importChannel = Unwrap(await import.OpenBulkImport().Run(ct).ConfigureAwait(false));

          await PumpAsync(exportStream, importChannel, ct).ConfigureAwait(false);

          await importChannel.FlushAsync(ct).ConfigureAwait(false);
          await importChannel.CompleteAsync(ct).ConfigureAwait(false);
          return FlowUnit.Default;
        }
        finally
        {
          // Import first: when the pump did not reach CompleteAsync, this
          // disposal is the abort signal (rollback) and must fire before
          // the export side is torn down. Disposal failures never mask
          // the pump's own outcome.
          if (importChannel is not null) await DisposeQuietly(importChannel).ConfigureAwait(false);
          if (exportStream is not null) await DisposeQuietly(exportStream).ConfigureAwait(false);
        }
      }, source)
      .MapError(err => FlowSource.UnwrapFailure(err, FlowSource.Id));

  /// <summary>
  /// The bounded-buffer copy loop. Cancellation-aware on both the read
  /// and the write side; the caller owns channel lifecycle.
  /// </summary>
  private static async Task PumpAsync(
    Stream exportStream,
    BulkImportChannel importChannel,
    CancellationToken ct
  )
  {
    var buffer = ArrayPool<byte>.Shared.Rent(BufferBytes);
    try
    {
      int read;
      while ((read = await exportStream
        .ReadAsync(buffer.AsMemory(0, BufferBytes), ct)
        .ConfigureAwait(false)) > 0)
      {
        await importChannel.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
      }
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  /// <summary>
  /// Unwrap an opened channel, rethrowing a typed failure through the
  /// <see cref="FlowSourceFailure"/> carrier so the capability's own
  /// <see cref="RuntimeError"/> survives the surrounding
  /// <c>LiftAsync</c> boundary instead of flattening to
  /// <see cref="RuntimeError.External"/>.
  /// </summary>
  private static A Unwrap<A>(EffResult<A> result) =>
    result switch
    {
      EffResult<A>.Success s => s.Value,
      EffResult<A>.Failure f => throw new FlowSourceFailure(f.Error),
      _ => throw new InvalidOperationException("Unreachable: EffResult is a closed sum"),
    };

  /// <summary>
  /// Best-effort disposal — an abort-path cleanup failure must not mask
  /// the error that put us on the abort path, and a cleanup failure after
  /// a committed transfer must not un-succeed it.
  /// </summary>
  private static async ValueTask DisposeQuietly(Stream stream)
  {
    try
    {
      await stream.DisposeAsync().ConfigureAwait(false);
    }
    catch
    {
      // Best-effort: the channel's own dispose already guarantees the
      // abort/rollback semantics; a secondary teardown failure here is
      // noise relative to the transfer's outcome.
    }
  }
}
