namespace Flowthru.Validation.Runtime;

/// <summary>
/// Resolves and probes <see cref="ServiceDependency.External"/> values without
/// Core knowing the concrete extension. The host registers one
/// dispatcher per <see cref="IExtensionServiceDependency.Category"/>; the
/// pre-flight pipeline consults the dispatcher to verify reachability
/// before any step runs.
/// </summary>
/// <remarks>
/// <para>
/// Per §2.5, this is the proof-of-concept for the open-extension pattern.
/// Core ships no concrete dispatcher — every external service category
/// is an extension responsibility. The <see cref="Category"/> property
/// is the discriminator the host uses to match a dispatcher to an
/// incoming <see cref="IExtensionServiceDependency"/>.
/// </para>
/// </remarks>
public interface IServiceDependencyDispatcher
{
  /// <summary>
  /// The <see cref="IExtensionServiceDependency.Category"/> this dispatcher
  /// handles. Multiple dispatchers can be registered with the host so
  /// long as their categories are unique.
  /// </summary>
  string Category { get; }

  /// <summary>
  /// Probe reachability of <paramref name="serviceRef"/>. Returns a
  /// successful effect when the service is reachable / configured,
  /// or a <see cref="PreFlightError.External"/> wrapping an
  /// <see cref="IExtensionPreFlightError"/> when it isn't.
  /// </summary>
  FlowIO<Validated<PreFlightError, FlowUnit>> Inspect(IExtensionServiceDependency serviceRef);
}
