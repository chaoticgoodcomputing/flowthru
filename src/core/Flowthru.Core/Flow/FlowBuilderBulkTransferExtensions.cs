using Flowthru.Data.Catalog;

namespace Flowthru.Flow;

/// <summary>
/// Intent-level bulk transfer helper on <see cref="FlowBuilder"/> — the
/// homogeneous-movement sibling of
/// <see cref="FlowBuilderStreamingExtensions.AddBulkLoad{T}"/>.
/// </summary>
public static class FlowBuilderBulkTransferExtensions
{
  /// <summary>
  /// Move the contents of one catalog item to another as an <em>on-DAG</em>
  /// identity step, so the transfer participates in scheduling, caching,
  /// pre-flight, and the metadata graph like any other step — and inherits
  /// the conflict keys of <em>both</em> endpoint items. The Flow developer
  /// writes intent — <c>AddBulkTransfer(orders, warehouseOrders)</c> — and
  /// pre-flight selects the execution rung: a probe of both endpoints'
  /// bulk capabilities picks the fastest compatible path, reporting the
  /// selection (and any downgrade to the streaming fallback) in the run's
  /// validation output. A pairing with no executable rung fails
  /// pre-flight instead of silently taking a slow path.
  /// </summary>
  /// <typeparam name="T">The row type moving from source to target.</typeparam>
  /// <param name="builder">The flow builder.</param>
  /// <param name="source">The source item to move data from.</param>
  /// <param name="target">The target item to land data in.</param>
  /// <param name="options">
  /// Transfer options (e.g. <see cref="BulkTransferOptions.RequireNative"/>);
  /// null = <see cref="BulkTransferOptions.Default"/>.
  /// </param>
  /// <param name="label">
  /// Optional step label; defaults to
  /// <c>BulkTransfer_{source.Label}_to_{target.Label}</c>.
  /// </param>
  public static FlowBuilder AddBulkTransfer<T>(
    this FlowBuilder builder,
    IItem<IEnumerable<T>> source,
    IItem<IEnumerable<T>> target,
    BulkTransferOptions? options = null,
    string? label = null
  )
    where T : notnull
  {
    if (builder is null) throw new ArgumentNullException(nameof(builder));
    if (source is null) throw new ArgumentNullException(nameof(source));
    if (target is null) throw new ArgumentNullException(nameof(target));

    // One negotiation per transfer, computed lazily and shared by both
    // endpoints: pre-flight forces it (and reports it); the endpoints
    // re-read the same cached verdict at execution time, so selection and
    // execution can never disagree.
    var stepLabel = label ?? BulkTransferNegotiation.DefaultStepLabel(source, target);
    var negotiation = new Lazy<Validated<PreFlightError, BulkTransferDecision>>(
      () => BulkTransferNegotiation.Negotiate(source, target, options, stepLabel)
    );

    return builder.AddStep<FlowSource<T>, FlowSource<T>>(
      label: stepLabel,
      transform: static s => s,
      inputs: new BulkTransferSourceItem<T>(source, negotiation),
      // The target endpoint also holds the source item: the native rung
      // pumps bytes from the source's bulk-export channel inside the
      // target's Save, re-probing both capabilities exactly as
      // negotiation did.
      outputs: new BulkTransferTargetItem<T>(target, source, negotiation)
    );
  }
}
