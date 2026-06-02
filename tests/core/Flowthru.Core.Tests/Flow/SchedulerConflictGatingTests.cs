using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
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

  [Test]
  public async Task ItemDeclaredCapacityOne_SerializesStepsTouchingIt()
  {
    // Neither step declares a service dependency; the conflict comes from
    // the shared INPUT item, which declares a capacity-1 resource. Proves
    // a step inherits the conflict keys of the items it touches — the
    // INode lift, and how EFCore/SQLite contention will reach the scheduler.
    var shared = new DepItem("cg-shared-item", ServiceDependency.Of<ISerialResource>());
    var outA = ItemFactory.Singleton.Memory<int>("cg-item-out-a");
    var outB = ItemFactory.Singleton.Memory<int>("cg-item-out-b");

    var running = 0;
    var maxRunning = 0;
    var gate = new object();
    Func<int, FlowIO<int>> track = x => FlowIO.LiftAsync(
      async ct =>
      {
        var now = Interlocked.Increment(ref running);
        lock (gate) maxRunning = Math.Max(maxRunning, now);
        await Task.Delay(60, ct).ConfigureAwait(false);
        Interlocked.Decrement(ref running);
        return x;
      },
      source: "cg:item-track"
    );

    IStepNode Step(string label, IItem<int> output) =>
      new Step<int, int>(
        label: label,
        transform: track,
        inputs: new IItem[] { shared },
        outputs: new IItem[] { output },
        loadInputs: () => shared.Load(),
        saveOutputs: v => output.Save(v)
      );

    var flow = FlowBuilder.CreateFlow("cg-item-flow", b =>
    {
      b.Add(Step("reader-a", outA));
      b.Add(Step("reader-b", outB));
    });

    var scheduler = new ParallelFlowScheduler(profiles: new SerialResourceProvider());
    var result = await scheduler.ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(maxRunning, Is.EqualTo(1),
      "Two steps reading a shared capacity-1 item must serialize — proves item-declared "
      + "conflict keys are inherited by the steps that touch the item."
    );
  }

  /// <summary>Minimal test item that declares service dependencies (for item-derived gating).</summary>
  private sealed class DepItem : IItem<int>
  {
    private readonly IReadOnlyList<ServiceDependency> _deps;
    public DepItem(string label, params ServiceDependency[] deps) { Label = label; _deps = deps; }
    public string Label { get; }
    public NodeTraits Traits { get; } = new();
    public IReadOnlyList<ServiceDependency> ServiceDependencies => _deps;
    public FlowIO<ValidationResult> Validate() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<int> Load() => FlowIO.Pure(0);
    public FlowIO<FlowUnit> Save(int data) => FlowIO.Pure(FlowUnit.Default);
    public FlowIO<bool> Exists() => FlowIO.Pure(true);
    public FlowIO<ValidationResult> InspectShallow(int sampleSize = 100) => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectDeep() => FlowIO.Pure(ValidationResult.Success());
    public FlowIO<ValidationResult> InspectTarget() => FlowIO.Pure(ValidationResult.Success());
  }
}
