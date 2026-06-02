using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Hosting;

/// <summary>
/// Tests for <see cref="ValidationDepth.Hermetic"/> — the zero-I/O
/// pre-flight rung. The organizing invariant: at Hermetic depth pre-flight
/// validates structure and wiring but performs no I/O (no registration
/// probes, no adapter inspection, no resource acquisition), so an offline
/// smoke test (<c>DryRun.On + Hermetic</c>) runs with no live environment.
/// Plan-build structural failures (cycle / duplicate producer / duplicate
/// label) are a run precondition and surface as <c>FlowResult</c> data at
/// every depth rather than throwing.
/// </summary>
/// <remarks>
/// Companion to <see cref="RegistrationValidationHookTests"/> (registration
/// surface) and the throw-based <c>CircularDependencyTests</c> /
/// <c>MultipleWritersTests</c>, which pin that <c>FlowBuilder.Build()</c> —
/// the eager single-flow path — still throws.
/// </remarks>
[TestFixture]
public class HermeticValidationTests
{
  public sealed class EmptyCatalog : CatalogAbstract { }

  private interface IUnregisteredService { }

  /// <summary>Minimal external service ref for the dispatcher-presence test.</summary>
  private sealed record FakeExternalRef(string DagId, string DisplayName, string Category)
    : IExtensionServiceRef;

  // ── Smoke test: Hermetic + DryRun performs no registration I/O ───────

