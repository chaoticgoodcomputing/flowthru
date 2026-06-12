using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Step.Python.Internal;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the routing logic in <see cref="PythonServiceDependencyDispatcher"/>:
/// <list type="bullet">
///   <item>Category is the literal "python".</item>
///   <item>Non-PythonServiceDependency inputs fail with a typed error.</item>
///   <item>Registered services delegate to the executor's InvokeInspector.</item>
///   <item>Unregistered services return a successful Validated (non-fatal,
///     mirroring the C#-side IFlowServiceInspector resolution).</item>
/// </list>
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonServiceDependencyDispatcherTests
{
  private static PythonServiceDependencyDispatcher Build(
    PythonRuntimeOptions opts,
    IPythonExecutor executor
  )
  {
    var registry = new PythonServiceInspectorRegistry(Options.Create(opts));
    return new PythonServiceDependencyDispatcher(
      registry,
      executor,
      NullLogger<PythonServiceDependencyDispatcher>.Instance
    );
  }

  [Test]
  public void Category_IsPython()
  {
    var dispatcher = Build(new PythonRuntimeOptions(), new RecordingExecutor());
    Assert.That(dispatcher.Category, Is.EqualTo("python"));
  }

  [Test]
  public void Constructor_NullArgs_Throw()
  {
    var registry = new PythonServiceInspectorRegistry(Options.Create(new PythonRuntimeOptions()));
    var executor = new RecordingExecutor();
    var logger = NullLogger<PythonServiceDependencyDispatcher>.Instance;

    Assert.That(
      () => new PythonServiceDependencyDispatcher(null!, executor, logger),
      Throws.TypeOf<ArgumentNullException>()
    );
    Assert.That(
      () => new PythonServiceDependencyDispatcher(registry, null!, logger),
      Throws.TypeOf<ArgumentNullException>()
    );
    Assert.That(
      () => new PythonServiceDependencyDispatcher(registry, executor, null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public async Task Inspect_NonPythonServiceDependency_FailsWithTypedError()
  {
    var dispatcher = Build(new PythonRuntimeOptions(), new RecordingExecutor());
    var alienRef = new AlienServiceDependency("alien.svc");

    var io = dispatcher.Inspect(alienRef);
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.External>());
  }

  [Test]
  public async Task Inspect_UnregisteredService_ReturnsValidNonFatal()
  {
    var executor = new RecordingExecutor();
    var dispatcher = Build(new PythonRuntimeOptions(), executor);
    var serviceRef = new PythonServiceDependency("Services.Unregistered");

    var io = dispatcher.Inspect(serviceRef);
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
    Assert.That(executor.InvokeCalls, Is.Empty,
      "Unregistered services should not delegate to the executor — non-fatal early return.");
  }

  [Test]
  public async Task Inspect_RegisteredService_DelegatesToExecutor()
  {
    var opts = new PythonRuntimeOptions();
    opts.RegisterService("Services.X", svc => svc.WithInspector("Services.x_inspector"));

    var executor = new RecordingExecutor();
    var dispatcher = Build(opts, executor);
    var serviceRef = new PythonServiceDependency("Services.X");

    var io = dispatcher.Inspect(serviceRef);
    var result = await io.Run();
    Assert.That(result, Is.InstanceOf<EffResult<Validated<PreFlightError, FlowUnit>>.Success>());

    Assert.That(executor.InvokeCalls, Has.Count.EqualTo(1));
    Assert.That(executor.InvokeCalls[0].ServiceClassPath, Is.EqualTo("Services.X"));
    Assert.That(executor.InvokeCalls[0].InspectorModule,
      Is.EqualTo("Services.x_inspector"));
  }

  // ── Helpers ─────────────────────────────────────────────────────────

  private sealed record AlienServiceDependency(string DagId) : IExtensionServiceDependency
  {
    public string DisplayName => DagId;
    public string Category => "alien";
  }

  private sealed class RecordingExecutor : IPythonExecutor
  {
    public List<PythonServiceRegistration> InvokeCalls { get; } = new();

    public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
      PythonServiceRegistration registration
    )
    {
      InvokeCalls.Add(registration);
      return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
    }

    public FlowIO<TOutput> Invoke<TInput, TOutput>(
      string moduleName,
      string functionName,
      TInput input
    ) => FlowIO.Fail<TOutput>(
      new RuntimeError.InvariantViolated(
        "RecordingExecutor", "Invoke not used in dispatcher tests"
      )
    );

    public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName) =>
      FlowIO.Fail<PythonStepMetadata>(
        new RuntimeError.InvariantViolated(
          "RecordingExecutor", "ValidateStep not used in dispatcher tests"
        )
      );
  }
}
