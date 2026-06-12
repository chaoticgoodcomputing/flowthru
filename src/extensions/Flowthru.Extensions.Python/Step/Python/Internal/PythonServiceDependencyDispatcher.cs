using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;
using Microsoft.Extensions.Logging;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Routes <see cref="ServiceDependency.External"/> values whose
/// <see cref="IExtensionServiceDependency.Category"/> is <c>"python"</c> to
/// the Python executor's sidecar inspector. Registered via DI by
/// <c>UsePython()</c>; Core's pre-flight pipeline picks it up through
/// the <see cref="IEnumerable{IServiceDependencyDispatcher}"/> resolution.
/// </summary>
/// <remarks>
/// <para>
/// When a Python service has no matching
/// <c>RegisterService(...)</c> entry in the inspector registry, the
/// dispatcher logs a warning and returns success — missing inspectors
/// are non-fatal, mirroring the C#-side
/// <see cref="IFlowServiceInspector{T}"/> resolution semantics.
/// </para>
/// </remarks>
internal sealed class PythonServiceDependencyDispatcher : IServiceDependencyDispatcher
{
  private readonly IPythonServiceInspectorRegistry _registry;
  private readonly IPythonExecutor _executor;
  private readonly ILogger<PythonServiceDependencyDispatcher> _logger;

  public PythonServiceDependencyDispatcher(
    IPythonServiceInspectorRegistry registry,
    IPythonExecutor executor,
    ILogger<PythonServiceDependencyDispatcher> logger
  )
  {
    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc/>
  public string Category => "python";

  /// <inheritdoc/>
  public FlowIO<Validated<PreFlightError, FlowUnit>> Inspect(IExtensionServiceDependency serviceRef)
  {
    if (serviceRef is not PythonServiceDependency python)
    {
      // Defensive: Category="python" should only ever produce PythonServiceDependency
      // values; mismatch is a programming error in whoever constructed the ref.
      return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(
          new PythonPreFlightError.ServiceInspectionFailed(
            ServiceClassPath: serviceRef.DagId,
            Detail: $"Expected PythonServiceDependency but got {serviceRef.GetType().Name}."
          )
        )
      ));
    }

    if (!_registry.TryGet(python.ClassPath, out var registration) || registration is null)
    {
      _logger.LogWarning(
        "Python service '{ClassPath}' has no matching python.RegisterService(...) "
          + "registration; pre-flight cannot inspect it.",
        python.ClassPath
      );
      // Non-fatal — match C# side behaviour for missing IFlowServiceInspector.
      return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
    }

    return _executor.InvokeInspector(registration);
  }
}
