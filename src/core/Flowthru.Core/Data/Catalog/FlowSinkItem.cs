using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Catalog;

/// <summary>
/// A write-only DAG output that compiles a streamed <see cref="FlowSource{T}"/>
/// into an <see cref="IFlowSink{T}"/>. Created by
/// <c>FlowBuilder.AddBulkLoad(...)</c> as the output of a streaming bulk-load
/// step: <see cref="Save"/> drives the batch sink (O(batch) memory);
/// <see cref="Load"/> fails, because a sink is not a readable source.
/// </summary>
/// <typeparam name="T">The row type written to the sink.</typeparam>
internal sealed class FlowSinkItem<T> : IItem<FlowSource<T>>
  where T : notnull
{
  private readonly IFlowSink<T> _sink;

  internal FlowSinkItem(string label, IFlowSink<T> sink)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    _sink = sink ?? throw new ArgumentNullException(nameof(sink));
  }

  public string Label { get; }
  public NodeTraits Traits => new();
  public Type DataType => typeof(FlowSource<T>);

  public FlowIO<FlowSource<T>> Load() =>
    FlowIO.Fail<FlowSource<T>>(new RuntimeError.External(
      $"FlowSinkItem[{Label}].Load",
      new InvalidOperationException(
        $"Bulk-load sink '{Label}' is write-only — it consumes a FlowSource and cannot be loaded from.")));

  public FlowIO<FlowUnit> Save(FlowSource<T> data) => data.Compile().Into(_sink);

  // A sink has no pre-existing readable state; the write target is reachable.
  public FlowIO<bool> Exists() => FlowIO.Pure(false);
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => FlowIO.Pure(ValidationResult.Success());
  public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
  public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());

  public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
  public FlowIO<FlowUnit> SaveUntyped(object data) => Save((FlowSource<T>)data);
}
