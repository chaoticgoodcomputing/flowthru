using System.Collections.Concurrent;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Capabilities;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Validation;
using Flowthru.Core.Effects;
using Flowthru.Core.Flows;
using Flowthru.Core.Tests.Fixtures.TestCatalogs;
using Flowthru.Core.Tests.Fixtures.TestSteps;

namespace Flowthru.Core.Tests.Validation.PreFlightInspection;

/// <summary>
/// Tests verifying the parallel pre-flight inspection behaviour of
/// <see cref="Flow.ValidateExternalInputsAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests exercise the three contracts introduced by the parallel inspection path:
/// </para>
/// <list type="number">
/// <item>
///   <strong>Error aggregation</strong> — all per-entry errors are collected regardless of
///   concurrency; none are dropped by the <c>ConcurrentBag</c> → sequential-merge pattern.
/// </item>
/// <item>
///   <strong>Concurrency</strong> — with <c>maxDegreeOfParallelism &gt; 1</c>, independent
///   I/O-bound inspections actually execute concurrently (overlapping windows).
/// </item>
/// <item>
///   <strong>Sequential default</strong> — with <c>maxDegreeOfParallelism = 1</c>, inspections
///   run serially (no overlapping windows), preserving the pre-existing behavior.
/// </item>
/// </list>
/// <para>
/// Concurrency is verified structurally (overlapping execution windows) rather than by
/// elapsed time — wall-clock assertions are inherently flaky on loaded CI machines.
/// </para>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
public class ParallelPreFlightTests
{
  private static readonly TimeSpan InspectionDelay = TimeSpan.FromMilliseconds(200);

  /// <summary>Returns true when two inspection windows overlap in time.</summary>
  private static bool Overlaps(
    (string Label, DateTime Start, DateTime End) a,
    (string Label, DateTime Start, DateTime End) b
  ) => a.Start < b.End && b.Start < a.End;

