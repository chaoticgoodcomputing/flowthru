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
/// Tests verifying the target validation pass introduced in
/// <see cref="Flow.ValidateExternalInputsAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// These tests exercise the five behavioral contracts of the write-destination
/// validation pass:
/// </para>
/// <list type="number">
/// <item>
///   <strong>Failure propagation</strong> — a flow whose output <c>InspectTarget()</c>
///   fails causes the overall result to be invalid.
/// </item>
/// <item>
///   <strong>Error attribution</strong> — healthy sources + failing target produces
///   only the target's errors, not source errors.
/// </item>
/// <item>
///   <strong>Source short-circuit</strong> — when source validation fails, <c>InspectTarget()</c>
///   is never called on any output item.
/// </item>
/// <item>
///   <strong><c>CanInspect = false</c> skipped</strong> — adapters that declare themselves
///   non-inspectable are not probed even when their target would fail.
/// </item>
/// <item>
///   <strong><c>SkipTargetInspection()</c> escape hatch</strong> — explicitly opting an
///   output out of target validation suppresses its failure.
/// </item>
/// </list>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("PreFlight")]
public class TargetPreFlightTests
{
  // ─────────────────────────────────────────────────────────────────────────
  // (1) Failing target causes invalid result
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_WithFailingTarget_ReturnsInvalid()
  {
    // Arrange: healthy input, output backed by a failing target adapter.
    var input = ItemFactory.Enumerable.Memory<TestData>("input");
    var output = new Item<IEnumerable<TestData>>("output", new FailingTargetAdapter("output"));

    var flow = FlowBuilder.CreateFlow(builder =>
      builder.AddStep("Step", PassthroughStep.Create(), input, output)
    );
    flow.Build();

    // Skip source inspection — input has no data and this test is about target behavior.
    flow.ValidationOptions.Inspect(input, InspectionLevel.None);

    // Act
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    // Assert
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors, Has.Count.EqualTo(1));
    Assert.That(result.Errors[0].CatalogKey, Is.EqualTo("output"));
    Assert.That(result.Errors[0].ErrorType, Is.EqualTo(ValidationErrorType.WriteAccessDenied));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (2) Healthy sources + failing target → only target errors surface
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_HealthySources_FailingTarget_OnlyTargetErrorsSurface()
  {
    // Arrange: external input configured for shallow inspection (passes), output fails target.
    var input = new Item<IEnumerable<TestData>>("input", new PassingInspectionAdapter("input"));
    var output = new Item<IEnumerable<TestData>>("output", new FailingTargetAdapter("output"));

    var flow = FlowBuilder.CreateFlow(builder =>
      builder.AddStep("Step", PassthroughStep.Create(), input, output)
    );
    flow.Build();

    flow.ValidationOptions.Inspect(input, InspectionLevel.Shallow);

    // Act
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    // Assert: exactly one error, from the target — not the source
    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors, Has.Count.EqualTo(1));
    Assert.That(result.Errors[0].CatalogKey, Is.EqualTo("output"));
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (3) Source failure short-circuits — target InspectTarget() never called
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_SourceFails_SkipsTargetInspection()
  {
    // Arrange: source inspection fails; output uses a recording target adapter.
    var input = new Item<IEnumerable<TestData>>("input", new FailingSourceAdapter("input"));
    var targetCallCount = 0;
    var output = new Item<IEnumerable<TestData>>(
      "output",
      new CountingTargetAdapter("output", () => Interlocked.Increment(ref targetCallCount))
    );

    var flow = FlowBuilder.CreateFlow(builder =>
      builder.AddStep("Step", PassthroughStep.Create(), input, output)
    );
    flow.Build();

    flow.ValidationOptions.Inspect(input, InspectionLevel.Shallow);

    // Act
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    // Assert: result invalid (source error), and target was never probed
    Assert.That(result.IsValid, Is.False);
    Assert.That(
      targetCallCount,
      Is.EqualTo(0),
      "InspectTarget() must not be called when source validation already failed"
    );
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (4) CanInspect = false skips target inspection
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_CanInspectFalse_SkipsTargetInspection()
  {
    // Arrange: output adapter that would fail InspectTarget but declares CanInspect = false.
    var input = ItemFactory.Enumerable.Memory<TestData>("input");
    var output = new Item<IEnumerable<TestData>>(
      "output",
      new NonInspectableFailingTargetAdapter("output")
    );

    var flow = FlowBuilder.CreateFlow(builder =>
      builder.AddStep("Step", PassthroughStep.Create(), input, output)
    );
    flow.Build();

    // Skip source inspection — input has no data and this test is about target behavior.
    flow.ValidationOptions.Inspect(input, InspectionLevel.None);

    // Act
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    // Assert: CanInspect = false is honoured — result passes despite adapter returning failure
    Assert.That(result.IsValid, Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // (5) SkipTargetInspection() suppresses the failure
  // ─────────────────────────────────────────────────────────────────────────

  [Test]
  public async Task ValidateExternalInputsAsync_SkipTargetInspection_SuppressesFailure()
  {
    // Arrange: failing target, but explicitly opted out of target validation.
    var input = ItemFactory.Enumerable.Memory<TestData>("input");
    var output = new Item<IEnumerable<TestData>>("output", new FailingTargetAdapter("output"));

    var flow = FlowBuilder.CreateFlow(builder =>
      builder.AddStep("Step", PassthroughStep.Create(), input, output)
    );
    flow.Build();

    // Skip source inspection — input has no data and this test is about the escape hatch.
    flow.ValidationOptions.Inspect(input, InspectionLevel.None);
    flow.ValidationOptions.SkipTargetInspection(output);

    // Act
    var result = await flow.ValidateExternalInputsAsync(cancellationToken: CancellationToken.None);

    // Assert: escape hatch suppresses the failure
    Assert.That(result.IsValid, Is.True);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Test doubles
  // ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// Adapter whose source inspection always succeeds and whose <c>InspectTarget()</c>
  /// always returns a <see cref="ValidationErrorType.WriteAccessDenied"/> failure.
  /// </summary>
  private sealed class FailingTargetAdapter : IStorageAdapter<IEnumerable<TestData>>
  {
    private readonly string _label;

    public FailingTargetAdapter(string label) => _label = label;

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(
        new ValidationResult(
          new[]
          {
            new ValidationError(
              _label,
              ValidationErrorType.WriteAccessDenied,
              $"Simulated write-destination failure for '{_label}'",
              null
            ),
          }
        )
      );
  }

  /// <summary>
  /// Adapter whose source inspection always succeeds and whose <c>InspectTarget()</c>
  /// also always succeeds — used to represent a healthy input that should not contribute errors.
  /// </summary>
  private sealed class PassingInspectionAdapter : IStorageAdapter<IEnumerable<TestData>>
  {
    private readonly string _label;

    public PassingInspectionAdapter(string label) => _label = label;

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(true);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }

  /// <summary>
  /// Adapter whose source inspection fails (simulates a bad external input).
  /// </summary>
  private sealed class FailingSourceAdapter : IStorageAdapter<IEnumerable<TestData>>
  {
    private readonly string _label;

    public FailingSourceAdapter(string label) => _label = label;

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(
        new ValidationResult(
          new[]
          {
            new ValidationError(
              _label,
              ValidationErrorType.NotFound,
              $"Simulated source failure for '{_label}'",
              null
            ),
          }
        )
      );

    public FlowIO<ValidationResult> InspectDeep() => InspectShallow(0);

    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }

  /// <summary>
  /// Adapter that invokes an <paramref name="onTargetCalled"/> callback each time
  /// <c>InspectTarget()</c> is invoked. Used to assert that the target pass was
  /// (or was not) reached.
  /// </summary>
  private sealed class CountingTargetAdapter : IStorageAdapter<IEnumerable<TestData>>
  {
    private readonly string _label;
    private readonly Action _onTargetCalled;

    public CountingTargetAdapter(string label, Action onTargetCalled)
    {
      _label = label;
      _onTargetCalled = onTargetCalled;
    }

    public StorageTraits Traits => new StorageTraits();

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectTarget()
    {
      _onTargetCalled();
      return FlowIO.Pure(ValidationResult.Success());
    }
  }

  /// <summary>
  /// Adapter with <c>Traits.CanInspect = false</c> whose <c>InspectTarget()</c> would
  /// fail if called. Used to verify that non-inspectable adapters are never probed.
  /// </summary>
  private sealed class NonInspectableFailingTargetAdapter : IStorageAdapter<IEnumerable<TestData>>
  {
    private readonly string _label;

    public NonInspectableFailingTargetAdapter(string label) => _label = label;

    public StorageTraits Traits => new StorageTraits { CanInspect = false };

    public FlowIO<IEnumerable<TestData>> Load() => FlowIO.Lift(() => Enumerable.Empty<TestData>());

    public FlowIO<FlowUnit> Save(IEnumerable<TestData> data) => FlowIO.Pure(FlowUnit.Default);

    public FlowIO<bool> Exists() => FlowIO.Pure(false);

    public FlowIO<ValidationResult> InspectShallow(int sampleSize) =>
      FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());

    public FlowIO<ValidationResult> InspectTarget() =>
      FlowIO.Pure(
        new ValidationResult(
          new[]
          {
            new ValidationError(
              _label,
              ValidationErrorType.WriteAccessDenied,
              $"Should never be called for '{_label}'",
              null
            ),
          }
        )
      );
  }
}
