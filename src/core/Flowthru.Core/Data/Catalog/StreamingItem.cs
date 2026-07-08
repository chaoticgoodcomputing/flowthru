using Flowthru.Data.Storage;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Data.Catalog;

/// <summary>
/// A read-only catalog item whose payload is a deferred
/// <see cref="FlowSource{TRow}"/> stream — the view produced by
/// <c>IItem&lt;IEnumerable&lt;TRow&gt;&gt;.AsStream()</c>. <see cref="Load"/>
/// hands back the (lazy) source description; nothing is read until a consumer
/// compiles and runs it. Node-level concerns (existence, inspection,
/// fingerprint, gating) delegate to the eager origin item, since both are
/// backed by the same medium.
/// </summary>
/// <typeparam name="TRow">The row type produced by the stream.</typeparam>
internal sealed class StreamingItem<TRow> : IReadOnlyItem<FlowSource<TRow>>
  where TRow : notnull
{
  private readonly ISupportsStreamingView<TRow> _source;
  private readonly IItem _origin;

  internal StreamingItem(string label, ISupportsStreamingView<TRow> source, IItem origin)
  {
    Label = label ?? throw new ArgumentNullException(nameof(label));
    _source = source ?? throw new ArgumentNullException(nameof(source));
    _origin = origin ?? throw new ArgumentNullException(nameof(origin));
  }

  public string Label { get; }
  public NodeTraits Traits => _origin.Traits;
  public Type DataType => typeof(FlowSource<TRow>);
  public InspectionLevel? MaxInspectionLevel => _origin.MaxInspectionLevel;
  public bool HasEfficientCount => _origin.HasEfficientCount;
  public string? StorageKind => _origin.StorageKind;
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _origin.ServiceDependencies;

  public FlowIO<FlowSource<TRow>> Load() => FlowIO.Pure(_source.OpenStreamingSource());

  public FlowIO<FlowUnit> Save(FlowSource<TRow> data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"StreamingItem[{Label}].Save",
      new InvalidOperationException(
        $"Streaming item '{Label}' is read-only — .AsStream() produces an input-only view. "
        + "Write through the eager item instead.")));

  public FlowIO<bool> Exists() => _origin.Exists();
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => _origin.InspectShallow(sampleSize);
  public FlowIO<ValidationResult> InspectDeep() => _origin.InspectDeep();
  public FlowIO<ValidationResult> InspectTarget() => _origin.InspectTarget();
  public FlowIO<ValidationResult> Validate() => _origin.InspectShallow();

  public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
  public FlowIO<FlowUnit> SaveUntyped(object data) => Save((FlowSource<TRow>)data);

  public FlowIO<string>? TryGetFingerprint() => _origin.TryGetFingerprint();
  public ISupportsBulkExport? TryGetBulkExport() => _origin.TryGetBulkExport();
  public ISupportsBulkImport? TryGetBulkImport() => _origin.TryGetBulkImport();
}
