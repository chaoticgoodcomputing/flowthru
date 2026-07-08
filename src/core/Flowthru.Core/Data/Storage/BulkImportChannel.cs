namespace Flowthru.Data.Storage;

/// <summary>
/// The writable byte channel a bulk-import capability
/// (<see cref="ISupportsBulkImport.OpenBulkImport"/>) hands to the native
/// transfer rung. It is an ordinary <see cref="Stream"/> plus one explicit
/// lifecycle signal: <see cref="CompleteAsync"/> finalizes the import
/// (e.g. finishes a Postgres <c>COPY</c> and commits its transaction),
/// while disposal <em>without</em> a prior completion aborts it (e.g.
/// rolls the transaction back).
/// </summary>
/// <remarks>
/// <para>
/// The explicit signal exists because disposal alone cannot distinguish
/// "the payload arrived whole" from "the transfer died partway" — and a
/// torn half-imported table is exactly the failure a transactional target
/// must never exhibit. The protocol mirrors <see cref="Flowthru.Prelude.IFlowSink{T}"/>:
/// the caller writes the payload, calls <see cref="CompleteAsync"/> exactly
/// once on success, and disposes on <em>every</em> path. Implementations
/// must therefore treat <c>Dispose</c>/<c>DisposeAsync</c> without a
/// completed <see cref="CompleteAsync"/> as the abort signal, and make the
/// abort best-effort-safe to call after any failure (including
/// cancellation).
/// </para>
/// <para>
/// Only the native bulk-transfer rung drives this channel; Flow and
/// Catalog Developers never touch it directly.
/// </para>
/// </remarks>
public abstract class BulkImportChannel : Stream
{
  /// <summary>
  /// Finalize the import after the complete payload has been written:
  /// flush any provider buffers, finish the provider's bulk operation,
  /// and commit whatever transaction the import runs in. Called at most
  /// once, and only after every payload byte was written successfully. A
  /// failure here (the provider rejecting the payload, a commit error)
  /// must leave the channel in a state where disposal still aborts
  /// cleanly.
  /// </summary>
  public abstract ValueTask CompleteAsync(CancellationToken cancellationToken);
}
