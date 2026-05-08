namespace Flowthru.Hosting;

/// <summary>
/// Fluent handle returned by <c>IFlowthruBuilder.RegisterFlow</c>.
/// Carries the registration's mutable metadata (description, etc.)
/// without exposing the underlying flow factory delegate.
/// </summary>
public interface IFlowRegistration
{
  /// <summary>The flow's label — also the slicing key (§2.4).</summary>
  string Label { get; }

  /// <summary>
  /// Attach a human-readable description for diagnostic output and
  /// metadata exporters. Returns <c>this</c> for chaining.
  /// </summary>
  IFlowRegistration WithDescription(string description);
}
