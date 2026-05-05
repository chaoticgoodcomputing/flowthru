using Flowthru.Core.Data.Validation;
using Flowthru.Extensions.Python.Services;

namespace Flowthru.Extensions.Python.Execution;

/// <summary>
/// Abstraction for executing Python code.
/// </summary>
/// <remarks>
/// <para>
/// Decouples the execution strategy from Python step wiring.
/// Two implementations ship out of the box:
/// <list type="bullet">
/// <item><see cref="PythonNetExecutor"/> — in-process via Python.NET (opt-in)</item>
/// <item><see cref="SubprocessPythonExecutor"/> — isolated child process per service (default)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Isolation contract:</strong>
/// Two FlowthruServices using <see cref="SubprocessPythonExecutor"/> do not share Python state.
/// Each executor spawns its own Python worker process with its own venv, <c>sys.path</c>,
/// <c>sys.modules</c>, and GIL — complete isolation at the cost of IPC marshalling overhead.
/// </para>
/// <para>
/// All implementations must handle:
/// <list type="bullet">
/// <item>Module import and caching</item>
/// <item>Function resolution and invocation</item>
/// <item>Argument marshalling (C# ↔ Python) — scalar, tabular (Arrow IPC), and raw bytes</item>
/// <item>Error propagation (Python exceptions → <see cref="InvalidOperationException"/>)</item>
/// <item>Sidecar inspector invocation — see <see cref="InvokeInspector"/></item>
/// </list>
/// </para>
/// </remarks>
public interface IPythonExecutor
{
  /// <summary>
  /// Invokes a Python function, marshalling input and output to/from C# types.
  /// </summary>
  /// <typeparam name="TInput">
  /// C# input type. May be a scalar, <c>IEnumerable&lt;TSchema&gt;</c> (tabular),
  /// <c>byte[]</c> (raw bytes), or a <c>ValueTuple</c> of any of those (multi-input).
  /// </typeparam>
  /// <typeparam name="TOutput">
  /// C# output type. Same range as <typeparamref name="TInput"/>.
  /// </typeparam>
  /// <param name="moduleName">
  /// Dotted module name (e.g., <c>"Flows.DataScience.train_model"</c>).
  /// Must be resolvable via the executor's configured <c>sys.path</c>.
  /// </param>
  /// <param name="functionName">Python function name within the module.</param>
  /// <param name="input">Typed input value.</param>
  /// <returns>Typed output value returned by the Python function.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if the module cannot be imported, the function cannot be resolved,
  /// or marshalling fails.
  /// </exception>
  TOutput Invoke<TInput, TOutput>(string moduleName, string functionName, TInput input);

  /// <summary>
  /// Validates that a Python step exists and satisfies Flowthru's <c>@step</c> contract,
  /// returning metadata extracted from the decorator.
  /// </summary>
  /// <param name="moduleName">Dotted Python module name.</param>
  /// <param name="functionName">Python function name within the module.</param>
  /// <returns>
  /// Metadata read from the <c>@step</c> decorator — currently the list of declared
  /// service dependencies (<c>__flowthru_services__</c>). Returns
  /// <see cref="PythonStepMetadata.Empty"/> when the step declares no services.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown if the module is not importable, the function is missing, or the
  /// <c>@step</c> decorator is absent.
  /// </exception>
  PythonStepMetadata ValidateStep(string moduleName, string functionName);

  /// <summary>
  /// Runs a sidecar inspector against a freshly-constructed instance of the
  /// declared Python service. The C# preflight loop dispatches one
  /// <c>InvokeInspector</c> call per registered Python service before any
  /// step in the flow executes.
  /// </summary>
  /// <param name="registration">
  /// The service ↔ inspector linkage from
  /// <see cref="IPythonServiceInspectorRegistry"/>.
  /// </param>
  /// <returns>
  /// The inspector's <see cref="ValidationResult"/>. The Python side returns
  /// a <c>flowthru.ValidationResult</c> dataclass; this method translates
  /// the wire-format payload into the C# domain type.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when the service module/class or inspector module/function
  /// cannot be resolved, when service construction raises, or when the
  /// inspector returns something other than a <c>ValidationResult</c>.
  /// </exception>
  /// <remarks>
  /// The Python service class is constructed with no arguments — service
  /// configuration is expected to flow in via env vars (the
  /// <c>flowthru.config</c> bridge). Inspector calls reuse the same
  /// long-lived subprocess that step invocations use; the constructed
  /// service instance is cached in the worker's module-cache and lives
  /// for the worker's lifetime.
  /// </remarks>
  ValidationResult InvokeInspector(PythonServiceRegistration registration);
}
