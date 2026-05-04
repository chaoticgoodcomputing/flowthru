namespace Flowthru.Core.Effects;

/// <summary>
/// Type-erased view of a <see cref="FlowResource{TScope}"/> used by the
/// framework to acquire and release resources of heterogeneous scope types
/// during flow execution.
/// </summary>
/// <remarks>
/// User code should declare and consume the typed <see cref="FlowResource{TScope}"/>;
/// this interface exists so the service layer can collect resources from
/// multiple catalogs into a single sequence and run them through a uniform
/// acquire-then-LIFO-release loop.
/// </remarks>
public interface IFlowResource
{
  /// <summary>
  /// Effect that acquires the resource and returns the scope (boxed).
  /// </summary>
  FlowIO<object?> AcquireUntyped();

  /// <summary>
  /// Builds the release effect for a previously acquired scope. The optional
  /// exception parameter is the body's primary failure (when non-null), so
  /// the release closure can inspect it to implement policies like
  /// "preserve on failure for debugging."
  /// </summary>
  FlowIO<FlowUnit> ReleaseUntyped(object? scope, Exception? bodyException);
}
