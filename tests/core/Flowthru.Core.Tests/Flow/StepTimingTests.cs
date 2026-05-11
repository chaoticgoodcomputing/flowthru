using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Verifies the Phase 8.0.7 timing surface: every
/// <see cref="StepResult.Succeeded"/> and
/// <see cref="StepResult.Failed"/> the scheduler emits carries a
/// non-negative <c>Duration</c>; <see cref="FlowResult.Duration"/>
/// is at least the sum of the recorded step durations on a
/// sequential run; the metadata pipeline downstream of the scheduler
/// can rely on these fields.
/// </summary>
[TestFixture]
public class StepTimingTests
{
  [Test]
  public async Task SucceededStep_HasPositiveDuration()
  {
    var output = ItemFactory.Singleton.Memory<int>("computed");
    var flow = FlowBuilder.CreateFlow("timing", b =>
    {
      b.AddStep<int>("compute", () =>
      {
        // Trivial work that's nonetheless > 0ms wall-clock — Stopwatch
        // resolution on every supported runtime is well under 1ms.
        Thread.Sleep(2);
        return 42;
      }, output);
    });

    var result = await flow.RunAsync();

    Assert.That(result.IsSuccess, Is.True);
    var step = (StepResult.Succeeded)result.StepResults.Single();
    Assert.That(step.Duration, Is.GreaterThan(TimeSpan.Zero),
      "The scheduler should record a non-zero duration for any step "
      + "that actually executes.");
  }

  [Test]
  public async Task FailedStep_AlsoCarriesDuration()
  {
    var input = ItemFactory.Singleton.Memory<int>("input");
    var output = ItemFactory.Singleton.Memory<int>("output");
    await input.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("failing", b =>
    {
      b.AddStep<int, int>("divide", x => 100 / x, input, output);
    });

    var result = await flow.RunAsync();

    Assert.That(result.HasFailures, Is.True);
    var step = (StepResult.Failed)result.StepResults.Single();
    Assert.That(step.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero),
      "Even a failing step records the time spent before the failure.");
  }

  [Test]
  public async Task SkippedStep_NoDurationField()
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    await raw.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("skip-flow", b =>
    {
      b.AddStep<int, int>("explode", x => 100 / x, raw, stage1);
      b.AddStep<int, int>("downstream", x => x + 1, stage1, stage2);
    });
    var result = await flow.RunAsync();

    var skipped = result.StepResults.OfType<StepResult.Skipped>().Single();
    Assert.That(skipped.StepLabel, Is.EqualTo("downstream"));
    // No Duration property to check — Skipped is structurally untimed.
    Assert.That(skipped.Reason, Is.Not.Empty);
  }

  [Test]
  public async Task FlowResult_DurationIsAtLeastSumOfStepDurations()
  {
    var raw = ItemFactory.Singleton.Memory<int>("raw");
    var stage1 = ItemFactory.Singleton.Memory<int>("stage1");
    var stage2 = ItemFactory.Singleton.Memory<int>("stage2");
    await raw.Save(1).Run();

    var flow = FlowBuilder.CreateFlow("multi-step", b =>
    {
      b.AddStep<int, int>("a", x => { Thread.Sleep(2); return x + 1; }, raw, stage1);
      b.AddStep<int, int>("b", x => { Thread.Sleep(2); return x * 2; }, stage1, stage2);
    });

    var result = await flow.RunAsync();
    Assert.That(result.IsSuccess, Is.True);

    var sum = result.StepResults
      .OfType<StepResult.Succeeded>()
      .Select(s => s.Duration)
      .Aggregate(TimeSpan.Zero, (acc, d) => acc + d);

    Assert.That(result.Duration, Is.GreaterThanOrEqualTo(sum),
      "Sequential run: total duration must envelope the sum of step durations.");
    Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
  }

  [Test]
  public async Task EmptyFlow_HasZeroOrTinyDuration()
  {
    var flow = FlowBuilder.CreateFlow("empty", _ => { });
    var result = await flow.RunAsync();
    Assert.That(result.StepResults, Is.Empty);
    Assert.That(result.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
  }
}
