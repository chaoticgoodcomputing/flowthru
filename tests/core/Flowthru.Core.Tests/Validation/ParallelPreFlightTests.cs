using System.Collections.Concurrent;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.PreFlight;

namespace Flowthru.Core.Tests.Validation;

/// <summary>
/// Tests verifying parallel pre-flight inspection behaviour ported from
/// the legacy <c>ParallelPreFlightTests</c> (gap #4 in the test-coverage
/// gap analysis).
/// </summary>
/// <remarks>
/// <para>
/// Parallelism is a load-bearing claim in the pre-flight pipeline — the
/// shape of N independent I/O-bound inspections is exactly the workload
/// that benefits most from concurrency. These tests pin both contracts:
/// </para>
/// <list type="number">
/// <item><strong>Aggregation</strong> — with <c>maxDegreeOfParallelism &gt; 1</c>,
///   every per-input error survives the concurrent fan-out / sequential
///   merge pattern. None are dropped.</item>
/// <item><strong>Concurrency proof</strong> — with
///   <c>maxDegreeOfParallelism &gt; 1</c>, independent I/O-bound
///   inspections actually overlap in wall-clock time. This is verified
///   structurally (overlapping execution windows recorded by adapter
///   probes) so the assertion is robust on loaded CI machines.</item>
/// <item><strong>Sequential default</strong> — with
///   <c>maxDegreeOfParallelism = 1</c> the inspections run serially:
///   no two execution windows overlap.</item>
/// </list>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
public class ParallelPreFlightTests
{
  private static readonly TimeSpan InspectionDelay = TimeSpan.FromMilliseconds(200);

  /// <summary>True when two recorded execution windows overlap in time.</summary>
  private static bool Overlaps(
    (string Label, DateTime Start, DateTime End) a,
    (string Label, DateTime Start, DateTime End) b
  ) => a.Start < b.End && b.Start < a.End;

