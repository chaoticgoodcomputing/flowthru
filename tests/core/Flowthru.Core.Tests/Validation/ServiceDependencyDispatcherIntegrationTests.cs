using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// End-to-end integration tests for the
/// <see cref="IServiceDependencyDispatcher"/> pre-flight layer
/// (<see cref="PreFlightPipeline.Run"/>'s Layer 4). The contract-level
/// tests in <c>Runtime/ServiceDependencyDispatcherTests.cs</c> pin the
/// dispatcher's per-call behavior in isolation; these exercise the
/// integration site — DI resolution, dedup, category routing, and the
/// no-dispatcher-registered failure mode.
/// </summary>
[TestFixture]
public class ServiceDependencyDispatcherIntegrationTests
{
  // ── Test-only extension types (parallel to ServiceDependencyDispatcherTests) ─

  private sealed record FakeExtensionServiceDependency(string DagId, string DisplayName, string Category)
    : IExtensionServiceDependency;

  private sealed class RecordingDispatcher : IServiceDependencyDispatcher
  {
    private readonly bool _fail;
    public RecordingDispatcher(string category, bool fail = false)
    {
      Category = category;
      _fail = fail;
    }
    public string Category { get; }
    public int InvokeCount { get; private set; }
    public List<string> InvokedDagIds { get; } = new();

