using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Extensions.Python.Execution;
using Microsoft.Extensions.Logging;

namespace Flowthru.Extensions.Python.Services;

/// <summary>
/// <see cref="IServiceRefDispatcher"/> implementation for
/// <see cref="ServiceRef.Python"/> variants. Looks up the corresponding
/// <see cref="PythonServiceRegistration"/> in the
/// <see cref="IPythonServiceInspectorRegistry"/> and dispatches the probe
/// via <see cref="IPythonExecutor.InvokeInspector(PythonServiceRegistration)"/>.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a singleton in DI by <c>FlowthruServiceBuilderExtensions.UsePython</c>.
/// The Core preflight loop discovers it via
/// <c>IEnumerable&lt;IServiceRefDispatcher&gt;</c> resolution and selects
/// it for any <see cref="ServiceRef.Python"/> ref.
/// </para>
/// <para>
/// Mirrors the C#-side behaviour from <c>Flow.InspectStepServicesAsync</c>:
/// when no registration is found for a declared service, the dispatcher
/// logs a warning and returns success rather than failing the run —
/// missing inspectors are non-fatal, the user just doesn't get preflight
/// coverage for that service.
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

  /// <inheritdoc />
  public bool CanHandle(ServiceRef serviceRef) => serviceRef is ServiceRef.Python;

  /// <inheritdoc />
  public Task<ValidationResult> InspectAsync(
    ServiceRef serviceRef,
    CancellationToken cancellationToken
  )
  {
    if (serviceRef is not ServiceRef.Python python)
    {
      throw new ArgumentException(
        $"PythonServiceRefDispatcher cannot handle {serviceRef.GetType().Name}.",
        nameof(serviceRef)
      );
    }

    if (!_registry.TryGet(python.ClassPath, out var registration) || registration is null)
    {
      _logger.LogWarning(
        "Python service '{ClassPath}' has no matching python.RegisterService(...) "
          + "registration; preflight cannot inspect it.",
        python.ClassPath
      );
      // Non-fatal — match C# side behaviour for missing IFlowthruInspector.
      return Task.FromResult(ValidationResult.Success());
    }

    // The executor's InvokeInspector is synchronous (subprocess round-trip
    // is sync from the C# perspective; the worker handles its own GIL).
    // Wrap in Task.FromResult to satisfy the dispatcher contract.
    var result = _executor.InvokeInspector(registration);
    return Task.FromResult(result);
  }
}
