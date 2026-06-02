namespace Flowthru.Validation.PreFlight;

/// <summary>
/// How far the <see cref="PreFlightPipeline"/> reaches — the I/O boundary
/// of pre-flight. Mirrors the lower rungs of
/// <see cref="Flowthru.Flow.ValidationDepth"/>: <see cref="Hermetic"/>
/// corresponds to <c>ValidationDepth.Hermetic</c>, <see cref="Full"/> to
/// <c>Shallow</c>/<c>Deep</c>.
/// </summary>
public enum PreFlightScope
{
  /// <summary>
  /// Run only the checks that perform <em>no I/O</em>: dispatcher presence
  /// for external service refs and C# service-dependency DI registration.
  /// Skip adapter inspection, flow validation hooks, service-inspector
  /// probes, and <c>dispatcher.Inspect</c> — anything that touches a live
  /// resource.
  /// </summary>
  Hermetic,

  /// <summary>
  /// Run every layer: the hermetic checks plus all live-resource probes.
  /// </summary>
  Full,
}
