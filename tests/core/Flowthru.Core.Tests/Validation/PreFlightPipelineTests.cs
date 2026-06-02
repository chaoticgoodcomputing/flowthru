using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Tests for <see cref="PreFlightPipeline"/> — exercises Phase 4's
/// done-criterion that independent failures across multiple sources
/// aggregate into a single <see cref="Validated{TError, TValue}.Invalid"/>
/// without short-circuiting.
/// </summary>
[TestFixture]
public class PreFlightPipelineTests
{
  [Test]
  public async Task EmptyHooks_OnHealthyFlow_ReturnsValid()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");
    await input.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("ok", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var result = await PreFlightPipeline.Run(flow).Run();
    Assert.That(result, Is.InstanceOf<EffResult<Validated<PreFlightError, FlowUnit>>.Success>());
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(inner.IsValid, Is.True);
  }

  [Test]
  public async Task MissingInput_FromAdapterInspection_ReportsAsPreFlightError()
  {
    // Memory adapter returns NotFound when no Save has happened yet.
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    var flow = FlowBuilder.CreateFlow("missing", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var result = await PreFlightPipeline.Run(flow).Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>(),
      "Missing input should flow through adapter Inspect → Validated.Invalid.");
  }

  [Test]
  public async Task HermeticScope_SkipsAdapterInspection_SoMissingInputIsNotProbed()
  {
    // Same setup as MissingInput_FromAdapterInspection above (no Save, so
    // Full scope reports MissingInput) — but at Hermetic scope adapter
    // inspection (Layer 1, I/O) is skipped, so nothing is probed and the
    // flow validates clean. This pins the zero-I/O contract at the pipeline.
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");

    var flow = FlowBuilder.CreateFlow("missing", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var result = await PreFlightPipeline.Run(flow, scope: PreFlightScope.Hermetic).Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(inner.IsValid, Is.True,
      "Hermetic scope must skip adapter inspection — a missing external input is I/O, not "
      + "structure, so it is not probed offline.");
  }

  [Test]
  public async Task IndependentHooks_BothFail_AggregateBothErrors()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");
    await input.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("hooks", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var hookA = new FailingHook("hook.A", new PreFlightError.MissingInput("a", "src-a"));
    var hookB = new FailingHook("hook.B", new PreFlightError.SchemaDrift("b", "expected", "actual"));

    var result = await PreFlightPipeline
      .Run(flow, hooks: new IFlowValidationHook[] { hookA, hookB })
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)inner).Errors;
    Assert.That(errors.OfType<PreFlightError.MissingInput>().Any(), Is.True,
      "Hook A's MissingInput should be present in the aggregate.");
    Assert.That(errors.OfType<PreFlightError.SchemaDrift>().Any(), Is.True,
      "Hook B's SchemaDrift should be present alongside Hook A's failure — no short-circuit.");
  }

  [Test]
  public async Task ServiceProbes_FailuresAggregateAlongsideHookFailures()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");
    await input.Save(1).Run();
    var flow = FlowBuilder.CreateFlow("probes", b =>
      b.AddStep<int, int>("noop", x => x, input, output)
    );

    var probeFailure = FlowIO.Pure<Validated<PreFlightError, FlowUnit>>(
      Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.InspectionFailed("svc.X", "unreachable")
      )
    );

    var result = await PreFlightPipeline
      .Run(flow,
        hooks: new IFlowValidationHook[]
        {
          new FailingHook("hook", new PreFlightError.MissingInput("y", "src-y")),
        },
        serviceProbes: new[] { probeFailure })
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    var errors = ((Validated<PreFlightError, FlowUnit>.Invalid)inner).Errors;

    Assert.That(errors.OfType<PreFlightError.MissingInput>().Any(), Is.True);
    Assert.That(errors.OfType<PreFlightError.InspectionFailed>().Any(), Is.True);
  }

  private sealed class FailingHook : IFlowValidationHook
  {
    private readonly PreFlightError _error;
    public FailingHook(string id, PreFlightError error)
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
