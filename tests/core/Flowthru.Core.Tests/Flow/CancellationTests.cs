using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Cancellation-token propagation through the runtime. Reactivates
/// the legacy <c>CancellationTests</c> coverage against the new
/// shape: (1) CT flows through <see cref="FlowIO{A}.Run(CancellationToken)"/>;
/// (2) the executor checks <see cref="CancellationToken.IsCancellationRequested"/>
/// between steps; (3) async transforms that observe CT on cancel
/// produce <see cref="RuntimeError.Cancelled"/> rather than
/// <see cref="RuntimeError.External"/>.
/// </summary>
[TestFixture]
public class CancellationTests
{
  [Test]
  public async Task AlreadyCancelledToken_StopsBeforeFirstStep()
  {
    var input = ItemFactory.Singleton.Memory<int>("ct-already-in");
    var output = ItemFactory.Singleton.Memory<int>("ct-already-out");
    await input.Save(1).Run();

    var stepRan = false;
    var flow = FlowBuilder.CreateFlow("ct-already", b =>
      b.AddStep<int, int>("noop", x => { stepRan = true; return x; }, input, output)
    );

    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var result = await flow.RunAsync(
      new ExecutionOptions { ValidationDepth = ValidationDepth.None },
      cts.Token
    );

    Assert.That(stepRan, Is.False, "Pre-cancelled token should stop before any step runs.");
    Assert.That(result.HasFailures, Is.True);
    Assert.That(
      result.FirstFailure?.Error,
      Is.InstanceOf<RuntimeError.StepFailed>().Or.InstanceOf<RuntimeError.Cancelled>()
    );
    var actualError = result.FirstFailure!.Error;
    var inner = actualError is RuntimeError.StepFailed s ? s.Cause : actualError;
    Assert.That(inner, Is.InstanceOf<RuntimeError.Cancelled>(),
      "Pre-cancelled token should surface as RuntimeError.Cancelled (closed-sum tag)."
    );
  }

  [Test]
  public async Task FreshToken_RunsToCompletion()
  {
    var input = ItemFactory.Singleton.Memory<int>("ct-fresh-in");
    var output = ItemFactory.Singleton.Memory<int>("ct-fresh-out");
    await input.Save(2).Run();

    var flow = FlowBuilder.CreateFlow("ct-fresh", b =>
      b.AddStep<int, int>("double", x => x * 2, input, output)
    );

    using var cts = new CancellationTokenSource();
    var result = await flow.RunAsync(ExecutionOptions.Default, cts.Token);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(((EffResult<int>.Success)await output.Load().Run()).Value, Is.EqualTo(4));
  }

