using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Effects;

/// <summary>
/// Sidecar capability contract for preflight-inspecting a service that a step depends on.
/// </summary>
/// <typeparam name="TService">
/// The service type being inspected. The closed type is registered in DI so the engine
/// can resolve the right inspector for each step's service dependency.
/// </typeparam>
/// <remarks>
/// <para>
/// Services do not implement Flowthru types directly — inspection is always attached
/// via <see cref="Services.FlowthruInspectionExtensions.AddFlowthruInspect{TService}"/>.
/// This decouples the service contract from Flowthru's preflight surface, so third-party
/// SDK clients (AWS, Mailchimp, Stripe, etc.) can be made inspectable without wrapping
/// or subclassing.
/// </para>
/// <para>
/// Implementations should perform a lightweight reachability probe (ping endpoint, list
/// connectivity check, etc.) and return a <see cref="ValidationResult"/>. Throwing is
/// permitted; the engine wraps thrown exceptions via
/// <see cref="ValidationResult.FromException"/>.
/// </para>
/// </remarks>
public interface IFlowthruInspector<TService>
{
  /// <summary>
  /// Probes the service for preflight readiness. Should be cheap and idempotent.
  /// </summary>
  /// <param name="service">The resolved service instance.</param>
  /// <param name="ct">Cancellation token honoured by the engine's preflight loop.</param>
  /// <returns>
  /// A <see cref="FlowIO{ValidationResult}"/> capturing the probe outcome. Returning
  /// <see cref="ValidationResult.Success"/> indicates the service is reachable and ready.
  /// </returns>
  FlowIO<ValidationResult> InspectAsync(TService service, CancellationToken ct = default);
}
