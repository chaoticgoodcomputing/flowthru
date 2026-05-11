using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Validation.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Runtime;

/// <summary>
/// Tests for the DI-wired service-inspector path: callers register
/// <see cref="IFlowServiceInspector{TService}"/> sidecars (or
/// delegate probes) via <see cref="IFlowthruBuilder.AddFlowServiceInspector{TService}(IFlowServiceInspector{TService})"/>
/// and its lambda overload; <see cref="IFlowthruService.RunAsync"/> invokes them
/// during pre-flight, surfacing the aggregated outcome as a single
/// <c>preflight</c> <see cref="StepResult.Failed"/> wrapping a
/// <see cref="RuntimeError.InvariantViolated"/>.
/// </summary>
/// <remarks>
/// <para>
/// Ports the legacy <c>02_Validation/PreFlightInspection/ServiceInspectionTests</c>
/// (gap #2 from the test-coverage gap analysis). The legacy fixture exercised
/// the inspector pipeline through a direct <c>flow.ValidateExternalInputsAsync</c>
/// call against hand-rolled FlowStep instances; the active surface exposes the
/// same behaviours through the <see cref="IFlowthruService"/> hosting boundary
/// (<see cref="ServiceCollectionExtensions.AddFlowthru"/> + <c>RunAsync</c>).
/// </para>
/// <para>
/// The active pipeline drives each registered inspector probe once per call
/// (registrations are appended to a list, not deduplicated per step), so the
/// legacy "called once across N steps sharing the dep" and "user override wins"
/// assertions are reframed here as the equivalent invariants on the registration
/// surface rather than the dispatch loop.
/// </para>
/// </remarks>
[TestFixture]
public class ServiceInspectionTests
{
  // ── Test fixtures ───────────────────────────────────────────────────

  public interface IFakeService { }
  public sealed class FakeService : IFakeService { }

  public sealed class CountingCatalog : CatalogAbstract
  {
    public IItem<int> Input => CreateItem(() => ItemFactory.Singleton.Memory<int>("svc-input"));
    public IItem<int> Output => CreateItem(() => ItemFactory.Singleton.Memory<int>("svc-output"));
  }

  /// <summary>Class-based inspector that records per-instance invocation counts (avoids static-state pollution under parallel test execution).</summary>
  public sealed class ProbingFakeServiceInspector : IFlowServiceInspector<IFakeService>
  {
    private int _count;

    public int InvocationCount => Volatile.Read(ref _count);

    public Task<InspectionResult> InspectAsync(
      IFakeService service,
      CancellationToken cancellationToken
    )
    {
      Interlocked.Increment(ref _count);
      return Task.FromResult(Inspect.Pass());
    }
  }

  // ── (1) Inspector registered → invoked, success surfaces ─────────────

