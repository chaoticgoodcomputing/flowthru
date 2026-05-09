namespace Flowthru.Validation.Runtime;

/// <summary>
/// Sidecar that probes reachability / configuration of a registered
/// service before any step runs. The interface form is for
/// non-trivial inspectors that benefit from being a real type —
/// stateful, testable, debuggable. For one-line probes the function
/// shape of <c>IFlowthruBuilder.AddFlowServiceInspector&lt;TService&gt;</c>
/// is shorter.
/// </summary>
/// <typeparam name="TService">
/// The service type this inspector probes — must match a registered
/// service in the host's <see cref="System.IServiceProvider"/>. The
/// service is typically authored as a plain C# service with no
/// awareness of Flowthru; the inspector attaches the probe externally.
/// </typeparam>
/// <remarks>
/// <para>
/// Per §2.5, this is one of three pre-flight contribution layers
/// (alongside adapter-internal validation and
/// <c>IFlowValidationHook</c>). Inspectors are how an end user adds
/// custom reachability checks for their own services without touching
/// the framework — a Reader-shaped contribution wired through DI
/// rather than a global plugin point.
/// </para>
/// <para>
/// Returning <see cref="InspectionResult"/> keeps the FP algebra
/// behind the API surface. Implementations construct one of
/// <see cref="Inspect.Pass"/>, <see cref="Inspect.Fail(string, string?)"/>,
/// or <see cref="Inspect.FailIf"/> — the framework unwraps internally.
/// </para>
/// </remarks>
public interface IFlowServiceInspector<in TService>
  where TService : notnull
{
  /// <summary>
  /// Probe <paramref name="service"/>. Return
  /// <see cref="Inspect.Pass"/> when reachable / correctly configured,
  /// or <see cref="Inspect.Fail(string, string?)"/> with a description
  /// when it isn't.
  /// </summary>
  Task<InspectionResult> InspectAsync(TService service, CancellationToken cancellationToken);
}
