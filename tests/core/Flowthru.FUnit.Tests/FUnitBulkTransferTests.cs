using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.FUnit.Tests.Fixtures;
using Flowthru.Prelude;
using Flowthru.Step.Testing;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;

namespace Flowthru.FUnit.Tests;

/// <summary>
/// Exercises the FUnit bulk-transfer affordance (issue #134):
/// <see cref="FUnitContext.NegotiateTransfer{T}"/> runs the same
/// pre-flight rung negotiation <c>AddBulkTransfer</c> performs, so a step
/// test can assert the selected rung — or the negotiation error — without
/// building a flow.
/// </summary>
[TestFixture]
[Category("FUnit")]
#pragma warning disable FU002 // FUnitContext subclass not guarded by #if FUNIT_ENABLED — this IS the test suite.
public class FUnitBulkTransferTests : FUnitContext
{
  [Test]
  public void NegotiateTransfer_HeterogeneousPair_ReportsStreamingRung()
  {
    var negotiation = NegotiateTransfer<NumberRow>(
      new StreamableItem<NumberRow>("numbers"),
      new SinkableItem<NumberRow>("numbers-copy")
    );

    Assert.That(negotiation.IsValid, Is.True);
    var decision = ((Validated<PreFlightError, BulkTransferDecision>.Valid)negotiation).Value;
    Assert.That(decision.Rung, Is.EqualTo(BulkTransferRung.Streaming),
      "A pair with no native capabilities must negotiate the streaming fallback.");
    Assert.That(decision.Reason, Does.Contain("streaming fallback"),
      "The reported rung must carry its selection rationale.");
  }

  [Test]
  public void NegotiateTransfer_RequireNative_SurfacesThePreFlightError()
  {
    var negotiation = NegotiateTransfer<NumberRow>(
      new StreamableItem<NumberRow>("numbers"),
      new SinkableItem<NumberRow>("numbers-copy"),
      new BulkTransferOptions { RequireNative = true }
    );

    Assert.That(negotiation.IsValid, Is.False,
      "RequireNative must fail negotiation while no native rung exists.");
    var errors = ((Validated<PreFlightError, BulkTransferDecision>.Invalid)negotiation).Errors;
    Assert.That(errors.Single(), Is.InstanceOf<PreFlightError.BulkTransferRungUnavailable>());
  }

  // ---------------------------------------------------------------------------
  // Item doubles — a streamable source and a sinkable target, the minimum
  // shape the streaming rung's feasibility checks probe for.
  // ---------------------------------------------------------------------------

  private sealed class StreamableItem<T> : IItem<IEnumerable<T>>, ISupportsStreamingView<T>
    where T : notnull
  {
    public StreamableItem(string label) => Label = label;

    public string Label { get; }
    public NodeTraits Traits => new();

    public bool SupportsStreaming => true;
    public FlowSource<T> OpenStreamingSource() => FlowSource.FromEnumerable(Array.Empty<T>());

    public FlowIO<IEnumerable<T>> Load() => FlowIO.Pure(Enumerable.Empty<T>());
    public FlowIO<FlowUnit> Save(IEnumerable<T> data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
  }

  private sealed class SinkableItem<T> : IItem<IEnumerable<T>>, ISupportsStreamingSink<T>
    where T : notnull
  {
    public SinkableItem(string label) => Label = label;

    public string Label { get; }
    public NodeTraits Traits => new();

    public IFlowSink<T> OpenStreamingSink() => new RecordingSink<T>();

    public FlowIO<IEnumerable<T>> Load() => FlowIO.Pure(Enumerable.Empty<T>());
    public FlowIO<FlowUnit> Save(IEnumerable<T> data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(false);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
  }
}
