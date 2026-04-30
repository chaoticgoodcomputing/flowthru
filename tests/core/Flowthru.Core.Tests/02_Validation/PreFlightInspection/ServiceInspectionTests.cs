using Flowthru.Core.Data;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Flows;
using Flowthru.Core.Graph;
using Flowthru.Core.Services;
using Flowthru.Core.Tests.Fixtures.TestCatalogs;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Tests for Phase 3 of the effects-as-steps initiative: preflight inspection of step
/// service dependencies via DI-registered <see cref="IFlowthruInspector{TService}"/>
/// sidecars. Tests construct <see cref="FlowStep"/> directly with explicit
/// <see cref="FlowStep.ServiceDependencies"/> lists; Phase 4 will source-generate
/// these for ordinary <c>FlowBuilder.AddStep</c> construction.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
[Category("ServiceInspection")]
public class ServiceInspectionTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // (1) Inspector registered → invoked, result reported
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task StepWithService_InspectorRegistered_ReturnsSuccess()
  {
    var probeCount = 0;
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
    {
      probeCount++;
      return FlowIO.Pure(ValidationResult.Success());
    });
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(probeCount, Is.EqualTo(1), "inspector should run exactly once");
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (2) Service registered, no inspector → success + warning logged
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task StepWithService_NoInspectorRegistered_ReturnsSuccessWithoutFailure()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    // No inspector registered is a non-fatal warning condition; preflight succeeds.
    Assert.That(result.IsValid, Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (3) Inspector throws → wrapped via ValidationResult.FromException
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task StepWithService_InspectorThrows_ReturnsFailureWithException()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
      FlowIO.LiftAsync<ValidationResult>(
        (CancellationToken inner) =>
          throw new InvalidOperationException("simulated probe failure")
      )
    );
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors, Has.Count.EqualTo(1));
      Assert.That(result.Errors[0].Message, Does.Contain("simulated probe failure"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (4) Two steps share a service → inspector called once (idempotency)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task TwoStepsShareService_InspectorCalledOnce()
  {
    var probeCount = 0;
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
    {
      probeCount++;
      return FlowIO.Pure(ValidationResult.Success());
    });
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithTwoStepsSharingService<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(probeCount, Is.EqualTo(1), "shared service must be probed exactly once");
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (5) Class-based inspector via AddFlowthruInspect<TService, TInspector>
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ClassBasedInspector_ResolvedAndInvoked()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService, ProbingFakeServiceInspector>();
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(ProbingFakeServiceInspector.InvocationCount, Is.GreaterThan(0));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (6) Delegate inspector via AddFlowthruInspect<TService>(probe)
  //     (covered by tests (1)-(4); this is the explicit assertion)
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task DelegateInspector_ResolvedAndInvoked()
  {
    var captured = (object?)null;
    var services = new ServiceCollection();
    var instance = new FakeService();
    services.AddSingleton<IFakeService>(instance);
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
    {
      captured = svc;
      return FlowIO.Pure(ValidationResult.Success());
    });
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.That(captured, Is.SameAs(instance), "inspector receives the resolved service instance");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (7) User override wins via TryAddSingleton semantics
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task UserOverride_WinsRegardlessOfRegistrationOrder()
  {
    var defaultRan = false;
    var overrideRan = false;
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());

    // User registers their override FIRST.
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
    {
      overrideRan = true;
      return FlowIO.Pure(ValidationResult.Success());
    });

    // Extension default registered AFTER — should be ignored due to TryAdd.
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
    {
      defaultRan = true;
      return FlowIO.Pure(ValidationResult.Success());
    });

    var sp = services.BuildServiceProvider();
    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(overrideRan, Is.True, "first registration should run (TryAdd preserves it)");
      Assert.That(defaultRan, Is.False, "second registration should be ignored");
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (8) Step with empty ServiceDependencies → preflight skips entirely
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task StepWithEmptyServiceDependencies_PreflightSkipsServiceInspection()
  {
    var probeCount = 0;
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
    {
      probeCount++;
      return FlowIO.Pure(ValidationResult.Success());
    });
    var sp = services.BuildServiceProvider();

    // Build a flow where the step has NO declared service dependencies.
    var flow = BuildFlowWithoutServiceDep(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(probeCount, Is.EqualTo(0), "inspector should not run when no step declares the dep");
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (9) Inspector returns failure → preflight aggregate reports failure
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task InspectorReturnsFailure_PreflightReportsFailure()
  {
    var services = new ServiceCollection();
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService>((IFakeService svc, CancellationToken ct) =>
      FlowIO.Pure(
        ValidationResult.Failure(
          catalogKey: "IFakeService",
          errorType: ValidationErrorType.NotFound,
          message: "service is unreachable"
        )
      )
    );
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithServiceDep<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors, Has.Count.EqualTo(1));
      Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.NotFound));
      Assert.That(result.Errors[0].Message, Does.Contain("unreachable"));
    });
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Test helpers
  // ─────────────────────────────────────────────────────────────────────────

  // Builds a flow with one passthrough step that declares a single service dep.
  // The transform itself doesn't actually use the service — the test exercises
  // the inspection path via FlowStep.ServiceDependencies, not closure-captured state.
  private static Flow BuildFlowWithServiceDep<TService>(IServiceProvider sp)
  {
    var catalog = new SimpleThreeStepCatalog();
    var flow = new Flow { ServiceProvider = sp };

    // Construct FlowStep directly so we can declare ServiceDependencies.
    Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> transform = async input =>
    {
      await Task.Yield();
      return input;
    };

    flow.AddStep(
      new FlowStep(
        label: "ServiceDepStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.Input },
        outputs: new INode[] { catalog.Output },
        serviceDependencies: new[] { typeof(TService) }
      )
    );

    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();
    return flow;
  }

  private static Flow BuildFlowWithTwoStepsSharingService<TService>(IServiceProvider sp)
  {
    var catalog = new SimpleThreeStepCatalog();
    var flow = new Flow { ServiceProvider = sp };

    Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> transform = async input =>
    {
      await Task.Yield();
      return input;
    };

    flow.AddStep(
      new FlowStep(
        label: "FirstStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.Input },
        outputs: new INode[] { catalog.StepOne },
        serviceDependencies: new[] { typeof(TService) }
      )
    );

    flow.AddStep(
      new FlowStep(
        label: "SecondStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.StepOne },
        outputs: new INode[] { catalog.Output },
        serviceDependencies: new[] { typeof(TService) }
      )
    );

    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();
    return flow;
  }

  private static Flow BuildFlowWithoutServiceDep(IServiceProvider sp)
  {
    var catalog = new SimpleThreeStepCatalog();
    var flow = new Flow { ServiceProvider = sp };

    Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> transform = async input =>
    {
      await Task.Yield();
      return input;
    };

    flow.AddStep(
      new FlowStep(
        label: "NoServiceDepStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.Input },
        outputs: new INode[] { catalog.Output }
      )
    );

    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();
    return flow;
  }

  // Test-only fakes
  public interface IFakeService { }

  public sealed class FakeService : IFakeService { }

  public sealed class ProbingFakeServiceInspector : IFlowthruInspector<IFakeService>
  {
    public static int InvocationCount;

    public FlowIO<ValidationResult> InspectAsync(IFakeService service, CancellationToken ct = default)
    {
      System.Threading.Interlocked.Increment(ref InvocationCount);
      return FlowIO.Pure(ValidationResult.Success());
    }
  }
}
