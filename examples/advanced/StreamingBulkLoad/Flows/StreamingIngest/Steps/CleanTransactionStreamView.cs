using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Flows.Shared;

namespace StreamingBulkLoad.Flows.StreamingIngest.Steps;

/// <summary>
/// A read-only streaming Catalog Item that layers the stateless
/// <see cref="TransactionCleaning"/> transform onto a raw
/// <c>FlowSource&lt;TransactionRecord&gt;</c> view — i.e. it is
/// <c>raw.AsStream().Map(Normalize).Where(IsValid)</c> expressed as an Item so
/// it can be handed to <c>FlowBuilder.AddBulkLoad(...)</c> on the DAG.
/// </summary>
/// <remarks>
/// <para>
/// Why an Item wrapper rather than an inline call: the streaming combinators
/// (<c>Map</c>/<c>Where</c>) live on <see cref="FlowSource{A}"/>, not on the
/// <c>IReadOnlyItem</c> that <c>.AsStream()</c> returns, and the on-DAG bulk-load
/// helper takes an <em>item</em> (<c>IReadOnlyItem&lt;FlowSource&lt;T&gt;&gt;</c>),
/// not a bare source. Wrapping the transformed source back up as a read-only
/// item is the seam that keeps the load on the DAG — scheduled, pre-flighted,
/// under the read cap — while still applying the transform.
/// </para>
/// <para>
/// Everything stays lazy: <see cref="Load"/> only <em>describes</em> the mapped
/// stream; no row is pulled until <c>AddBulkLoad</c>'s sink compiles and drains
/// it one row group at a time. Peak memory is O(batch), same as the untransformed
/// path. Node-level concerns (existence, inspection, fingerprint) delegate to the
/// inner view, which delegates to the eager Parquet origin.
/// </para>
/// </remarks>
public sealed class CleanTransactionStreamView : IReadOnlyItem<FlowSource<TransactionRecord>>
{
  private readonly IReadOnlyItem<FlowSource<TransactionRecord>> _inner;

  /// <summary>
  /// Wrap a raw streaming view (from <c>rawParquetItem.AsStream()</c>) with the
  /// normalise-then-filter transform.
  /// </summary>
  public CleanTransactionStreamView(string label, IReadOnlyItem<FlowSource<TransactionRecord>> inner)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    _inner = inner ?? throw new ArgumentNullException(nameof(inner));
  }

  public string Label { get; }
  public NodeTraits Traits => _inner.Traits;
  public Type DataType => typeof(FlowSource<TransactionRecord>);

  /// <summary>Describe the transformed stream — the streaming combinators applied lazily.</summary>
  public FlowIO<FlowSource<TransactionRecord>> Load() =>
    _inner.Load().Map(source =>
      source
        .Map(TransactionCleaning.Normalize)
        .Where(TransactionCleaning.IsValid));

  public FlowIO<FlowUnit> Save(FlowSource<TransactionRecord> data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"CleanTransactionStreamView[{Label}].Save",
      new InvalidOperationException(
        $"'{Label}' is a read-only streaming view — write through the eager Parquet item instead.")));

  public FlowIO<bool> Exists() => _inner.Exists();
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => _inner.InspectShallow(sampleSize);
  public FlowIO<ValidationResult> InspectDeep() => _inner.InspectDeep();
  public FlowIO<ValidationResult> InspectTarget() => _inner.InspectTarget();
  public FlowIO<ValidationResult> Validate() => _inner.Validate();

  public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
  public FlowIO<FlowUnit> SaveUntyped(object data) => Save((FlowSource<TransactionRecord>)data);
}
