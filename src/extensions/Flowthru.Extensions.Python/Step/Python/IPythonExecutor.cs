using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime.Python;

namespace Flowthru.Step.Python;

/// <summary>
/// Abstraction for executing Python code from a Flowthru pipeline.
/// All operations return <see cref="FlowIO{A}"/> — Python failures
/// surface as typed
/// <see cref="Flowthru.Validation.Runtime.RuntimeError.ExtensionError"/>
/// values wrapping <see cref="PythonRuntimeError"/>, never as thrown
/// exceptions.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Two implementations ship.</strong>
/// <list type="bullet">
///   <item>
///     <c>SubprocessPythonExecutor</c> — default. Spawns one child
///     Python process per executor instance; full OS-level isolation.
///     JSON over stdin/stdout, Apache Arrow IPC for tabular data.
///   </item>
///   <item>
///     <c>PythonNetExecutor</c> — opt-in via
///     <c>UsePython(o => o.ExecutionMode = InProcess)</c>. In-process
///     interpreter via Python.NET; lower marshalling overhead at the
///     cost of GIL-shared interpreter state across all executors.
///   </item>
/// </list>
/// </para>
/// </remarks>
public interface IPythonExecutor
{
  /// <summary>
  /// Invoke a Python function, marshalling input and output between
  /// C# and Python. Returns a <see cref="FlowIO{A}"/> that yields the
  /// typed output on success, or a typed
  /// <see cref="PythonRuntimeError"/>-wrapped failure.
  /// </summary>
  /// <typeparam name="TInput">
  /// C# input type. Scalar, <c>IEnumerable&lt;TSchema&gt;</c> (tabular),
  /// <c>byte[]</c> (raw), or a value tuple of those (multi-input).
  /// </typeparam>
  /// <typeparam name="TOutput">
  /// C# output type. Same range as <typeparamref name="TInput"/>.
  /// </typeparam>
  /// <param name="moduleName">
  /// Dotted Python module name resolvable via the executor's configured
  /// <c>sys.path</c> (e.g. <c>"Flows.DataScience.Steps.train_model"</c>).
  /// </param>
  /// <param name="functionName">Function name within the module.</param>
  /// <param name="input">Typed input value.</param>
  FlowIO<TOutput> Invoke<TInput, TOutput>(
    string moduleName,
    string functionName,
    TInput input
  );

  /// <summary>
  /// Validate that a Python step exists, satisfies the <c>@step</c>
  /// contract, and return its decorator-derived metadata. Called once
  /// at flow-construction time to fail fast on missing modules,
  /// missing functions, or missing decorators.
  /// </summary>
  FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName);

  /// <summary>
  /// Run a Python service's sidecar inspector against a freshly-constructed
  /// instance of the service class. Returns the dispatcher-shaped
  /// <see cref="Validated{TError, TValue}"/> directly — accumulated
  /// failures per service-instance go straight into the pre-flight
  /// pipeline.
  /// </summary>
  FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
    Step.Python.PythonServiceRegistration registration
  );
}
