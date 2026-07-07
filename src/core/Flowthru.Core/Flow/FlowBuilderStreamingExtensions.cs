using Flowthru.Data.Catalog;
using Flowthru.Prelude;

namespace Flowthru.Flow;

/// <summary>
/// Intent-level streaming helpers on <see cref="FlowBuilder"/>.
/// </summary>
public static class FlowBuilderStreamingExtensions
{
  /// <summary>
  /// Wire a streaming source item to a batch sink as an <em>on-DAG</em> identity
  /// step: the source streams row-by-row and the sink writes batches, in
  /// O(batch) memory, with the load participating in scheduling, caching, and
  /// pre-flight like any other step. The Flow developer writes intent —
  /// <c>AddBulkLoad(orders.AsStream(), sink)</c> — not <c>Compile</c>/<c>Into</c>
  /// mechanics, and never the off-DAG form that would bypass the scheduler and
  /// the read cap.
  /// </summary>
  /// <typeparam name="T">The row type flowing from source to sink.</typeparam>
  /// <param name="builder">The flow builder.</param>
  /// <param name="source">A streaming source view (from <c>item.AsStream()</c>).</param>
  /// <param name="sink">The batch sink (e.g. an EFCore.Bulk streaming sink).</param>
  /// <param name="label">Optional step label; defaults to <c>BulkLoad_{source.Label}</c>.</param>
  /// <param name="sinkLabel">Optional output-item label; defaults to <c>{source.Label}.sink</c>.</param>
  public static FlowBuilder AddBulkLoad<T>(
    this FlowBuilder builder,
    IReadOnlyItem<FlowSource<T>> source,
    IFlowSink<T> sink,
    string? label = null,
    string? sinkLabel = null
  )
    where T : notnull
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (source is null) throw new ArgumentNullException(nameof(source));
    if (sink is null) throw new ArgumentNullException(nameof(sink));

    var output = new FlowSinkItem<T>(sinkLabel ?? $"{source.Label}.sink", sink);
    return builder.AddStep<FlowSource<T>, FlowSource<T>>(
      label: label ?? $"BulkLoad_{source.Label}",
      transform: static s => s,
      inputs: source,
      outputs: output
    );
  }
}
