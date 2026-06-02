using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;

namespace Flowthru.Core.Tests.Flow;

/// <summary>
/// Tests for <see cref="ParallelFlowScheduler"/> conflict gating —
/// the scheduler admits at most a resource's declared
/// <see cref="ServiceProfile.Capacity"/> concurrent holders of a
/// conflict key, derived from a step's
/// <see cref="IStepNode.ServiceDependencies"/>. Two steps that share a
/// capacity-1 service must serialize even under high parallelism, while
/// steps with no constrained dependency run concurrently. (ADR-0019.)
/// </summary>
[TestFixture]
public class SchedulerConflictGatingTests
{
  /// <summary>Marker for a fictional serial resource a step depends on.</summary>
  private interface ISerialResource { }

  /// <summary>Returns capacity 1 for <see cref="ISerialResource"/>; unbounded otherwise.</summary>
  private sealed class SerialResourceProvider : IServiceProfileProvider
  {
    private static readonly string SerialId = ServiceDependency.Of<ISerialResource>().DagId;

    public ServiceProfile Resolve(ServiceDependency dependency) =>
      dependency.DagId == SerialId ? new ServiceProfile { Capacity = 1 } : ServiceProfile.Unbounded;
  }

  // A flow of two independent steps (both read the same root, write
  // distinct outputs) — so they're both initially ready and would
  // co-run at Parallelism >= 2 unless a conflict key holds them apart.
  private static async Task<int> RunTwoIndependentStepsAsync(bool declareSerialDep)
  {
    var root = ItemFactory.Singleton.Memory<int>($"cg-root-{declareSerialDep}");
    var outA = ItemFactory.Singleton.Memory<int>($"cg-a-{declareSerialDep}");
    var outB = ItemFactory.Singleton.Memory<int>($"cg-b-{declareSerialDep}");
    await root.Save(0).Run();

    var running = 0;
    var maxRunning = 0;
    var gate = new object();

    Func<int, FlowIO<int>> track = x => FlowIO.LiftAsync(
      async ct =>
      {
        var now = Interlocked.Increment(ref running);
        lock (gate) maxRunning = Math.Max(maxRunning, now);
        await Task.Delay(60, ct).ConfigureAwait(false); // window for overlap to be observable
        Interlocked.Decrement(ref running);
        return x;
      },
      source: "cg:track"
    );

    var deps = declareSerialDep ? new[] { ServiceDependency.Of<ISerialResource>() } : null;

    IStepNode Step(string label, IItem<int> output) =>
      new Step<int, int>(
        label: label,
        transform: track,
        inputs: new IItem[] { root },
        outputs: new IItem[] { output },
        loadInputs: () => root.Load(),
        saveOutputs: v => output.Save(v),
        serviceDependencies: deps
      );

    var flow = FlowBuilder.CreateFlow("cg-flow", b =>
    {
      b.Add(Step("step-a", outA));
      b.Add(Step("step-b", outB));
    });

    var scheduler = new ParallelFlowScheduler(profiles: new SerialResourceProvider());
    var result = await scheduler.ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "both steps should succeed");
    return maxRunning;
  }

  [Test]
  public async Task CapacityOne_SharedServiceDep_SerializesSteps()
  {
    var maxConcurrent = await RunTwoIndependentStepsAsync(declareSerialDep: true);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "Two steps sharing a capacity-1 service must never co-run, even at Parallelism=4."
    );
  }

  [Test]
  public async Task NoConstrainedDep_StepsRunConcurrently()
  {
    var maxConcurrent = await RunTwoIndependentStepsAsync(declareSerialDep: false);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "With no constrained dependency, two independent steps run concurrently at Parallelism=4 — "
      + "proves the harness observes overlap, so the serialized case above is meaningful."
    );
  }
}