  // ─────────────────────────────────────────────────────────────────────────
  // Error aggregation under parallelism
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_WithParallelism_AggregatesAllInspectionErrors()
  {
    // Arrange: three independent external inputs, each backed by a
    // failing adapter. With parallelism > 1 every per-input failure
    // must still appear in the aggregated Invalid — the concurrent
    // dispatch must not drop or coalesce them.
    var input1 = MakeItem("input_1", new FailingInspectionAdapter("input_1"));
    var input2 = MakeItem("input_2", new FailingInspectionAdapter("input_2"));
    var input3 = MakeItem("input_3", new FailingInspectionAdapter("input_3"));

    var output1 = ItemFactory.Singleton.Memory<int>("output_1");
    var output2 = ItemFactory.Singleton.Memory<int>("output_2");
    var output3 = ItemFactory.Singleton.Memory<int>("output_3");

    var flow = FlowBuilder.CreateFlow("parallel-aggregate", b =>
    {
      b.AddStep<int, int>("step1", x => x, input1, output1);
      b.AddStep<int, int>("step2", x => x, input2, output2);
      b.AddStep<int, int>("step3", x => x, input3, output3);
    });

    // Act
    var result = await PreFlightPipeline
      .Run(flow, maxDegreeOfParallelism: 3)
      .Run();
    var inner = ((EffResult<Validated<PreFlightError, FlowUnit>>.Success)result).Value;

    // Assert
    Assert.That(inner, Is.InstanceOf<Validated<PreFlightError, FlowUnit>.Invalid>());
    var invalid = (Validated<PreFlightError, FlowUnit>.Invalid)inner;
    Assert.That(invalid.Errors, Has.Count.EqualTo(3),
      "All 3 per-entry errors must be aggregated regardless of dispatch order.");
    var labels = invalid.Errors.OfType<PreFlightError.InspectionFailed>()
      .Select(e => e.ItemId)
      .ToHashSet();
    Assert.That(labels, Is.EquivalentTo(new[] { "input_1", "input_2", "input_3" }),
      "Every input's failure must be present.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Concurrency proof — independent inputs overlap when parallelism > 1
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_WithParallelism_IndependentInputsOverlapInTime()
  {
    // Arrange: three slow-inspecting external inputs recording each
    // window into a shared bag. Under parallelism > 1, at least two
    // windows must overlap — the structural proof that the dispatch
    // actually fans out concurrently.
    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();
    var input1 = MakeItem("input_1", new RecordingInspectionAdapter(log, "input_1", InspectionDelay));
    var input2 = MakeItem("input_2", new RecordingInspectionAdapter(log, "input_2", InspectionDelay));
    var input3 = MakeItem("input_3", new RecordingInspectionAdapter(log, "input_3", InspectionDelay));

    var output1 = ItemFactory.Singleton.Memory<int>("output_1");
    var output2 = ItemFactory.Singleton.Memory<int>("output_2");
    var output3 = ItemFactory.Singleton.Memory<int>("output_3");

    var flow = FlowBuilder.CreateFlow("parallel-overlap", b =>
    {
      b.AddStep<int, int>("step1", x => x, input1, output1);
      b.AddStep<int, int>("step2", x => x, input2, output2);
      b.AddStep<int, int>("step3", x => x, input3, output3);
    });

    // Act
    await PreFlightPipeline
      .Run(flow, maxDegreeOfParallelism: 3)
      .Run();

    // Assert
    var entries = log.ToList();
    Assert.That(entries, Has.Count.EqualTo(3),
      "Every input's window must have been recorded.");

    var anyOverlap = entries
      .SelectMany(a => entries, (a, b) => (a, b))
      .Where(pair => pair.a.Label != pair.b.Label)
      .Any(pair => Overlaps(pair.a, pair.b));

    Assert.That(anyOverlap, Is.True,
      "With maxDegreeOfParallelism = 3, at least two inspection windows "
      + "should overlap — parallelism is load-bearing and must produce "
      + "actual concurrent execution, not just correct aggregation.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Sequential default — no overlap with maxDegreeOfParallelism = 1
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task Run_WithSequential_InspectionsDoNotOverlap()
  {
    // Arrange: same topology as the overlap test, but pin maxDegreeOfParallelism = 1
    // (the default). The inspections must run serially with strictly
    // non-overlapping windows so the legacy single-threaded behaviour
    // is preserved when the caller opts in to sequential mode.
    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();
    var input1 = MakeItem("input_1", new RecordingInspectionAdapter(log, "input_1", InspectionDelay));
    var input2 = MakeItem("input_2", new RecordingInspectionAdapter(log, "input_2", InspectionDelay));
    var input3 = MakeItem("input_3", new RecordingInspectionAdapter(log, "input_3", InspectionDelay));

    var output1 = ItemFactory.Singleton.Memory<int>("output_1");
    var output2 = ItemFactory.Singleton.Memory<int>("output_2");
    var output3 = ItemFactory.Singleton.Memory<int>("output_3");

    var flow = FlowBuilder.CreateFlow("parallel-sequential", b =>
    {
      b.AddStep<int, int>("step1", x => x, input1, output1);
      b.AddStep<int, int>("step2", x => x, input2, output2);
      b.AddStep<int, int>("step3", x => x, input3, output3);
    });

    // Act — maxDegreeOfParallelism = 1 forces the sequential fast path.
    await PreFlightPipeline
      .Run(flow, maxDegreeOfParallelism: 1)
      .Run();

    // Assert
    var entries = log.ToList();
    Assert.That(entries, Has.Count.EqualTo(3));

    var anyOverlap = entries
      .SelectMany(a => entries, (a, b) => (a, b))
      .Where(pair => pair.a.Label != pair.b.Label)
      .Any(pair => Overlaps(pair.a, pair.b));

    Assert.That(anyOverlap, Is.False,
      "With maxDegreeOfParallelism = 1, inspection windows should never "
      + "overlap — the sequential fast path is observable.");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Helpers and test doubles
  // ─────────────────────────────────────────────────────────────────────────

  private static IItem<int> MakeItem(string label, IStorageAdapter<int> adapter) =>
    new Item<int>(label, adapter);

  /// <summary>
  /// Adapter whose <see cref="InspectShallow"/> always returns a failing
  /// <see cref="ValidationResult"/> — used to verify error aggregation
  /// under parallel dispatch.
  /// </summary>
  private sealed class FailingInspectionAdapter : IStorageAdapter<int>
  {
    private readonly string _label;

    public FailingInspectionAdapter(string label) => _label = label;

    public StorageTraits Traits => new();

    public FlowIO<int> Load() => FlowIO.Pure(0);

    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Failure(
        _label,
        ValidationErrorType.InspectionFailure,
        $"Simulated inspection failure for '{_label}'"
      ));

    public FlowIO<ValidationResult> InspectDeep() => InspectShallow(0);

    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(ValidationResult.Success());
  }

  /// <summary>
  /// Storage adapter whose <see cref="InspectShallow"/> records a
  /// timestamped execution window to a shared log and then delays —
  /// used to verify concurrent vs. serial dispatch by inspecting the
  /// recorded windows for overlap.
  /// </summary>
  private sealed class RecordingInspectionAdapter : IStorageAdapter<int>
  {
    private readonly ConcurrentBag<(string Label, DateTime Start, DateTime End)> _log;
    private readonly string _label;
    private readonly TimeSpan _delay;

    public RecordingInspectionAdapter(
      ConcurrentBag<(string Label, DateTime Start, DateTime End)> log,
      string label,
      TimeSpan delay
    )
    {
      _log = log;
      _label = label;
      _delay = delay;
    }

    public StorageTraits Traits => new();

    public FlowIO<int> Load() => FlowIO.Pure(0);

    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.LiftAsync(async ct =>
      {
        var start = DateTime.UtcNow;
        await Task.Delay(_delay, ct).ConfigureAwait(false);
        _log.Add((_label, start, DateTime.UtcNow));
        return ValidationResult.Success();
      });

    public FlowIO<ValidationResult> InspectDeep() => InspectShallow(0);

    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(ValidationResult.Success());
  }
}
