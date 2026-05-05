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
/// Tests for the preflight loop's dispatch on non-<see cref="ServiceRef.CSharp"/>
/// variants via registered <see cref="IServiceRefDispatcher"/> implementations.
/// Mirrors <see cref="ServiceInspectionTests"/> in shape but exercises the
/// extension-provided dispatcher path rather than the C#-side
/// <see cref="IFlowthruInspector{TService}"/> path.
/// </summary>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
[Category("ServiceInspection")]
public class ServiceRefDispatcherTests
{
  // ── Dispatcher invoked once per unique ref ──────────────────────────

  [Test]
  public async Task PythonRef_RegisteredDispatcher_IsInvoked()
  {
    var dispatcher = new RecordingDispatcher();
    var sp = BuildServiceProvider(dispatcher);
    var flow = BuildFlowWithPythonRef(sp, "Services.X.Y");

    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(dispatcher.InvokeCount, Is.EqualTo(1));
      Assert.That(
        ((ServiceRef.Python)dispatcher.LastRef!).ClassPath,
        Is.EqualTo("Services.X.Y")
      );
    });
  }

  [Test]
  public async Task PythonRef_TwoStepsSharingService_DispatcherInvokedOnce()
  {
    // ServiceRef has value equality — two steps declaring the same Python
    // ref collapse to one entry in the preflight loop's GroupBy. The
    // dispatcher should fire exactly once per unique ref, not once per step.
    var dispatcher = new RecordingDispatcher();
    var sp = BuildServiceProvider(dispatcher);
    var flow = BuildFlowWithTwoStepsSharingPythonRef(sp, "Services.Shared");

    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(
        dispatcher.InvokeCount,
        Is.EqualTo(1),
        "dispatcher should run once per unique ref, not once per step"
      );
    });
  }

  // ── Dispatcher returns failure → run aborts ─────────────────────────

  [Test]
  public async Task PythonRef_DispatcherReturnsFailure_PreflightFails()
  {
    var dispatcher = new RecordingDispatcher
    {
      ResultToReturn = ValidationResult.Failure(
        catalogKey: "Services.X.Y",
        errorType: ValidationErrorType.InspectionFailure,
        message: "simulated python preflight failure"
      ),
    };
    var sp = BuildServiceProvider(dispatcher);
    var flow = BuildFlowWithPythonRef(sp, "Services.X.Y");

    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors.Single().Message, Is.EqualTo("simulated python preflight failure"));
    });
  }

  // ── Dispatcher throws → wrapped via FromException ───────────────────

  [Test]
  public async Task PythonRef_DispatcherThrows_FailureWrappedViaFromException()
  {
    // Throwing dispatchers are test bugs in principle, but the loop must
    // wrap exceptions rather than letting them escape — otherwise a
    // misbehaving extension takes down the whole flow.
    var dispatcher = new RecordingDispatcher
    {
      Behavior = DispatcherBehavior.Throw,
    };
    var sp = BuildServiceProvider(dispatcher);
    var flow = BuildFlowWithPythonRef(sp, "Services.X.Y");

    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.False);
      Assert.That(result.Errors, Has.Count.EqualTo(1));
      Assert.That(result.Errors.Single().Message, Does.Contain("simulated dispatcher failure"));
    });
  }

  // ── No dispatcher registered → non-fatal warning ────────────────────

  [Test]
  public async Task PythonRef_NoDispatcherRegistered_ReturnsSuccess()
  {
    // Mirrors the C# side's missing-IFlowthruInspector behaviour: when no
    // dispatcher knows how to handle a variant, the loop logs a warning
    // and continues. Halting on every unknown variant would make the
    // framework brittle in the face of partial extension configuration.
    var sp = BuildServiceProvider(/* no dispatcher */ null);
    var flow = BuildFlowWithPythonRef(sp, "Services.NobodyHandlesMe");

    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.That(result.IsValid, Is.True);
  }

  // ── CSharp ref still works alongside dispatchers ────────────────────

  [Test]
  public async Task CSharpRef_WithPythonDispatcherRegistered_StillUsesIFlowthruInspector()
  {
    // The dispatcher path is only consulted for non-CSharp variants. A
    // mixed flow (CSharp + Python) must route each ref through its own
    // dispatch path — this test pins that the CSharp path doesn't
    // accidentally fall through to the dispatcher.
    var probeRan = false;
    var dispatcher = new RecordingDispatcher();
    var services = new ServiceCollection();
    services.AddSingleton<IServiceRefDispatcher>(dispatcher);
    services.AddSingleton<IFakeService>(new FakeService());
    services.AddFlowthruInspect<IFakeService>(
      (IFakeService svc, CancellationToken ct) =>
      {
        probeRan = true;
        return FlowIO.Pure(ValidationResult.Success());
      }
    );
    var sp = services.BuildServiceProvider();

    var flow = BuildFlowWithCSharpRef<IFakeService>(sp);
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    Assert.Multiple(() =>
    {
      Assert.That(result.IsValid, Is.True);
      Assert.That(probeRan, Is.True, "C# inspector should run");
      Assert.That(dispatcher.InvokeCount, Is.Zero, "dispatcher should not be consulted for C# refs");
    });
  }

  // ── Test helpers ────────────────────────────────────────────────────

  public interface IFakeService { }

  public sealed class FakeService : IFakeService { }

  private enum DispatcherBehavior { Normal, Throw }

  /// <summary>
  /// Test-only <see cref="IServiceRefDispatcher"/> that handles
  /// <see cref="ServiceRef.Python"/> variants and records its invocations.
  /// </summary>
  private sealed class RecordingDispatcher : IServiceRefDispatcher
  {
    public ValidationResult ResultToReturn { get; set; } = ValidationResult.Success();
    public DispatcherBehavior Behavior { get; set; } = DispatcherBehavior.Normal;

    public int InvokeCount { get; private set; }
    public ServiceRef? LastRef { get; private set; }

    public bool CanHandle(ServiceRef serviceRef) => serviceRef is ServiceRef.Python;

    public Task<ValidationResult> InspectAsync(ServiceRef serviceRef, CancellationToken ct)
    {
      InvokeCount++;
      LastRef = serviceRef;

      if (Behavior == DispatcherBehavior.Throw)
      {
        throw new InvalidOperationException("simulated dispatcher failure");
      }

      return Task.FromResult(ResultToReturn);
    }
  }

  private static IServiceProvider BuildServiceProvider(RecordingDispatcher? dispatcher)
  {
    var services = new ServiceCollection();
    if (dispatcher is not null)
    {
      services.AddSingleton<IServiceRefDispatcher>(dispatcher);
    }
    return services.BuildServiceProvider();
  }

  private static Flow BuildFlowWithPythonRef(IServiceProvider sp, string classPath)
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
        label: "PythonStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.Input },
        outputs: new INode[] { catalog.Output },
        serviceDependencies: new ServiceRef[] { ServiceRef.OfPython(classPath) }
      )
    );

    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();
    return flow;
  }

  private static Flow BuildFlowWithTwoStepsSharingPythonRef(
    IServiceProvider sp,
    string classPath
  )
  {
    var catalog = new SimpleThreeStepCatalog();
    var flow = new Flow { ServiceProvider = sp };

    Func<IEnumerable<TestData>, Task<IEnumerable<TestData>>> transform = async input =>
    {
      await Task.Yield();
      return input;
    };

    var sharedRef = ServiceRef.OfPython(classPath);

    flow.AddStep(
      new FlowStep(
        label: "FirstStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.Input },
        outputs: new INode[] { catalog.StepOne },
        serviceDependencies: new ServiceRef[] { sharedRef }
      )
    );

    flow.AddStep(
      new FlowStep(
        label: "SecondStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.StepOne },
        outputs: new INode[] { catalog.Output },
        serviceDependencies: new ServiceRef[] { sharedRef }
      )
    );

    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();
    return flow;
  }

  private static Flow BuildFlowWithCSharpRef<TService>(IServiceProvider sp)
    where TService : class
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
        label: "CSharpStep",
        description: null,
        step: transform,
        inputs: new INode[] { catalog.Input },
        outputs: new INode[] { catalog.Output },
        serviceDependencies: new ServiceRef[] { ServiceRef.Of<TService>() }
      )
    );

    flow.ValidationOptions.Inspect(catalog.Input, InspectionLevel.None);
    flow.Build();
    return flow;
  }
}