  [Test]
  public async Task CancelledBetweenSteps_RemainingStepsSkippedOrCancelled()
  {
    var stage1 = ItemFactory.Singleton.Memory<int>("ct-mid-stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("ct-mid-stage2");
    var stage3 = ItemFactory.Singleton.Memory<int>("ct-mid-stage3");
    var stage4 = ItemFactory.Singleton.Memory<int>("ct-mid-stage4");
    await stage1.Save(1).Run();

    using var cts = new CancellationTokenSource();
    var step2Ran = false;
    var step3Ran = false;

    var flow = FlowBuilder.CreateFlow("ct-mid", b =>
    {
      b.AddStep<int, int>(
        "step1",
        x => { cts.Cancel(); return x; },
        stage1,
        stage2
      );
      b.AddStep<int, int>("step2", x => { step2Ran = true; return x; }, stage2, stage3);
      b.AddStep<int, int>("step3", x => { step3Ran = true; return x; }, stage3, stage4);
    });

    var result = await flow.RunAsync(
      new ExecutionOptions { ValidationDepth = ValidationDepth.None },
      cts.Token
    );

    Assert.That(step2Ran, Is.False,
      "After cancellation between steps, step2 should not run."
    );
    Assert.That(step3Ran, Is.False, "Step 3 should not run either.");
  }

  [Test]
  public async Task CancelledDuringAsyncTransform_AwaitsCancellationCleanly()
  {
    var input = ItemFactory.Singleton.Memory<int>("ct-during-in");
    var output = ItemFactory.Singleton.Memory<int>("ct-during-out");
    await input.Save(1).Run();

    using var cts = new CancellationTokenSource();
    var flow = FlowBuilder.CreateFlow("ct-during", b =>
      b.AddStep<int, int>(
        "long-running",
        async (x, ct) =>
        {
          cts.Cancel();
          // Wait for the cancellation signal to be observed.
          await Task.Delay(TimeSpan.FromSeconds(5), ct);
          return x;
        },
        input,
        output
      )
    );

    var result = await flow.RunAsync(
      new ExecutionOptions { ValidationDepth = ValidationDepth.None },
      cts.Token
    );
    Assert.That(result.HasFailures, Is.True);

    var error = result.FirstFailure!.Error;
    var inner = error is RuntimeError.StepFailed s ? s.Cause : error;
    Assert.That(inner, Is.InstanceOf<RuntimeError.Cancelled>(),
      "Async transform that throws OperationCanceledException should surface as RuntimeError.Cancelled."
    );
  }

  [Test]
  public async Task CancelledDuringIOLoad_FailsAsCancelled()
  {
    // A custom storage adapter that observes CT during Load —
    // simulates a slow IO source that respects cancellation.
    using var cts = new CancellationTokenSource();
    var slowInput = new SlowMemoryItem<int>("ct-slow-load", _ =>
    {
      cts.Cancel();
      return Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
    });
    var output = ItemFactory.Singleton.Memory<int>("ct-slow-load-out");

    var flow = FlowBuilder.CreateFlow("ct-load", b =>
      b.AddStep<int, int>("identity", x => x, slowInput, output)
    );

    var result = await flow.RunAsync(
      new ExecutionOptions { ValidationDepth = ValidationDepth.None },
      cts.Token
    );

    Assert.That(result.HasFailures, Is.True);
    var error = result.FirstFailure!.Error;
    var inner = error is RuntimeError.StepFailed s ? s.Cause : error;
    Assert.That(inner, Is.InstanceOf<RuntimeError.Cancelled>(),
      "Cancelled-during-IO-load should surface as RuntimeError.Cancelled."
    );
  }

  /// <summary>
  /// Memory-backed item whose <c>Load</c> call awaits a caller-supplied
  /// async hook before returning data — lets the test inject a
  /// long-running cancellable IO operation.
  /// </summary>
  private sealed class SlowMemoryItem<T> : IItem<T>
  {
    private readonly Func<CancellationToken, Task> _onLoad;
    private T? _value;

    public SlowMemoryItem(string label, Func<CancellationToken, Task> onLoad)
    {
      Label = label;
      _onLoad = onLoad;
    }

    public string Label { get; }
    public Flowthru.Data.Catalog.NodeTraits Traits => new();

    public FlowIO<T> Load() => FlowIO.LiftAsync(async ct =>
    {
      await _onLoad(ct).ConfigureAwait(false);
      return _value ?? throw new InvalidOperationException("No value saved");
    });

    public FlowIO<FlowUnit> Save(T data) =>
      FlowIO.Lift(() => { _value = data; return FlowUnit.Default; });

    public FlowIO<bool> Exists() => FlowIO.Pure(_value is not null);

    public FlowIO<Flowthru.Data.Storage.ValidationResult> Validate() =>
      FlowIO.Pure(Flowthru.Data.Storage.ValidationResult.Success());

    public FlowIO<Flowthru.Data.Storage.ValidationResult> InspectShallow(int sampleSize = 100) =>
      FlowIO.Pure(Flowthru.Data.Storage.ValidationResult.Success());
    public FlowIO<Flowthru.Data.Storage.ValidationResult> InspectDeep() =>
      FlowIO.Pure(Flowthru.Data.Storage.ValidationResult.Success());
    public FlowIO<Flowthru.Data.Storage.ValidationResult> InspectTarget() =>
      FlowIO.Pure(Flowthru.Data.Storage.ValidationResult.Success());
  }
}
