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

  // running/maxRunning tracker — each step bumps a shared counter on entry,
  // records the peak, delays so overlap is observable, decrements on exit.
  private static (Func<int, FlowIO<int>> Track, Func<int> Max) MakeTracker(string source)
  {
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
      source: source
    );
    return (track, () => maxRunning);
  }

  [Test]
  public async Task WritesToSharedResource_Serialize()
  {
    // Two steps each WRITE a distinct item backed by the same serial
    // resource (write capacity 1) → they must serialize. Models concurrent
    // SQLite writes to one database reaching the scheduler via the items.
    var root = ItemFactory.Singleton.Memory<int>("cg-w-root");
    await root.Save(0).Run();
    var outA = new DepItem("cg-w-a", ServiceDependency.Of<ISerialResource>());
    var outB = new DepItem("cg-w-b", ServiceDependency.Of<ISerialResource>());

    var (track, max) = MakeTracker("cg:w");
    IStepNode Step(string label, IItem<int> output) =>
      new Step<int, int>(label, track, new IItem[] { root }, new IItem[] { output },
        loadInputs: () => root.Load(), saveOutputs: v => output.Save(v));

    var flow = FlowBuilder.CreateFlow("cg-write", b => { b.Add(Step("w-a", outA)); b.Add(Step("w-b", outB)); });
    var result = await new ParallelFlowScheduler(profiles: new SerialResourceProvider())
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(max(), Is.EqualTo(1),
      "Two steps writing items backed by the same capacity-1 resource must serialize."
    );
  }

  [Test]
  public async Task ReadsFromSharedResource_RunConcurrently()
  {
    // Two steps READ the same item backed by the serial resource. Reads are
    // unbounded (SQLite allows many readers), so they parallelize — proving
    // read/write asymmetry: the write key gates, the read key does not.
    var shared = new DepItem("cg-r-shared", ServiceDependency.Of<ISerialResource>());
    var outA = ItemFactory.Singleton.Memory<int>("cg-r-a");
    var outB = ItemFactory.Singleton.Memory<int>("cg-r-b");

    var (track, max) = MakeTracker("cg:r");
    IStepNode Step(string label, IItem<int> output) =>
      new Step<int, int>(label, track, new IItem[] { shared }, new IItem[] { output },
        loadInputs: () => shared.Load(), saveOutputs: v => output.Save(v));

    var flow = FlowBuilder.CreateFlow("cg-read", b => { b.Add(Step("r-a", outA)); b.Add(Step("r-b", outB)); });
    var result = await new ParallelFlowScheduler(profiles: new SerialResourceProvider())
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(max(), Is.EqualTo(2),
      "Concurrent reads of a shared resource must parallelize — reads aren't gated by the write capacity."
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