    public FlowIO<Validated<PreFlightError, FlowUnit>> Inspect(IExtensionServiceDependency serviceRef)
    {
      InvokeCount++;
      InvokedDagIds.Add(serviceRef.DagId);
      return _fail
        ? FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Fail(
            new PreFlightError.InspectionFailed(serviceRef.DagId, "simulated")))
        : FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
    }
  }

  // ── Helpers ─────────────────────────────────────────────────────────────

  private sealed class EmptyCatalog { }

  private static BuiltFlow FlowWithStepCarryingRef(
    string stepLabel,
    IExtensionServiceDependency extRef,
    string flowLabel = "test"
  )
  {
    // The step must declare at least one output for slicing-by-flow-label
    // to retain it. A memory sentinel works without adapter wiring.
    return FlowBuilder.CreateFlow(flowLabel, b =>
    {
      var output = ItemFactory.Singleton.Memory<int>($"{stepLabel}.out");
      var step = new Step<FlowUnit, int>(
        label: stepLabel,
        transform: _ => FlowIO.Pure(0),
        inputs: System.Array.Empty<IItem>(),
        outputs: new IItem[] { output },
        loadInputs: () => FlowIO.Pure(FlowUnit.Default),
        saveOutputs: value => output.Save(value),
        serviceDependencies: new[] { (ServiceDependency)new ServiceDependency.External(extRef) }
      );
      b.Add(step);
    });
  }

  // ── Integration: dispatcher invoked when step declares matching ref ───

  [Test]
  public async Task PreFlight_StepCarriesExternalRef_DispatcherInvoked()
  {
    var dispatcher = new RecordingDispatcher(category: "python");
    var extRef = new FakeExtensionServiceDependency("ext.python.MyService", "MyService", "python");
    var flow = FlowWithStepCarryingRef("step.A", extRef);

    var result = await PreFlightPipeline.Run(
      flow,
      serviceRefDispatchers: new IServiceDependencyDispatcher[] { dispatcher }
    ).Run();

    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>(),
      "Dispatcher returned Pass; pre-flight should be valid end-to-end.");
    Assert.That(dispatcher.InvokeCount, Is.EqualTo(1),
      "The dispatcher's Inspect must run exactly once when its category matches.");
    Assert.That(dispatcher.InvokedDagIds.Single(), Is.EqualTo("ext.python.MyService"));
  }

  [Test]
  public async Task PreFlight_DispatcherFails_PreFlightCarriesInspectionFailed()
  {
    var dispatcher = new RecordingDispatcher(category: "python", fail: true);
    var extRef = new FakeExtensionServiceDependency("ext.python.BadService", "BadService", "python");
    var flow = FlowWithStepCarryingRef("step.B", extRef);

    var result = await PreFlightPipeline.Run(
      flow,
      serviceRefDispatchers: new IServiceDependencyDispatcher[] { dispatcher }
    ).Run();

    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.InspectionFailed>());
    Assert.That(((PreFlightError.InspectionFailed)invalid.Errors[0]).ItemId,
      Is.EqualTo("ext.python.BadService"));
  }

  [Test]
  public async Task PreFlight_NoDispatcherForCategory_EmitsRegistrationCheckFailed()
  {
    // Dispatcher registered for "sql" but step's ref is "python". The
    // unregistered category surfaces as RegistrationCheckFailed — a
    // user-actionable error pointing at the missing DI registration.
    var sqlDispatcher = new RecordingDispatcher(category: "sql");
    var extRef = new FakeExtensionServiceDependency("ext.python.Svc", "Svc", "python");
    var flow = FlowWithStepCarryingRef("step.C", extRef);

    var result = await PreFlightPipeline.Run(
      flow,
      serviceRefDispatchers: new IServiceDependencyDispatcher[] { sqlDispatcher }
    ).Run();

    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors, Has.Count.EqualTo(1));
    var rcf = (PreFlightError.RegistrationCheckFailed)invalid.Errors[0];
    Assert.That(rcf.HookId, Does.Contain("python"),
      "RegistrationCheckFailed must name the unregistered category.");
    Assert.That(rcf.CheckMessage, Does.Contain("ext.python.Svc"),
      "Error must name the specific ref so the user can locate the call site.");
    Assert.That(rcf.CheckMessage, Does.Contain("step.C"),
      "Error must name the step that carries the unresolved ref.");
    Assert.That(sqlDispatcher.InvokeCount, Is.EqualTo(0),
      "Wrong-category dispatchers must not be invoked.");
  }

  [Test]
  public async Task PreFlight_SameRefAcrossMultipleSteps_DispatcherInvokedOnce()
  {
    // Dedup by DagId — a service ref shared by N steps probes its
    // remote service exactly once per RunAsync, not once per step.
    // Mirrors the "called once across N steps sharing a dep" invariant
    // from the CSharp-side inspector path.
    var dispatcher = new RecordingDispatcher(category: "ext");
    var sharedRef = new FakeExtensionServiceDependency("ext.shared.Svc", "Svc", "ext");

    var flow = FlowBuilder.CreateFlow("test", b =>
    {
      var stepA = new Step<FlowUnit, FlowUnit>(
        label: "A",
        transform: _ => FlowIO.Pure(FlowUnit.Default),
        inputs: System.Array.Empty<IItem>(),
        outputs: System.Array.Empty<IItem>(),
        loadInputs: () => FlowIO.Pure(FlowUnit.Default),
        saveOutputs: _ => FlowIO.Pure(FlowUnit.Default),
        serviceDependencies: new[] { (ServiceDependency)new ServiceDependency.External(sharedRef) }
      );
      var stepB = new Step<FlowUnit, FlowUnit>(
        label: "B",
        transform: _ => FlowIO.Pure(FlowUnit.Default),
        inputs: System.Array.Empty<IItem>(),
        outputs: System.Array.Empty<IItem>(),
        loadInputs: () => FlowIO.Pure(FlowUnit.Default),
        saveOutputs: _ => FlowIO.Pure(FlowUnit.Default),
        serviceDependencies: new[] { (ServiceDependency)new ServiceDependency.External(sharedRef) }
      );
      b.Add(stepA);
      b.Add(stepB);
    });

    await PreFlightPipeline.Run(
      flow,
      serviceRefDispatchers: new IServiceDependencyDispatcher[] { dispatcher }
    ).Run();

    Assert.That(dispatcher.InvokeCount, Is.EqualTo(1),
      "Same DagId across multiple steps must produce one Inspect call total.");
  }

  [Test]
  public async Task PreFlight_NoDispatchersAtAll_NoOpForCSharpRefs()
  {
    // A flow with only ServiceDependency.CSharp (no Externals) and no
    // dispatchers registered must pre-flight successfully — the
    // dispatcher layer is opt-in via External refs.
    var flow = FlowBuilder.CreateFlow("test", b =>
    {
      var step = new Step<FlowUnit, FlowUnit>(
        label: "csharp-only",
        transform: _ => FlowIO.Pure(FlowUnit.Default),
        inputs: System.Array.Empty<IItem>(),
        outputs: System.Array.Empty<IItem>(),
        loadInputs: () => FlowIO.Pure(FlowUnit.Default),
        saveOutputs: _ => FlowIO.Pure(FlowUnit.Default),
        serviceDependencies: new[] { ServiceDependency.Of<EmptyCatalog>() }
      );
      b.Add(step);
    });

    var result = await PreFlightPipeline.Run(flow).Run();

    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;
    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>(),
      "CSharp-only ServiceDependencies must not require the dispatcher layer.");
  }

  // ── End-to-end via FlowthruService (DI-resolved dispatchers) ──────────

  [Test]
  public async Task FlowthruService_DispatcherFromDI_InvokedDuringRunAsync()
  {
    // The hosting layer must resolve IServiceDependencyDispatcher implementations
    // from DI and forward them to PreFlightPipeline. Without this wiring,
    // dispatchers registered via services.AddSingleton<IServiceDependencyDispatcher>
    // would never run.
    var dispatcher = new RecordingDispatcher(category: "ext");
    var sharedRef = new FakeExtensionServiceDependency("ext.MyDi.Svc", "Svc", "ext");

    var services = new ServiceCollection();
    services.AddSingleton<IServiceDependencyDispatcher>(dispatcher);
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("ext-flow", () => FlowWithStepCarryingRef("step.X", sharedRef));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("ext-flow");
    Assert.That(result.IsSuccess, Is.True,
      "Dispatcher returned Pass; flow should run successfully end-to-end.");
    Assert.That(dispatcher.InvokeCount, Is.EqualTo(1),
      "FlowthruService must resolve IServiceDependencyDispatcher from DI and pass to pre-flight.");
  }

  [Test]
  public async Task FlowthruService_NoDispatcherInDI_RegistrationCheckFailedSurfaced()
  {
    // No dispatcher registered for the "ext" category → pre-flight fails
    // with RegistrationCheckFailed. End-to-end this should surface as
    // a StepResult.Failed wrapping RuntimeError.PreFlightFailed → FT3006.
    var orphanedRef = new FakeExtensionServiceDependency("ext.orphan", "Orphan", "ext");

    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterFlow("orphan-flow", () => FlowWithStepCarryingRef("step.Y", orphanedRef));
    });

    await using var sp = services.BuildServiceProvider();
    var flowthru = sp.GetRequiredService<IFlowthruService>();

    var result = await flowthru.RunAsync("orphan-flow");
    Assert.That(result.HasFailures, Is.True);
    var failure = result.FirstFailure!;
    Assert.That(failure.StepLabel, Does.StartWith("preflight:registration:"),
      "Unregistered dispatcher category surfaces as a preflight:registration:* failure.");
    var pff = (RuntimeError.PreFlightFailed)failure.Error;
    Assert.That(pff.Cause, Is.InstanceOf<PreFlightError.RegistrationCheckFailed>());
  }
}
