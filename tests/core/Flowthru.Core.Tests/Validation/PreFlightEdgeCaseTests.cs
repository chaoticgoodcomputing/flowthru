using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Pre-flight + dependency-analyzer edge cases ported from the
/// legacy <c>CircularDependencyTests</c>, <c>MultipleWritersTests</c>,
/// and <c>02_Validation/GraphConstruction/</c>. The Phase-4 baseline
/// covers the happy path and one missing-input scenario; these
/// extend coverage to the structural-violation surface.
/// </summary>
[TestFixture]
public class PreFlightEdgeCaseTests
{
  [Test]
  public void TwoStepCycle_BuildThrows_WithCycleDescription()
  {
    var a = ItemFactory.Singleton.Memory<int>("cycle-a");
    var b = ItemFactory.Singleton.Memory<int>("cycle-b");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("cyclic", builder =>
      {
        builder.AddStep<int, int>("a-to-b", x => x, a, b);
        builder.AddStep<int, int>("b-to-a", x => x, b, a);
      })
    );
    Assert.That(ex!.Message, Does.Contain("Cycle detected"));
    Assert.That(ex.Message, Does.Contain("a-to-b"));
    Assert.That(ex.Message, Does.Contain("b-to-a"));
  }

  [Test]
  public void ThreeStepCycle_DetectedAndReported()
  {
    var a = ItemFactory.Singleton.Memory<int>("3-cyc-a");
    var b = ItemFactory.Singleton.Memory<int>("3-cyc-b");
    var c = ItemFactory.Singleton.Memory<int>("3-cyc-c");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("3cycle", builder =>
      {
        builder.AddStep<int, int>("a→b", x => x, a, b);
        builder.AddStep<int, int>("b→c", x => x, b, c);
        builder.AddStep<int, int>("c→a", x => x, c, a);
      })
    );
    Assert.That(ex!.Message, Does.Contain("Cycle detected"));
  }

  [Test]
  public void TwoStepsWritingSameOutput_FailSingleProducerLaw()
  {
    var input = ItemFactory.Singleton.Memory<int>("mw-in");
    var shared = ItemFactory.Singleton.Memory<int>("mw-shared");

    var ex = Assert.Throws<FlowBuildException>(() =>
      FlowBuilder.CreateFlow("mw", builder =>
      {
        builder.AddStep<int, int>("first", x => x + 1, input, shared);
        builder.AddStep<int, int>("second", x => x + 2, input, shared);
      })
    );
    Assert.That(ex!.Message, Does.Contain("'mw-shared'"),
      "Single-producer violation should name the contested item."
    );
    Assert.That(ex.Message, Does.Contain("first"));
    Assert.That(ex.Message, Does.Contain("second"));
  }

  [Test]
  public async Task PreFlightHook_ReturningInspectionFailed_AccumulatesAlongsideAdapter()
  {
    var input = ItemFactory.Singleton.Memory<int>("ef-in");
    var output = ItemFactory.Singleton.Memory<int>("ef-out");
    // No Save on input → adapter inspection will report MissingInput.

    var flow = FlowBuilder.CreateFlow("ef", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var hook = new InspectionFailedHook(
      "demo.hook",
      new PreFlightError.SchemaDrift("ef-in", "expected-shape", "actual-shape")
    );

    var result = await PreFlightPipeline
      .Run(flow, new IFlowValidationHook[] { hook })
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)inner).Errors;

    Assert.That(errors.OfType<PreFlightError.MissingInput>().Any(), Is.True,
      "Adapter inspection's missing-input failure should be present."
    );
    Assert.That(errors.OfType<PreFlightError.SchemaDrift>().Any(), Is.True,
      "Hook's schema-drift failure should accumulate alongside the adapter's failure (no short-circuit)."
    );
  }

  [Test]
  public async Task PreFlight_OnFlowWithOnlyIntermediateInputs_DoesNotInspectThem()
  {
    // The post-Phase-7 fix: pre-flight skips inputs that are produced
    // by some step in the same flow. Confirm: a 3-step linear chain
    // with one external input passes pre-flight even though the two
    // intermediates haven't been written yet.
    var raw = ItemFactory.Singleton.Memory<int>("pf-raw");
    var mid = ItemFactory.Singleton.Memory<int>("pf-mid");
    var final = ItemFactory.Singleton.Memory<int>("pf-final");
    await raw.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("pf-3step", b =>
    {
      b.AddStep<int, int>("a", x => x + 1, raw, mid);
      b.AddStep<int, int>("b", x => x + 1, mid, final);
    });

    var result = await PreFlightPipeline.Run(flow).Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(inner.IsValid, Is.True,
      "Pre-flight on a chain where only the source is external should pass — intermediates won't exist until run time."
    );
  }

  private sealed class InspectionFailedHook : IFlowValidationHook
  {
    private readonly PreFlightError _error;
    public InspectionFailedHook(string id, PreFlightError error)
    {
      HookId = id;
      _error = error;
    }
    public string HookId { get; }
    public FlowIO<Validated<PreFlightError, FlowUnit>> Validate(BuiltFlow flow) =>
      FlowIO.Pure<Validated<PreFlightError, FlowUnit>>(
        Validated<PreFlightError, FlowUnit>.Fail(_error)
      );
  }
}
