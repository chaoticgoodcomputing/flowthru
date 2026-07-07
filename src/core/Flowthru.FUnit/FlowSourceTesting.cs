using System.Runtime.CompilerServices;
using System.Threading;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Step.Testing;

/// <summary>
/// Streaming-test affordances for <see cref="FlowSource{T}"/> — the streaming
/// sibling of the eager <see cref="SampleBuilder"/> helpers. Lets a step test
/// lift in-memory samples into a stream, observe the pull-scoped bracket's
/// release, and observe a sink's batch lifecycle, without leaving the test
/// framework.
/// </summary>
public static class FlowSourceTesting
{
  /// <summary>
  /// Lift an in-memory sample sequence into a <see cref="FlowSource{T}"/> — the
  /// streaming form of a sample list. Mirrors the ADR's
  /// <c>Samples.Of&lt;T&gt;(...).AsStream()</c> ergonomics.
  /// </summary>
  public static FlowSource<T> AsStream<T>(this IEnumerable<T> items) =>
    FlowSource.FromEnumerable(items);
}

/// <summary>
/// A streaming test double: wraps an in-memory sequence in a bracketed
/// <see cref="FlowSource{T}"/> whose acquire/release and per-item pull are
/// observable. Lets a streaming test assert that the pull-scoped bracket
/// released (on every exit path — completion, failure, cancellation) and how
/// far the read progressed before a mid-stream cancel.
/// </summary>
/// <typeparam name="T">The element type produced by the source.</typeparam>
public sealed class TrackingSource<T>
{
  private readonly IEnumerable<T> _items;
  private readonly Action<int>? _onPulled;
  private int _acquireCount;
  private int _releaseCount;
  private int _pulledCount;
  private RuntimeError? _lastReleaseError;

  /// <summary>
  /// Build a tracking source over <paramref name="items"/>.
  /// </summary>
  /// <param name="items">The sequence the source yields.</param>
  /// <param name="onPulled">
  /// Optional callback invoked with the 1-based index each time an element is
  /// about to be yielded — the hook a test uses to cancel a
  /// <see cref="CancellationTokenSource"/> mid-stream and assert the read stops
  /// and the bracket releases.
  /// </param>
  public TrackingSource(IEnumerable<T> items, Action<int>? onPulled = null)
  {
    _items = items ?? throw new ArgumentNullException(nameof(items));
    _onPulled = onPulled;
  }

  /// <summary>How many times the underlying bracket was acquired (once per compiled run).</summary>
  public int AcquireCount => _acquireCount;

  /// <summary>How many times the underlying bracket was released.</summary>
  public int ReleaseCount => _releaseCount;

  /// <summary>How many elements were pulled before the stream ended (or was cancelled/failed).</summary>
  public int PulledCount => _pulledCount;

  /// <summary>True once the bracket has released at least once.</summary>
  public bool Released => Volatile.Read(ref _releaseCount) > 0;

  /// <summary>
  /// The <see cref="RuntimeError"/> the release closure last received —
  /// <c>null</c> after a clean completion, or the terminating error (e.g.
  /// <see cref="RuntimeError.Cancelled"/>) after a mid-stream abort.
  /// </summary>
  public RuntimeError? LastReleaseError => _lastReleaseError;

  /// <summary>
  /// The <see cref="FlowSource{T}"/> view. Deferred: acquire runs on the first
  /// pull of the compiled effect, never at construction.
  /// </summary>
  public FlowSource<T> Source =>
    FlowSource.Bracket(
      FlowResource.Make<int>(
        acquire: FlowIO.Lift(() =>
        {
          Interlocked.Increment(ref _acquireCount);
          return 0;
        }),
        release: (_, error) => FlowIO.Lift(() =>
        {
          Interlocked.Increment(ref _releaseCount);
          _lastReleaseError = error;
          return FlowUnit.Default;
        })
      ),
      (_, ct) => Pull(ct)
    );

  private async IAsyncEnumerable<T> Pull([EnumeratorCancellation] CancellationToken ct)
  {
    await Task.CompletedTask.ConfigureAwait(false);
    foreach (var item in _items)
    {
      ct.ThrowIfCancellationRequested();
      var index = Interlocked.Increment(ref _pulledCount);
      _onPulled?.Invoke(index);
      yield return item;
    }
  }
}

/// <summary>
/// An observable <see cref="IFlowSink{T}"/> test double. Records the batch
/// lifecycle (<c>open</c> → <c>write(n)</c>… → <c>complete</c> → <c>dispose</c>)
/// and every element written, so a streaming test can assert the sink was
/// completed and released — including the abort path where
/// <see cref="IFlowSink{T}.CompleteAsync"/> is skipped but
/// <see cref="System.IAsyncDisposable.DisposeAsync"/> still runs.
/// </summary>
/// <typeparam name="T">The element type consumed by the sink.</typeparam>
public sealed class RecordingSink<T> : IFlowSink<T>
{
  private readonly List<string> _events = new();
  private readonly List<T> _written = new();

  /// <summary>Build a recording sink with the given <paramref name="batchSize"/>.</summary>
  public RecordingSink(int batchSize = 1) => BatchSize = batchSize;

  /// <summary>The lifecycle trace in order: <c>open</c>, <c>write(n)</c>, <c>complete</c>, <c>dispose</c>.</summary>
  public IReadOnlyList<string> Events => _events;

  /// <summary>Every element written across all batches.</summary>
  public IReadOnlyList<T> Written => _written;

  /// <summary>True once <see cref="CompleteAsync"/> ran (the stream drained fully).</summary>
  public bool Completed { get; private set; }

  /// <summary>True once <see cref="DisposeAsync"/> ran (release happened on some exit path).</summary>
  public bool Disposed { get; private set; }

  /// <inheritdoc/>
  public int BatchSize { get; }

  /// <inheritdoc/>
  public ValueTask OpenAsync(CancellationToken cancellationToken)
  {
    _events.Add("open");
    return ValueTask.CompletedTask;
  }

  /// <inheritdoc/>
  public ValueTask WriteBatchAsync(IReadOnlyList<T> batch, CancellationToken cancellationToken)
  {
    _events.Add($"write({batch.Count})");
    _written.AddRange(batch);
    return ValueTask.CompletedTask;
  }

  /// <inheritdoc/>
  public ValueTask CompleteAsync(CancellationToken cancellationToken)
  {
    _events.Add("complete");
    Completed = true;
    return ValueTask.CompletedTask;
  }

  /// <inheritdoc/>
  public ValueTask DisposeAsync()
  {
    _events.Add("dispose");
    Disposed = true;
    return ValueTask.CompletedTask;
  }
}
