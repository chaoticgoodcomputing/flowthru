using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.PreFlight.Python;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;
using Microsoft.Extensions.Logging;

namespace Flowthru.Step.Python.Internal;

/// <summary>
/// Routes <see cref="ServiceRef.External"/> values whose
/// <see cref="IExtensionServiceRef.Category"/> is <c>"python"</c> to
/// the Python executor's sidecar inspector. Registered via DI by
/// <c>UsePython()</c>; Core's pre-flight pipeline picks it up through
/// the <see cref="IEnumerable{IServiceRefDispatcher}"/> resolution.
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
internal sealed class PythonServiceRefDispatcher : IServiceRefDispatcher
{
  private readonly IPythonServiceInspectorRegistry _registry;
  private readonly IPythonExecutor _executor;
  private readonly ILogger<PythonServiceRefDispatcher> _logger;

  public PythonServiceRefDispatcher(
    IPythonServiceInspectorRegistry registry,
    IPythonExecutor executor,
    ILogger<PythonServiceRefDispatcher> logger
  )
  {
    _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc/>
  public string Category => "python";

  /// <inheritdoc/>
  public FlowIO<Validated<PreFlightError, FlowUnit>> Inspect(IExtensionServiceRef serviceRef)
  {
    if (serviceRef is not PythonServiceRef python)
    {
      // Defensive: Category="python" should only ever produce PythonServiceRef
      // values; mismatch is a programming error in whoever constructed the ref.
      return FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Fail(
        new PreFlightError.External(
          new PythonPreFlightError.ServiceInspectionFailed(
            ServiceClassPath: serviceRef.DagId,
            Detail: $"Expected PythonServiceRef but got {serviceRef.GetType().Name}."
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
