using System.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Diagnostics;
using Flowthru.Flow;
using Flowthru.Prelude;

namespace Flowthru.Core.Tests.Diagnostics;

/// <summary>
/// Asserts the Core runtime emits the expected
/// <see cref="FlowthruActivitySource"/> trace spans. Per ADR-0006,
/// these activities exist for distributed-tracing consumers; engine
/// logs are emitted directly via <c>ILogger&lt;TSelf&gt;</c>, not
/// through the retired CLI activity bridge.
/// </summary>
[TestFixture]
public class ActivitySourceTests
{
  /// <summary>
  /// Subscribes to the Flowthru source for the duration of one
  /// flow run and returns every activity emitted.
  /// </summary>
  private static async Task<IReadOnlyList<Activity>> RunAndCaptureActivities(
    Func<Task> action
  )
  {
    var captured = new List<Activity>();
    using var listener = new ActivityListener
    {
      ShouldListenTo = src => src.Name == FlowthruActivitySource.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
      ActivityStopped = activity =>
      {
        lock (captured) captured.Add(activity);
      },
    };
    ActivitySource.AddActivityListener(listener);
    await action().ConfigureAwait(false);
    return captured;
  }

  [Test]
  public async Task TaskGraphExecutor_EmitsStepActivityPerStep()
  {
    var input = ItemFactory.Singleton.Memory<int>("a-input");
    var middle = ItemFactory.Singleton.Memory<int>("a-mid");
    var output = ItemFactory.Singleton.Memory<int>("a-out");
    await input.Save(10).Run();

    var flow = FlowBuilder.CreateFlow("two-step", b =>
    {
      b.AddStep<int, int>("plus-one", x => x + 1, input, middle);
      b.AddStep<int, int>("times-two", x => x * 2, middle, output);
    });

    var activities = await RunAndCaptureActivities(async () =>
    {
      await flow.RunAsync();
    });

    var stepActivities = activities
      .Where(a => a.OperationName == FlowthruActivitySource.StepActivityName)
      .ToList();
    Assert.That(stepActivities, Has.Count.EqualTo(2),
      "Two steps in the flow → two flowthru.step activities.");

    var labels = stepActivities
      .Select(a => a.GetTagItem(FlowthruActivitySource.TagStepLabel) as string)
      .ToList();
    Assert.That(labels, Is.EquivalentTo(new[] { "plus-one", "times-two" }));

    Assert.That(
      stepActivities.All(a => a.Status == ActivityStatusCode.Ok),
      Is.True,
      "Both step activities should report Ok status."
    );
  }

  [Test]
  public async Task TaskGraphExecutor_FailedStep_SetsActivityStatusError()
  {
    var input = ItemFactory.Singleton.Memory<int>("b-input");
    var output = ItemFactory.Singleton.Memory<int>("b-out");
    await input.Save(0).Run();

    var flow = FlowBuilder.CreateFlow("boom", b =>
      b.AddStep<int, int>("explode", x => 100 / x, input, output)
    );

    var activities = await RunAndCaptureActivities(async () => { await flow.RunAsync(); });

    var stepActivity = activities
      .Single(a => a.OperationName == FlowthruActivitySource.StepActivityName);
    Assert.That(stepActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
    Assert.That(stepActivity.StatusDescription, Is.Not.Null.And.Not.Empty);
  }
}
