using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins the <see cref="PythonStepValidationHook"/> behaviour against
/// flows containing zero, one, or several <see cref="PythonStep{TIn, TOut}"/>
/// instances. The hook walks every step, invokes the executor's
/// <c>ValidateStep</c>, and either translates the failure or checks
/// schema agreement against the C# generic type names.
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonStepValidationHookTests
{
  // ── Probe schemas with predictable type names ───────────────────────

  public sealed record ProbeIn { public required int X { get; init; } }
  public sealed record ProbeOut { public required int Y { get; init; } }

  // ── Helpers ─────────────────────────────────────────────────────────

  private static BuiltFlow BuildPythonFlow(
    IPythonExecutor executor,
    string module = "demo",
    string function = "step"
  )
  {
    var input = ItemFactory.Singleton.Memory<ProbeIn>("input");
    var output = ItemFactory.Singleton.Memory<ProbeOut>("output");

    return FlowBuilder.CreateFlow("python-test", b =>
      b.AddPythonStep<ProbeIn, ProbeOut>(
        label: "step",
        module: module,
        function: function,
        input: input,
        output: output,
        executor: executor
      )
    );
  }

  private static BuiltFlow BuildEmptyFlow() =>
    FlowBuilder.CreateFlow("empty", b =>
    {
      var input = ItemFactory.Singleton.Memory<int>("input");
      var output = ItemFactory.Singleton.Memory<int>("output");
      b.AddStep<int, int>("plain", x => x + 1, input, output);
    });

  // ── Constructor ─────────────────────────────────────────────────────

  [Test]
  public void Constructor_NullExecutor_Throws()
  {
    Assert.That(
      () => new PythonStepValidationHook(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  [Test]
  public void HookId_IsStable()
  {
    var hook = new PythonStepValidationHook(new RecordingExecutor());
    Assert.That(hook.HookId, Is.EqualTo("python.step-shape"));
  }

  [Test]
  public void Validate_NullFlow_Throws()
  {
    var hook = new PythonStepValidationHook(new RecordingExecutor());
    Assert.That(
      () => hook.Validate(null!),
      Throws.TypeOf<ArgumentNullException>()
    );
  }

  // ── Empty flow / no Python steps ────────────────────────────────────

  [Test]
  public async Task Validate_FlowWithoutPythonSteps_PassesAndDoesNotInvokeExecutor()
  {
    var executor = new RecordingExecutor();
    var hook = new PythonStepValidationHook(executor);

    var io = hook.Validate(BuildEmptyFlow());
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
    Assert.That(executor.ValidateCalls, Is.Empty,
      "No Python steps means no executor ValidateStep calls.");
  }

  // ── Happy path ──────────────────────────────────────────────────────

  [Test]
  public async Task Validate_PythonStepWithMatchingSchemas_Passes()
  {
    var executor = new RecordingExecutor
    {
      ValidateStepResult = FlowIO.Pure(new PythonStepMetadata(
        Inputs: new[] { "ProbeIn" },
        Outputs: new[] { "ProbeOut" },
        Services: Array.Empty<string>()
      ))
    };
    var hook = new PythonStepValidationHook(executor);

    var io = hook.Validate(BuildPythonFlow(executor));
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Valid>());
    Assert.That(executor.ValidateCalls, Has.Count.EqualTo(1));
    Assert.That(executor.ValidateCalls[0], Is.EqualTo(("demo", "step")));
  }

  // ── Failure paths ───────────────────────────────────────────────────

  [Test]
  public async Task Validate_ExecutorFailure_TranslatesToInspectionFailedExternal()
  {
    var executor = new RecordingExecutor
    {
      ValidateStepResult = FlowIO.Fail<PythonStepMetadata>(
        new RuntimeError.External("decoder", new InvalidOperationException("module not found"))
      )
    };
    var hook = new PythonStepValidationHook(executor);

    var io = hook.Validate(BuildPythonFlow(executor));
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)validated;
    Assert.That(invalid.Errors[0], Is.InstanceOf<PreFlightError.External>());
  }

  [Test]
  public async Task Validate_SchemaMismatch_FailsWithExternalPythonError()
  {
    // Decorator says inputs=["WrongInput"], but C# step is typed
    // as PythonStep<ProbeIn, ProbeOut>. The hook should flag this.
    var executor = new RecordingExecutor
    {
      ValidateStepResult = FlowIO.Pure(new PythonStepMetadata(
        Inputs: new[] { "WrongInput" },
        Outputs: new[] { "ProbeOut" },
        Services: Array.Empty<string>()
      ))
    };
    var hook = new PythonStepValidationHook(executor);

    var io = hook.Validate(BuildPythonFlow(executor));
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
  }

  [Test]
  public async Task Validate_ArityMismatch_FailsWithExternalPythonError()
  {
    // C# step has 1 input (ProbeIn); decorator declares 2 inputs.
    var executor = new RecordingExecutor
    {
      ValidateStepResult = FlowIO.Pure(new PythonStepMetadata(
        Inputs: new[] { "ProbeIn", "ProbeIn" },
        Outputs: new[] { "ProbeOut" },
        Services: Array.Empty<string>()
      ))
    };
    var hook = new PythonStepValidationHook(executor);

    var io = hook.Validate(BuildPythonFlow(executor));
    var result = await io.Run();
    var validated = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    Assert.That(validated, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
  }

  // ── Fake IPythonExecutor that records ValidateStep calls ────────────

  private sealed class RecordingExecutor : IPythonExecutor
  {
    public List<(string Module, string Function)> ValidateCalls { get; } = new();
    public FlowIO<PythonStepMetadata> ValidateStepResult { get; set; } =
      FlowIO.Pure(PythonStepMetadata.Empty);

    public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName)
    {
      ValidateCalls.Add((moduleName, functionName));
      return ValidateStepResult;
    }

    public FlowIO<TOutput> Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input) =>
      FlowIO.Fail<TOutput>(new RuntimeError.InvariantViolated(
        "RecordingExecutor", "Invoke not used in validation-hook tests"
      ));

    public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
      PythonServiceRegistration registration
    ) => FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
  }
}
