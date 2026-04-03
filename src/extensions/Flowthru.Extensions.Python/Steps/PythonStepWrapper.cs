using Flowthru.Extensions.Python.Execution;

namespace Flowthru.Extensions.Python.Steps;

/// <summary>
/// Thin wrapper that binds an <see cref="IPythonExecutor"/> to a specific module/function pair,
/// exposing it as a typed <c>Func&lt;TInput, TOutput&gt;</c> for use with the Flow builder.
/// </summary>
/// <remarks>
/// All marshalling (scalar, tabular, bytes, multi-I/O tuples) is delegated to the executor.
/// </remarks>
public sealed class PythonStepWrapper<TInput, TOutput>
{
  private readonly IPythonExecutor _executor;
  private readonly string _moduleName;
  private readonly string _functionName;

  /// <summary>
  /// Initializes the wrapper with the executor and step information.
  /// </summary>
  /// <param name="executor"></param>
  /// <param name="moduleName"></param>
  /// <param name="functionName"></param>
  /// <exception cref="ArgumentNullException"></exception>
  public PythonStepWrapper(IPythonExecutor executor, string moduleName, string functionName)
  {
    _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    _moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
    _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
    _executor.ValidateStep(_moduleName, _functionName);
  }

  /// <summary>
  /// Gets the transformation function that invokes the Python step.
  /// </summary>
  /// <returns>
  /// A function that takes <typeparamref name="TInput"/> and returns <typeparamref name="TOutput"/>.
  /// </returns>
  public Func<TInput, TOutput> GetTransform() => Invoke;

  private TOutput Invoke(TInput input) =>
    _executor.Invoke<TInput, TOutput>(_moduleName, _functionName, input);
}
