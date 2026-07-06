namespace Flowthru.Prelude;

/// <summary>
/// Consumes a compiled <see cref="FlowSource{A}"/> batch-at-a-time — the sink
/// counterpart to the source. Driven by <see cref="FlowSourceCompiler{A}.Into"/>
/// inside the effect envelope: <see cref="OpenAsync"/> once, then
/// <see cref="WriteBatchAsync"/> per batch as elements arrive, then
/// <see cref="CompleteAsync"/>. <see cref="System.IAsyncDisposable.DisposeAsync"/>
/// runs on every exit path and must <em>abort</em> (e.g. roll back an open
/// transaction) when <see cref="CompleteAsync"/> was not reached.
/// </summary>
/// <remarks>
/// The writer owns its batch size (<see cref="BatchSize"/>), so the driver
/// re-chunks the row-at-a-time stream into the writer's preferred batches —
/// decoupling the read batch (e.g. a Parquet row group) from the write batch
/// (e.g. a Postgres <c>COPY</c>).
/// </remarks>
/// <typeparam name="T">The element type consumed by the sink.</typeparam>
public interface IFlowSink<in T> : IAsyncDisposable
{
  /// <summary>The number of elements per <see cref="WriteBatchAsync"/> call.</summary>
  int BatchSize { get; }

  /// <summary>Begin consumption (e.g. open a transaction / <c>COPY</c> writer).</summary>
  ValueTask OpenAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Write one batch of elements. The batch is valid only for the duration of
  /// the call — the driver may reuse the backing buffer afterwards, so a sink
  /// that retains rows must copy them.
  /// </summary>
  ValueTask WriteBatchAsync(IReadOnlyList<T> batch, CancellationToken cancellationToken);

  /// <summary>Finish consumption successfully (e.g. commit the transaction).</summary>
  ValueTask CompleteAsync(CancellationToken cancellationToken);
}