  [Test]
  public async Task Hermetic_SkipsShallowRegistrationProbe_AndRunsNothing()
  {
    // A registration hook defaults to MinimumDepth = Shallow — a live
    // probe. At Hermetic it must be skipped, so a smoke test passes even
    // when the probe would fail. The hook both fails AND counts, so a
    // green result with a zero count is unambiguous proof it never ran.
    var probeRan = 0;
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("live-probe", _ =>
      {
        probeRan++;
        return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Fail(
          new PreFlightError.RegistrationCheckFailed(
            HookId: "live-probe", CheckMessage: "DB unreachable"
          )
        ));
      });
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null,
      new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic, DryRun = DryRunOption.On }
    );

    Assert.Multiple(() =>
    {
      Assert.That(result.HasFailures, Is.False,
        "Hermetic must skip the Shallow registration probe, so the run passes offline.");
      Assert.That(probeRan, Is.Zero,
        "A Shallow-classified registration hook must not run at Hermetic depth.");
    });
  }

  [Test]
  public async Task Shallow_RunsRegistrationProbe()
  {
    // Negative control for the test above: at Shallow the same probe runs
    // and its failure blocks the run — proving Hermetic's skip is depth-
    // specific, not a blanket disable.
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("live-probe", _ => FlowIO.Pure(
        Validated<PreFlightError, FlowUnit>.Fail(new PreFlightError.RegistrationCheckFailed(
          HookId: "live-probe", CheckMessage: "DB unreachable"
        ))
      ));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null,
      new ExecutionOptions { ValidationDepth = ValidationDepth.Shallow, DryRun = DryRunOption.On }
    );

    Assert.That(result.HasFailures, Is.True,
      "At Shallow the live registration probe runs and its failure blocks the run.");
  }

  [Test]
  public async Task Hermetic_DoesNotPoisonRegistrationCache_ForLaterShallowRun()
  {
    // Depth-aware cache regression: a Hermetic pass (which skips the
    // Shallow probe) must not cache a result that a later Shallow run
    // would treat as "already validated" and short-circuit. The probe
    // counts; it must run exactly once — on the Shallow call.
    var probeRan = 0;
    var services = BuildHost(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.RegisterValidationHook("counted-probe", _ =>
      {
        probeRan++;
        return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
      });
    });

    var service = services.GetRequiredService<IFlowthruService>();
    await service.RunAsync(flowLabel: null,
      new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic, DryRun = DryRunOption.On });
    await service.RunAsync(flowLabel: null,
      new ExecutionOptions { ValidationDepth = ValidationDepth.Shallow, DryRun = DryRunOption.On });

    Assert.That(probeRan, Is.EqualTo(1),
      "Hermetic must not poison the cache: the Shallow run still runs the probe exactly once.");
  }

  // ── Structural failures surface as data, not exceptions ──────────────

  [Test]
  public async Task Hermetic_CrossFlowCycle_SurfacesAsFlowResultNotException()
  {
    // The cycle exists only in the MERGED DAG (each single-step flow is
    // acyclic), so this exercises the FlowthruService _merged path, not
    // FlowBuilder.Build(). It must return data, never throw.
    var a = ItemFactory.Singleton.Memory<int>("cycle-a");
    var b = ItemFactory.Singleton.Memory<int>("cycle-b");

    var services = BuildHost(host =>
    {
      host.RegisterCatalog(_ => new EmptyCatalog());
      host.RegisterFlow("a2b", () =>
        FlowBuilder.CreateFlow("a2b", fb => fb.AddStep<int, int>("a-to-b", x => x, a, b)));
      host.RegisterFlow("b2a", () =>
        FlowBuilder.CreateFlow("b2a", fb => fb.AddStep<int, int>("b-to-a", x => x, b, a)));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null, new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic });

    Assert.Multiple(() =>
    {
      Assert.That(result.HasFailures, Is.True);
      var failed = (StepResult.Failed)result.StepResults[0];
      Assert.That(failed.StepLabel, Does.StartWith("preflight:dag:"),
        "A merged-DAG cycle is a structural pre-flight failure, labelled preflight:dag:*.");
      Assert.That(failed.Error, Is.InstanceOf<RuntimeError.PreFlightFailed>());
      var cause = ((RuntimeError.PreFlightFailed)failed.Error).Cause;
      Assert.That(cause, Is.InstanceOf<PreFlightError.CircularDependency>());
    });
  }

  [Test]
  public async Task Hermetic_CrossFlowDuplicateProducer_SurfacesAsData()
  {
    var srcA = ItemFactory.Singleton.Memory<int>("dup-src-a");
    var srcB = ItemFactory.Singleton.Memory<int>("dup-src-b");
    var shared = ItemFactory.Singleton.Memory<int>("dup-shared-output");

    var services = BuildHost(host =>
    {
      host.RegisterCatalog(_ => new EmptyCatalog());
      host.RegisterFlow("p1", () =>
        FlowBuilder.CreateFlow("p1", fb => fb.AddStep<int, int>("producer-1", x => x, srcA, shared)));
      host.RegisterFlow("p2", () =>
        FlowBuilder.CreateFlow("p2", fb => fb.AddStep<int, int>("producer-2", x => x, srcB, shared)));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null, new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic });

    var failed = (StepResult.Failed)result.StepResults[0];
    var cause = ((RuntimeError.PreFlightFailed)failed.Error).Cause;
    Assert.That(cause, Is.InstanceOf<PreFlightError.DuplicateProducer>(),
      "An item produced by two steps across the merged DAG is a single-producer violation, "
      + "surfaced as data at Hermetic depth.");
  }

  [Test]
  public async Task Hermetic_DuplicateStepLabelAcrossFlows_SurfacesAsDuplicateLabel()
  {
    var services = BuildHost(host =>
    {
      host.RegisterCatalog(_ => new EmptyCatalog());
      host.RegisterFlow("flow-x", () =>
        FlowBuilder.CreateFlow("flow-x", fb => fb.AddStep("shared-step", () => { })));
      host.RegisterFlow("flow-y", () =>
        FlowBuilder.CreateFlow("flow-y", fb => fb.AddStep("shared-step", () => { })));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null, new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic });

    var failed = (StepResult.Failed)result.StepResults[0];
    var cause = ((RuntimeError.PreFlightFailed)failed.Error).Cause;
    Assert.That(cause, Is.InstanceOf<PreFlightError.DuplicateLabel>());
    Assert.That(((PreFlightError.DuplicateLabel)cause).Scope, Is.EqualTo("step"));
  }

  // ── Hermetic wiring checks (zero I/O) ────────────────────────────────

  [Test]
  public async Task Hermetic_UnregisteredCSharpServiceDependency_IsCaught()
  {
    // The new hermetic check: a step declaring a C# service dependency on
    // a type absent from DI fails pre-flight offline, via
    // IServiceProviderIsService (registration query, no instantiation).
    var input = ItemFactory.Singleton.Memory<int>("di-input");
    var output = ItemFactory.Singleton.Memory<int>("di-output");
    var step = new Step<int, int>(
      label: "needs-service",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: v => output.Save(v),
      serviceDependencies: new ServiceRef[] { ServiceRef.Of<IUnregisteredService>() }
    );

    var services = BuildHost(host =>
    {
      host.RegisterCatalog(_ => new EmptyCatalog());
      host.RegisterFlow("di-flow", () => FlowBuilder.CreateFlow("di-flow", fb => fb.Add(step)));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null, new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic, DryRun = DryRunOption.On });

    Assert.That(result.HasFailures, Is.True);
    var cause = ((RuntimeError.PreFlightFailed)((StepResult.Failed)result.StepResults[0]).Error).Cause;
    Assert.That(cause, Is.InstanceOf<PreFlightError.RegistrationCheckFailed>());
    Assert.That(cause.Message, Does.Contain(nameof(IUnregisteredService)),
      "The failure should name the unregistered service so it is actionable.");
  }

  [Test]
  public async Task Hermetic_MissingDispatcherForExternalServiceRef_IsCaught()
  {
    // Dispatcher presence is hermetic (a dictionary lookup). A step
    // referencing an external category with no registered dispatcher fails
    // offline; the dispatcher's Inspect probe (I/O) is never reached.
    var input = ItemFactory.Singleton.Memory<int>("disp-input");
    var output = ItemFactory.Singleton.Memory<int>("disp-output");
    var step = new Step<int, int>(
      label: "needs-dispatcher",
      transform: x => FlowIO.Pure(x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: v => output.Save(v),
      serviceDependencies: new ServiceRef[]
      {
        new ServiceRef.External(new FakeExternalRef("ext-1", "Ext One", "no-such-category")),
      }
    );

    var services = BuildHost(host =>
    {
      host.RegisterCatalog(_ => new EmptyCatalog());
      host.RegisterFlow("disp-flow", () => FlowBuilder.CreateFlow("disp-flow", fb => fb.Add(step)));
    });

    var service = services.GetRequiredService<IFlowthruService>();
    var result = await service.RunAsync(
      flowLabel: null, new ExecutionOptions { ValidationDepth = ValidationDepth.Hermetic, DryRun = DryRunOption.On });

    Assert.That(result.HasFailures, Is.True);
    var cause = ((RuntimeError.PreFlightFailed)((StepResult.Failed)result.StepResults[0]).Error).Cause;
    Assert.That(cause, Is.InstanceOf<PreFlightError.RegistrationCheckFailed>());
    Assert.That(cause.Message, Does.Contain("no-such-category"));
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static IServiceProvider BuildHost(Action<IFlowthruBuilder> configure)
  {
    var services = new ServiceCollection();
    services.AddFlowthru(configure);
    return services.BuildServiceProvider();
  }
}