  [Test]
  public async Task DelegateInspector_RegisteredAndSucceeds_FlowRunsSuccessfully()
  {
    var probeCount = 0;
    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
      {
        Interlocked.Increment(ref probeCount);
        return Task.FromResult(Inspect.Pass());
      });
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.Multiple(() =>
    {
      Assert.That(result.IsSuccess, Is.True, "Pre-flight should pass when the inspector reports success.");
      Assert.That(probeCount, Is.EqualTo(1), "The inspector probe should run exactly once per RunAsync call.");
    });
  }

  // ── (2) Service registered, no inspector → success (non-fatal) ───────

  [Test]
  public async Task ServiceRegistered_NoInspectorRegistered_PreflightStillSucceeds()
  {
    // Mirrors the C# side's non-fatal-missing-inspector contract: when no
    // inspector is registered the pipeline simply has nothing to probe, so
    // pre-flight passes (the service itself is registered in DI and the
    // step's inputs are present).
    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.That(result.IsSuccess, Is.True);
  }

  // ── (3) Inspector throws → wrapped, surfaces as a pre-flight failure ─

  [Test]
  public async Task InspectorThrows_FailureSurfacesAsPreflightStepFailed()
  {
    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
        throw new InvalidOperationException("simulated probe failure")
      );
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.Multiple(() =>
    {
      Assert.That(result.HasFailures, Is.True);
      var failed = result.FirstFailure!;
      Assert.That(failed.StepLabel, Is.EqualTo("preflight"),
        "Pre-flight failures collapse into a single 'preflight' StepResult.Failed.");
      Assert.That(failed.Error, Is.InstanceOf<RuntimeError.InvariantViolated>(),
        "The active pipeline currently wraps the aggregated pre-flight outcome in InvariantViolated.");
      Assert.That(failed.Error.Message, Does.Contain("simulated probe failure"),
        "The probe's exception message must reach the formatted pre-flight summary.");
    });
  }

  // ── (4) Probe runs once per registration ────────────────────────────

  [Test]
  public async Task InspectorRegistration_RunsOncePerRunAsyncRegardlessOfStepCount()
  {
    // The legacy fixture asserted that two steps sharing a service result
    // in one probe invocation. The active pipeline runs each registered
    // probe once per RunAsync (not once per step), so the invariant
    // "shared service => one probe call" still holds — for a stronger
    // reason than dispatch-loop deduplication.
    var probeCount = 0;
    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
      {
        Interlocked.Increment(ref probeCount);
        return Task.FromResult(Inspect.Pass());
      });
      RegisterChainedFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("chain");

    Assert.Multiple(() =>
    {
      Assert.That(result.IsSuccess, Is.True);
      Assert.That(probeCount, Is.EqualTo(1),
        "An inspector registration should fire exactly once per RunAsync, not once per step that depends on it.");
    });
  }

  // ── (5) Class-based IFlowServiceInspector resolves and runs ─────────

  [Test]
  public async Task ClassBasedInspector_RegisteredAsInstance_IsInvoked()
  {
    var instance = new ProbingFakeServiceInspector();
    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      b.AddFlowServiceInspector<IFakeService>(instance);
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.Multiple(() =>
    {
      Assert.That(result.IsSuccess, Is.True);
      Assert.That(instance.InvocationCount, Is.EqualTo(1),
        "An IFlowServiceInspector<T> instance registered via AddFlowServiceInspector should be invoked once.");
    });
  }

  // ── (6) Delegate inspector receives the resolved service instance ───

  [Test]
  public async Task DelegateInspector_ReceivesResolvedServiceInstance()
  {
    var fake = new FakeService();
    IFakeService? captured = null;

    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(fake);
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
      {
        captured = svc;
        return Task.FromResult(Inspect.Pass());
      });
      RegisterDoubleFlow(b);
    });

    await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.That(captured, Is.SameAs(fake),
      "The probe must receive the DI-resolved service instance, not a fresh / wrapped object.");
  }

  // ── (7) Two registrations for the same service both run ─────────────

  [Test]
  public async Task TwoInspectorsForSameService_BothInvoked()
  {
    // The legacy fixture asserted TryAdd-style override semantics on the
    // inspector registration (first wins, second is ignored). The active
    // FlowthruServiceBuilder appends registrations to a list — both run.
    // This test pins that observable behaviour so a future shift to
    // override semantics is a deliberate, test-visible decision.
    var firstRan = false;
    var secondRan = false;

    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
      {
        firstRan = true;
        return Task.FromResult(Inspect.Pass());
      });
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
      {
        secondRan = true;
        return Task.FromResult(Inspect.Pass());
      });
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.Multiple(() =>
    {
      Assert.That(result.IsSuccess, Is.True);
      Assert.That(firstRan, Is.True, "First registered inspector should run.");
      Assert.That(secondRan, Is.True,
        "Second registered inspector should also run — registrations are additive, not override.");
    });
  }

  // ── (8) Flow with no service dependencies / inspectors runs cleanly ──

  [Test]
  public async Task NoInspectorsRegistered_PreflightSkipsServiceInspectionLayer()
  {
    // The "no probe runs when nothing is registered" invariant — pre-flight
    // must not invent inspectors out of thin air, and must still pass for
    // a flow whose inputs are present and whose steps declare no deps.
    await using var sp = BuildHost(b =>
    {
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.That(result.IsSuccess, Is.True);
  }

  // ── (9) Inspector returns Fail → pre-flight reports failure ─────────

  [Test]
  public async Task InspectorReturnsFail_PreflightReportsInspectionFailure()
  {
    await using var sp = BuildHost(b =>
    {
      b.Services.AddSingleton<IFakeService>(new FakeService());
      b.AddFlowServiceInspector<IFakeService>((svc, ct) =>
        Task.FromResult(Inspect.Fail("service is unreachable", source: "IFakeService"))
      );
      RegisterDoubleFlow(b);
    });

    var result = await sp.GetRequiredService<IFlowthruService>().RunAsync("svc");

    Assert.Multiple(() =>
    {
      Assert.That(result.HasFailures, Is.True);
      var failed = result.FirstFailure!;
      Assert.That(failed.StepLabel, Is.EqualTo("preflight"));
      Assert.That(failed.Error, Is.InstanceOf<RuntimeError.InvariantViolated>());
      Assert.That(failed.Error.Message, Does.Contain("unreachable"),
        "Inspect.Fail's message must reach the aggregated pre-flight summary.");
    });
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static ServiceProvider BuildHost(Action<IFlowthruBuilder> configure)
  {
    var services = new ServiceCollection();
    services.AddFlowthru(configure);
    return services.BuildServiceProvider();
  }

  /// <summary>Registers a single-step flow with the input pre-saved so adapter pre-flight passes.</summary>
  private static void RegisterDoubleFlow(IFlowthruBuilder b)
  {
    b.RegisterCatalog(_ => new CountingCatalog());
    b.RegisterFlow<CountingCatalog>("svc", catalog =>
    {
      catalog.Input.Save(21).Run().GetAwaiter().GetResult();
      return FlowBuilder.CreateFlow("svc", p =>
        p.AddStep<int, int>("double", x => x * 2, catalog.Input, catalog.Output)
      );
    });
  }

  /// <summary>Two chained steps that both depend on the same conceptual service.</summary>
  private static void RegisterChainedFlow(IFlowthruBuilder b)
  {
    var stage1 = ItemFactory.Singleton.Memory<int>("svc-chain-1");
    var stage2 = ItemFactory.Singleton.Memory<int>("svc-chain-2");
    var stage3 = ItemFactory.Singleton.Memory<int>("svc-chain-3");
    stage1.Save(7).Run().GetAwaiter().GetResult();

    b.RegisterCatalog(_ => new CountingCatalog());
    b.RegisterFlow("chain", () => FlowBuilder.CreateFlow("chain", p =>
    {
      p.AddStep<int, int>("first", x => x + 1, stage1, stage2);
      p.AddStep<int, int>("second", x => x * 2, stage2, stage3);
    }));
  }
}
