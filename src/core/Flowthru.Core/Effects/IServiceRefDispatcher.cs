using Flowthru.Core.Data.Validation;

namespace Flowthru.Core.Effects;

/// <summary>
/// Extension-provided handler for non-<see cref="ServiceRef.CSharp"/>
/// service-ref variants. The Core preflight loop dispatches each
/// non-CSharp <see cref="ServiceRef"/> through whichever registered
/// dispatcher reports it can handle the variant.
/// </summary>
/// <remarks>
/// <para>
/// CSharp refs are handled directly by the Core preflight loop via
/// <see cref="System.IServiceProvider"/> + <see cref="IFlowthruInspector{TService}"/>.
/// Other variants (e.g. <see cref="ServiceRef.Python"/>) are extension
/// territory: an extension that introduces a new variant is responsible
/// for shipping a matching <see cref="IServiceRefDispatcher"/> registered
/// as a singleton in DI.
/// </para>
/// <para>
/// The Core consumes dispatchers via <c>IEnumerable&lt;IServiceRefDispatcher&gt;</c>
/// — multiple extensions can register dispatchers without coordination.
/// The first dispatcher whose <see cref="CanHandle"/> returns true wins.
/// </para>
/// </remarks>
public interface IServiceRefDispatcher
{
  /// <summary>
  /// Reports whether this dispatcher can handle the given service ref.
  /// Implementations typically pattern-match on the variant type.
  /// </summary>
  bool CanHandle(ServiceRef serviceRef);

  /// <summary>
  /// Runs the preflight probe for the service. The dispatcher resolves
  /// any extension-side instance/inspector state internally.
  /// </summary>
  /// <param name="serviceRef">
  /// The service ref to inspect. Guaranteed by the caller to satisfy
  /// <see cref="CanHandle(ServiceRef)"/>.
  /// </param>
  /// <param name="cancellationToken">
  /// Cancellation token honoured by the engine's preflight loop.
  /// </param>
  /// <returns>
  /// The probe outcome. Implementations should catch exceptions and
  /// surface them as <see cref="ValidationResult.FromException"/> rather
  /// than throwing — Core treats throws as test bugs.
  /// </returns>
  Task<ValidationResult> InspectAsync(ServiceRef serviceRef, CancellationToken cancellationToken);
}
