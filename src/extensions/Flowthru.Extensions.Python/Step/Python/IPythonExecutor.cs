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
/// One implementation ships: <c>SubprocessPythonExecutor</c>. It spawns one
/// child Python process per executor instance for full OS-level isolation,
/// using JSON over stdin/stdout for control + Apache Arrow IPC for tabular
/// data. Custom executors (in-process Python.NET, FFI, RPC over network,
/// etc.) can be registered against this interface via DI.
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

  /// <summary>
  /// Probe the configured interpreter for its version string and cache
  /// the result. Used to fold interpreter identity into the
  /// <c>CodeVersion</c> of <c>@step(cacheable=True)</c> Python steps so
  /// changing the interpreter invalidates the cache. Returns null when
  /// the interpreter cannot be invoked (missing venv, broken Python
  /// installation, etc.) — downstream cache logic treats null as
  /// "uncacheable", preserving the fail-safe-cache-miss contract.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The probe runs <c>python --version</c> in a short-lived subprocess
  /// (not the long-running worker) and captures the output. The first
  /// call performs the probe; subsequent calls return the cached
  /// string. Implementations should make this thread-safe — multiple
  /// <c>AddPythonStep</c> calls can race during flow registration.
  /// </para>
  /// <para>
  /// The default implementation returns null, matching the fail-safe
  /// "uncacheable" contract — test doubles and lightweight executors
  /// can opt out of caching support without explicit code.
  /// </para>
  /// </remarks>
  string? GetInterpreterVersion() => null;
}
