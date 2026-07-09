using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// The read side of an on-DAG bulk transfer step — a streaming view over
/// the transfer's source item, created by
/// <c>FlowBuilder.AddBulkTransfer(...)</c>. <see cref="Load"/> opens the
/// stream the selected rung reads from; every node-level concern (label,
/// existence, inspection, fingerprint, service dependencies) delegates to
/// the origin item, so the transfer step inherits the source endpoint's
/// conflict keys and cache identity for free.
/// </summary>
/// <typeparam name="T">The row type moving through the transfer.</typeparam>
internal sealed class BulkTransferSourceItem<T> : IReadOnlyItem<FlowSource<T>>
  where T : notnull
{
  private readonly IItem<IEnumerable<T>> _origin;
  private readonly Lazy<Validated<PreFlightError, BulkTransferDecision>> _negotiation;

  internal BulkTransferSourceItem(
    IItem<IEnumerable<T>> origin,
    Lazy<Validated<PreFlightError, BulkTransferDecision>> negotiation
  )
  {
    _origin = origin ?? throw new ArgumentNullException(nameof(origin));
    _negotiation = negotiation ?? throw new ArgumentNullException(nameof(negotiation));
  }

  public string Label => _origin.Label;
  public NodeTraits Traits => _origin.Traits;
  public Type DataType => typeof(FlowSource<T>);
  public InspectionLevel? MaxInspectionLevel => _origin.MaxInspectionLevel;
  public bool HasEfficientCount => _origin.HasEfficientCount;
  public string? StorageKind => _origin.StorageKind;
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _origin.ServiceDependencies;

  /// <summary>
  /// Open the stream for the negotiated rung. A failed negotiation
  /// surfaces here as the pre-flight error it would have been — the
  /// backstop for hosts that run with validation off.
  /// </summary>
  /// <remarks>
  /// On the native rung, no rows flow through the DAG at all — the
  /// target endpoint's <c>Save</c> pumps provider-native bytes directly —
  /// so this returns a sentinel <see cref="FlowSource{T}"/> that fails
  /// loudly if anything ever compiles it. A pulled sentinel means
  /// negotiation and execution drifted apart, which must surface as an
  /// invariant violation rather than silently transferring nothing.
  /// </remarks>
  public FlowIO<FlowSource<T>> Load() =>
    _negotiation.Value.Match(
      onValid: decision => decision.Rung switch
      {
        BulkTransferRung.Streaming =>
          BulkTransferNegotiation.ResolveStreamingView(_origin) is { } view
            ? FlowIO.Pure(view.OpenStreamingSource())
            : FlowIO.Fail<FlowSource<T>>(new RuntimeError.InvariantViolated(
                $"BulkTransferSourceItem[{Label}].Load",
                "negotiation selected the streaming rung but the source no longer resolves a "
                + "streaming view — negotiation and execution must probe identically.")),
        BulkTransferRung.Native =>
          FlowIO.Pure(FlowSource.Lift<T>(_ => throw new InvalidOperationException(
            $"Bulk transfer '{Label}' negotiated the native rung: bytes move provider-side "
            + "and the row channel is a sentinel that must never be compiled. Pulling it "
            + "means rung selection and execution drifted apart."))),
        _ => FlowIO.Fail<FlowSource<T>>(new RuntimeError.InvariantViolated(
          $"BulkTransferSourceItem[{Label}].Load",
          $"negotiation selected rung '{decision.Rung}', which this version cannot execute.")),
      },
      onInvalid: errors => FlowIO.Fail<FlowSource<T>>(new RuntimeError.PreFlightFailed(errors[0]))
    );

  public FlowIO<FlowUnit> Save(FlowSource<T> data) =>
    FlowIO.Fail<FlowUnit>(new RuntimeError.External(
      $"BulkTransferSourceItem[{Label}].Save",
      new InvalidOperationException(
        $"Bulk transfer source '{Label}' is a read-only view. "
        + "Write through the origin item instead.")));

  public FlowIO<bool> Exists() => _origin.Exists();
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => _origin.InspectShallow(sampleSize);
  public FlowIO<ValidationResult> InspectDeep() => _origin.InspectDeep();
  public FlowIO<ValidationResult> InspectTarget() => _origin.InspectTarget();
  public FlowIO<ValidationResult> Validate() => _origin.InspectShallow();

  public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
  public FlowIO<FlowUnit> SaveUntyped(object data) => Save((FlowSource<T>)data);

  public FlowIO<string>? TryGetFingerprint() => _origin.TryGetFingerprint();
  public ISupportsBulkExport? TryGetBulkExport() => _origin.TryGetBulkExport();
  public ISupportsBulkImport? TryGetBulkImport() => _origin.TryGetBulkImport();
}
