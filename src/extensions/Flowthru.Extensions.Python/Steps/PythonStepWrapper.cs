using Flowthru.Core.Effects;
using Flowthru.Extensions.Python.Execution;

namespace Flowthru.Extensions.Python.Steps;

/// <summary>
/// Thin wrapper that binds an <see cref="IPythonExecutor"/> to a specific module/function pair,
/// exposing it as a typed <c>Func&lt;TInput, TOutput&gt;</c> for use with the Flow builder.
/// </summary>
/// <remarks>
/// <para>
/// All marshalling (scalar, tabular, bytes, multi-I/O tuples) is delegated to the executor.
/// </para>
/// <para>
/// At construction the wrapper validates the Python step exists and captures the
/// <see cref="PythonStepMetadata"/> emitted by the <c>@step</c> decorator. Callers can
/// read <see cref="Services"/> to populate <c>FlowStep.ServiceDependencies</c> with the
/// step's declared Python service dependencies.
/// </para>
/// </remarks>
public sealed class PythonStepWrapper<TInput, TOutput>
{
  private readonly IPythonExecutor _executor;
  private readonly string _moduleName;
  private readonly string _functionName;

  /// <summary>
  /// Initializes the wrapper with the executor and step information. Validates
  /// the step against the executor and captures decorator-derived metadata.
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
    Metadata = _executor.ValidateStep(_moduleName, _functionName);

    if (Metadata.Services.Count > 0)
    {
      var refs = new ServiceRef[Metadata.Services.Count];
      for (int i = 0; i < Metadata.Services.Count; i++)
      {
        refs[i] = new ServiceRef.Python(Metadata.Services[i]);
      }
      ServiceRefs = refs;
    }
  }

  /// <summary>
  /// Decorator-derived metadata captured at registration. Currently surfaces
  /// the step's declared service dependencies via <see cref="ServiceRefs"/>.
  /// </summary>
  public PythonStepMetadata Metadata { get; }

  /// <summary>
  /// Service refs corresponding to each entry in
  /// <see cref="PythonStepMetadata.Services"/>, wrapped as
  /// <see cref="ServiceRef.Python"/> so callers can pass them to
  /// <see cref="Flowthru.Core.Graph.FlowStep"/> alongside C# refs.
  /// Empty when the step declares no services.
  /// </summary>
  public IReadOnlyList<ServiceRef> ServiceRefs { get; } =
    Array.Empty<ServiceRef>();

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