  // ─────────────────────────────────────────────────────────────────────────
  // Error aggregation
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_WithParallelism_AggregatesAllErrors()
  {
    // Arrange: 3 independent external inputs, each backed by a failing adapter.
    // All 3 errors must survive the ConcurrentBag → sequential-merge path.
    var input1 = new Item<IEnumerable<TestData>>(
      "input_1",
      new FailingInspectionAdapter("input_1")
    );
    var input2 = new Item<IEnumerable<TestData>>(
      "input_2",
      new FailingInspectionAdapter("input_2")
    );
    var input3 = new Item<IEnumerable<TestData>>(
      "input_3",
      new FailingInspectionAdapter("input_3")
    );

    var output1 = ItemFactory.Enumerable.Memory<TestData>("output_1");
    var output2 = ItemFactory.Enumerable.Memory<TestData>("output_2");
    var output3 = ItemFactory.Enumerable.Memory<TestData>("output_3");

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("Step1", PassthroughStep.Create(), input1, output1);
      builder.AddStep("Step2", PassthroughStep.Create(), input2, output2);
      builder.AddStep("Step3", PassthroughStep.Create(), input3, output3);
    });

    flow.Build();

    flow.ValidationOptions.Inspect(input1, InspectionLevel.Shallow);
    flow.ValidationOptions.Inspect(input2, InspectionLevel.Shallow);
    flow.ValidationOptions.Inspect(input3, InspectionLevel.Shallow);

    // Act
    var result = await flow.ValidateExternalInputsAsync(
      maxDegreeOfParallelism: 3,
      cancellationToken: CancellationToken.None
    );

    // Assert
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors, Has.Count.EqualTo(3), "All 3 per-entry errors must be aggregated");
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Concurrency — independent inputs overlap when parallelism > 1
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_WithParallelism_IndependentInputsOverlapInTime()
  {
    // Arrange
    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();

    var input1 = new Item<IEnumerable<TestData>>(
      "input_1",
      new RecordingInspectionAdapter(log, "input_1", InspectionDelay)
    );
    var input2 = new Item<IEnumerable<TestData>>(
      "input_2",
      new RecordingInspectionAdapter(log, "input_2", InspectionDelay)
    );
    var input3 = new Item<IEnumerable<TestData>>(
      "input_3",
      new RecordingInspectionAdapter(log, "input_3", InspectionDelay)
    );

    var output1 = ItemFactory.Enumerable.Memory<TestData>("output_1");
    var output2 = ItemFactory.Enumerable.Memory<TestData>("output_2");
    var output3 = ItemFactory.Enumerable.Memory<TestData>("output_3");

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("Step1", PassthroughStep.Create(), input1, output1);
      builder.AddStep("Step2", PassthroughStep.Create(), input2, output2);
      builder.AddStep("Step3", PassthroughStep.Create(), input3, output3);
    });

    flow.Build();

    flow.ValidationOptions.Inspect(input1, InspectionLevel.Shallow);
    flow.ValidationOptions.Inspect(input2, InspectionLevel.Shallow);
    flow.ValidationOptions.Inspect(input3, InspectionLevel.Shallow);

    // Act
    await flow.ValidateExternalInputsAsync(
      maxDegreeOfParallelism: 3,
      cancellationToken: CancellationToken.None
    );

    // Assert: at least two inspection windows must overlap
    var entries = log.ToList();
    Assert.That(entries, Has.Count.EqualTo(3));

    var anyOverlap = entries
      .SelectMany(a => entries, (a, b) => (a, b))
      .Where(pair => pair.a.Label != pair.b.Label)
      .Any(pair => Overlaps(pair.a, pair.b));

    Assert.That(
      anyOverlap,
      Is.True,
      "With maxDegreeOfParallelism = 3, at least two inspection windows should overlap"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Sequential default — no overlap with maxDegreeOfParallelism = 1
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_WithSequential_InspectionsDoNotOverlap()
  {
    // Arrange: same topology, but maxDegreeOfParallelism = 1 (default/sequential).
    var log = new ConcurrentBag<(string Label, DateTime Start, DateTime End)>();

    var input1 = new Item<IEnumerable<TestData>>(
      "input_1",
      new RecordingInspectionAdapter(log, "input_1", InspectionDelay)
    );
    var input2 = new Item<IEnumerable<TestData>>(
      "input_2",
      new RecordingInspectionAdapter(log, "input_2", InspectionDelay)
    );
    var input3 = new Item<IEnumerable<TestData>>(
      "input_3",
      new RecordingInspectionAdapter(log, "input_3", InspectionDelay)
    );

    var output1 = ItemFactory.Enumerable.Memory<TestData>("output_1");
    var output2 = ItemFactory.Enumerable.Memory<TestData>("output_2");
    var output3 = ItemFactory.Enumerable.Memory<TestData>("output_3");

    var flow = FlowBuilder.CreateFlow(builder =>
    {
      builder.AddStep("Step1", PassthroughStep.Create(), input1, output1);
      builder.AddStep("Step2", PassthroughStep.Create(), input2, output2);
      builder.AddStep("Step3", PassthroughStep.Create(), input3, output3);
    });

    flow.Build();

    flow.ValidationOptions.Inspect(input1, InspectionLevel.Shallow);
    flow.ValidationOptions.Inspect(input2, InspectionLevel.Shallow);
    flow.ValidationOptions.Inspect(input3, InspectionLevel.Shallow);

    // Act
    await flow.ValidateExternalInputsAsync(
      maxDegreeOfParallelism: 1,
      cancellationToken: CancellationToken.None
    );

    // Assert: no windows should overlap when running sequentially
    var entries = log.ToList();
    Assert.That(entries, Has.Count.EqualTo(3));

    var anyOverlap = entries
      .SelectMany(a => entries, (a, b) => (a, b))
      .Where(pair => pair.a.Label != pair.b.Label)
      .Any(pair => Overlaps(pair.a, pair.b));

    Assert.That(
      anyOverlap,
      Is.False,
      "With maxDegreeOfParallelism = 1, inspection windows should never overlap"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Test doubles
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Storage adapter whose <see cref="InspectShallow"/> always returns a failing
  /// <see cref="ValidationResult"/> — used to verify error aggregation.
  /// </summary>
  private sealed class FailingInspectionAdapter : IStorageAdapter<IEnumerable<TestData>>
  {
    private readonly string _label;

    public FailingInspectionAdapter(string label) => _label = label;

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(
        new ValidationResult(
          new[]
          {
            new ValidationError(
              _label,
              ValidationErrorType.InspectionFailure,
              $"Simulated inspection failure for '{_label}'",
              null
            ),
          }
        )
      );

    public FlowIO<ValidationResult> InspectDeep() => InspectShallow(0);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }

  /// <summary>
  /// Storage adapter whose <see cref="InspectShallow"/> records a timestamped execution
  /// window to a shared log and then delays — used to verify concurrent vs. serial dispatch.
  /// </summary>
  private sealed class RecordingInspectionAdapter : IStorageAdapter<IEnumerable<TestData>>
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

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize)
    {
      Func<CancellationToken, ValueTask<ValidationResult>> inspector = async (ct) =>
      {
        var start = DateTime.UtcNow;
        await Task.Delay(_delay, ct);
        _log.Add((_label, start, DateTime.UtcNow));
        return ValidationResult.Success();
      };
      return FlowIO.LiftAsync(inspector);
    }

    public FlowIO<ValidationResult> InspectDeep() => InspectShallow(0);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
