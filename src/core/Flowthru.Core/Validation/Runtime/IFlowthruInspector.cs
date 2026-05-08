namespace Flowthru.Validation.Runtime;

/// <summary>
/// Sidecar that probes reachability / configuration of a registered
/// service before any step runs. Registered with the host via
/// <c>IFlowthruBuilder.AddFlowthruInspect&lt;TService&gt;(…)</c>; the
/// pre-flight pipeline runs every inspector for every step that
/// declares <c>ServiceRef.Of&lt;TService&gt;()</c>.
/// </summary>
/// <typeparam name="TService">
/// The service type this inspector probes — must match a registered
/// service in the host's <see cref="System.IServiceProvider"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// Per §2.5, this is one of three pre-flight contribution layers
/// (alongside adapter-internal validation and
/// <c>IFlowValidationHook</c>). Inspectors are the way an end user
/// adds custom reachability checks for their own services without
/// touching the framework — a Reader-shaped contribution wired through
/// DI rather than via a global plugin point.
/// </para>
/// </remarks>
public interface IFlowthruInspector<in TService>
  where TService : notnull
{
  /// <summary>
  /// Probe <paramref name="service"/>. Returns a successful effect when
  /// the service is reachable / correctly configured, or a
  /// <see cref="Validated{TError, TValue}.Invalid"/> with one or more
  /// <see cref="PreFlightError"/>s when it isn't. Multiple errors are
  /// allowed per probe — the user sees every problem at once.
  /// </summary>
  FlowIO<Validated<PreFlightError, FlowUnit>> Inspect(TService service);
}
