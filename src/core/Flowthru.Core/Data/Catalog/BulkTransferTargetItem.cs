using Flowthru.Data.Storage;

namespace Flowthru.Data.Catalog;

/// <summary>
/// The write side of an on-DAG bulk transfer step, created by
/// <c>FlowBuilder.AddBulkTransfer(...)</c> as the step's output.
/// <see cref="Save"/> executes the negotiated rung — the native rung
/// pumps provider-native bytes from the source endpoint's bulk-export
/// channel into the target's bulk-import channel; the streaming rung
/// compiles the streamed <see cref="FlowSource{T}"/> into the target's
/// batch sink. Every node-level concern delegates to the target item, so
/// the transfer step inherits the target endpoint's conflict keys, cache
/// identity, and single-producer protection for free. Implements
/// <see cref="IBulkTransferEndpoint"/> so pre-flight folds the negotiation
/// verdict into its aggregate and the host reports the selected rung.
/// </summary>
/// <typeparam name="T">The row type moving through the transfer.</typeparam>
internal sealed class BulkTransferTargetItem<T> : IItem<FlowSource<T>>, IBulkTransferEndpoint
  where T : notnull
{
  private readonly IItem<IEnumerable<T>> _origin;
  private readonly IItem<IEnumerable<T>> _sourceOrigin;
  private readonly Lazy<Validated<PreFlightError, BulkTransferDecision>> _negotiation;

  internal BulkTransferTargetItem(
    IItem<IEnumerable<T>> origin,
    IItem<IEnumerable<T>> sourceOrigin,
    Lazy<Validated<PreFlightError, BulkTransferDecision>> negotiation
  )
  {
    _origin = origin ?? throw new ArgumentNullException(nameof(origin));
    _sourceOrigin = sourceOrigin ?? throw new ArgumentNullException(nameof(sourceOrigin));
    _negotiation = negotiation ?? throw new ArgumentNullException(nameof(negotiation));
  }

  public string Label => _origin.Label;
  public NodeTraits Traits => _origin.Traits;
  public Type DataType => typeof(FlowSource<T>);
  public InspectionLevel? MaxInspectionLevel => _origin.MaxInspectionLevel;
  public bool HasEfficientCount => _origin.HasEfficientCount;
  public string? StorageKind => _origin.StorageKind;
  public IReadOnlyList<ServiceDependency> ServiceDependencies => _origin.ServiceDependencies;

  /// <inheritdoc/>
  public Validated<PreFlightError, BulkTransferDecision> Negotiation => _negotiation.Value;

  public FlowIO<FlowSource<T>> Load() =>
    FlowIO.Fail<FlowSource<T>>(new RuntimeError.External(
      $"BulkTransferTargetItem[{Label}].Load",
      new InvalidOperationException(
        $"Bulk transfer target '{Label}' is write-through only. "
        + "Read through the target item instead.")));

  /// <summary>
  /// Execute the negotiated rung. A failed negotiation surfaces here as
  /// the pre-flight error it would have been — the backstop for hosts
  /// that run with validation off. Execution re-probes the endpoints
  /// through the same capability resolvers negotiation used, so what
  /// pre-flight reported is — by construction — what runs; a probe that
  /// no longer resolves is an invariant violation, never a silent
  /// downgrade.
  /// </summary>
  /// <remarks>
  /// On the native rung the <paramref name="data"/> row channel is the
  /// source endpoint's sentinel and is deliberately ignored: the transfer
  /// is a provider-native byte pump
  /// (<see cref="BulkTransferBytePump"/>) from the source's
  /// <see cref="ISupportsBulkExport"/> channel into this target's
  /// <see cref="ISupportsBulkImport"/> channel, and no row ever
  /// materialises in the CLR.
  /// </remarks>
  public FlowIO<FlowUnit> Save(FlowSource<T> data) =>
    _negotiation.Value.Match(
      onValid: decision => decision.Rung switch
      {
        BulkTransferRung.Streaming =>
          BulkTransferNegotiation.ResolveStreamingSink(_origin) is { } sinkable
            ? data.Compile().Into(sinkable.OpenStreamingSink())
            : FlowIO.Fail<FlowUnit>(new RuntimeError.InvariantViolated(
                $"BulkTransferTargetItem[{Label}].Save",
                "negotiation selected the streaming rung but the target no longer resolves a "
                + "streaming sink — negotiation and execution must probe identically.")),
        BulkTransferRung.Native =>
          _sourceOrigin.TryGetBulkExport() is { } export && _origin.TryGetBulkImport() is { } import
            ? BulkTransferBytePump.Transfer(
                export, import, source: $"BulkTransferTargetItem[{Label}].Save")
            : FlowIO.Fail<FlowUnit>(new RuntimeError.InvariantViolated(
                $"BulkTransferTargetItem[{Label}].Save",
                "negotiation selected the native rung but the endpoints no longer resolve a "
                + "bulk capability pair — negotiation and execution must probe identically.")),
        _ => FlowIO.Fail<FlowUnit>(new RuntimeError.InvariantViolated(
          $"BulkTransferTargetItem[{Label}].Save",
          $"negotiation selected rung '{decision.Rung}', which this version cannot execute.")),
      },
      onInvalid: errors => FlowIO.Fail<FlowUnit>(new RuntimeError.PreFlightFailed(errors[0]))
    );

  public FlowIO<bool> Exists() => _origin.Exists();
  public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => _origin.InspectShallow(sampleSize);
  public FlowIO<ValidationResult> InspectDeep() => _origin.InspectDeep();
  public FlowIO<ValidationResult> InspectTarget() => _origin.InspectTarget();
  public FlowIO<ValidationResult> Validate() => _origin.InspectTarget();

  public FlowIO<object> LoadUntyped() => Load().Map(value => (object)value!);
  public FlowIO<FlowUnit> SaveUntyped(object data) => Save((FlowSource<T>)data);

  public FlowIO<string>? TryGetFingerprint() => _origin.TryGetFingerprint();
  public ISupportsBulkExport? TryGetBulkExport() => _origin.TryGetBulkExport();
  public ISupportsBulkImport? TryGetBulkImport() => _origin.TryGetBulkImport();
}
