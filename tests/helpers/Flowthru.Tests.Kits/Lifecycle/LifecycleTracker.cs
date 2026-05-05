namespace Flowthru.Tests.Kits.Lifecycle;

/// <summary>
/// Records lifecycle events emitted by traceable resources during integration
/// tests. Thread-safe — multiple resources can append concurrently.
/// </summary>
public sealed class LifecycleTracker
{
  private readonly object _lock = new();
  private readonly List<LifecycleEvent> _events = new();

  /// <summary>Snapshot of all events recorded so far, in insertion order.</summary>
  public IReadOnlyList<LifecycleEvent> Events
  {
    get
    {
      lock (_lock)
      {
        return _events.ToArray();
      }
    }
  }

  internal void Record(string label, LifecyclePhase phase, Exception? bodyException = null)
  {
    lock (_lock)
    {
      _events.Add(new LifecycleEvent(label, phase, bodyException));
    }
  }
}

/// <summary>
/// One lifecycle event — a label (typically a catalog/resource name), the
/// phase that fired, and (for release events) the body exception observed.
/// </summary>
public sealed record LifecycleEvent(
  string Label,
  LifecyclePhase Phase,
  Exception? BodyException
);

/// <summary>Phases tracked by <see cref="LifecycleTracker"/>.</summary>
public enum LifecyclePhase
{
  Acquire,
  Release,
}
