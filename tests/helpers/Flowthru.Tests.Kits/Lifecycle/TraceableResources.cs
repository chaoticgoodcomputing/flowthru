using Flowthru.Core.Effects;

namespace Flowthru.Tests.Kits.Lifecycle;

/// <summary>
/// Factory for <see cref="FlowResource{TScope}"/> instances that record
/// acquire/release events to a <see cref="LifecycleTracker"/>. Used by
/// integration tests to verify framework lifecycle wiring without requiring
/// a real database/filesystem backend.
/// </summary>
public static class TraceableResources
{
  /// <summary>
  /// Build a resource that records acquire and release events under
  /// <paramref name="label"/>. The scope value is the label itself, which
  /// makes assertions about scope round-tripping straightforward.
  /// </summary>
  /// <param name="tracker">Tracker that receives the events.</param>
  /// <param name="label">Identifier recorded with each event.</param>
  /// <param name="releaseError">
  /// When non-null, the release effect throws this exception. Used to
  /// verify that the framework captures release errors into
  /// <c>FlowResult.TeardownErrors</c> rather than masking the primary
  /// outcome.
  /// </param>
  public static FlowResource<string> Make(
    LifecycleTracker tracker,
    string label,
    Exception? releaseError = null
  )
  {
    return FlowResource.Make<string>(
      acquire: FlowIO.Lift(() =>
      {
        tracker.Record(label, LifecyclePhase.Acquire);
        return label;
      }),
      release: (scope, ex) =>
        FlowIO.Lift(() =>
        {
          tracker.Record(label, LifecyclePhase.Release, ex);
          if (releaseError is not null)
          {
            throw releaseError;
          }
          return FlowUnit.Default;
        })
    );
  }
}
