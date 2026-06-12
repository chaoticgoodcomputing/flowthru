using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.Python;
using Flowthru.Validation.PreFlight;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.Python;

namespace Flowthru.Extensions.Python.Tests;

/// <summary>
/// Pins #101's conflict-gating wiring: every Python step declares the
/// shared <see cref="IPythonExecutor"/> as a service dependency, and
/// <see cref="PythonExecutorProfileContributor"/> resolves that dependency
/// to the executor's <see cref="IPythonExecutor.MaxConcurrency"/> capacity
/// (cache-neutral). End to end, two independent Python steps backed by a
/// serial executor must not co-run under the
/// <see cref="ParallelFlowScheduler"/>. (ADR-0019.)
/// </summary>
[TestFixture]
[Category("Python")]
public class PythonExecutorConflictTests
{
  // ── PythonStep carries the executor dependency ─────────────────────────

  [Test]
  public void PythonStep_DeclaresExecutor_AsServiceDependency()
  {
    var step = MakeStep("s", new RecordingExecutor(),
      ItemFactory.Singleton.Memory<int>("dep-in"),
      ItemFactory.Singleton.Memory<int>("dep-out"));

    Assert.That(
      step.ServiceDependencies,
      Has.Some.Matches<ServiceDependency>(
        d => d is ServiceDependency.CSharp cs && cs.ServiceType == typeof(IPythonExecutor)),
      "Every Python step must declare IPythonExecutor so the scheduler can gate it — "
      + "appended at the PythonStep construction chokepoint."
    );
  }

  [Test]
  public void PythonStep_PreservesCallerDeclaredDeps_AndAppendsExecutor()
  {
    var declared = new ServiceDependency[]
    {
      new ServiceDependency.External(new PythonServiceDependency("Services.Foo")),
    };
    var step = MakeStep("s", new RecordingExecutor(),
      ItemFactory.Singleton.Memory<int>("dep2-in"),
      ItemFactory.Singleton.Memory<int>("dep2-out"),
      services: declared);

    Assert.Multiple(() =>
    {
      Assert.That(
        step.ServiceDependencies,
        Has.Some.Matches<ServiceDependency>(
          d => d is ServiceDependency.External ext && ext.Cause.DagId == "python:Services.Foo"),
        "Caller-declared @step(services=[…]) refs must survive the executor append.");
      Assert.That(
        step.ServiceDependencies,
        Has.Some.Matches<ServiceDependency>(
          d => d is ServiceDependency.CSharp cs && cs.ServiceType == typeof(IPythonExecutor)),
        "The executor dependency is appended alongside, not in place of, declared refs.");
    });
  }

  // ── Contributor maps the executor dep to its capacity ──────────────────

  [Test]
  public void Contributor_ResolvesExecutorDep_ToMaxConcurrency_CacheNeutral()
  {
    var contributor = new PythonExecutorProfileContributor(new RecordingExecutor(maxConcurrency: 3));

    var profile = contributor.Contribute(ServiceDependency.Of<IPythonExecutor>());

    Assert.That(profile, Is.Not.Null, "The contributor must recognise the executor dependency.");
    Assert.That(profile!.Capacity, Is.EqualTo(3),
      "Capacity must reflect the resolved executor's MaxConcurrency, not a hardcoded value.");
    Assert.That(profile.AffectsOutputs, Is.False,
      "The executor is cache-neutral — its identity adds nothing beyond the step's CodeVersion.");
  }

  [Test]
  public void Contributor_StaysSilent_OnUnrelatedDependency()
  {
    var contributor = new PythonExecutorProfileContributor(new RecordingExecutor());

    Assert.That(
      contributor.Contribute(ServiceDependency.Of<IDisposable>()),
      Is.Null,
      "A contributor speaks only for its own resource — null lets the composite provider fall through."
    );
  }

  // ── End-to-end serialization under the scheduler ───────────────────────

  [Test]
  public async Task PythonSteps_ShareSerialExecutor_SerializeUnderParallelism()
  {
    var maxConcurrent = await RunTwoIndependentPythonStepsAsync(maxConcurrency: 1);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "A capacity-1 executor must hold two independent Python steps apart even at Parallelism=4."
    );
  }

  [Test]
  public async Task PythonSteps_ConcurrentCapableExecutor_RunConcurrently()
  {
    // Control: raise the executor's MaxConcurrency to 2 and the same two
    // steps overlap — proving the gate reads MaxConcurrency dynamically
    // rather than pinning Python steps to serial, and that the harness
    // genuinely observes overlap (so the serial assertion above is real).
    var maxConcurrent = await RunTwoIndependentPythonStepsAsync(maxConcurrency: 2);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "An executor declaring MaxConcurrency=2 must let two Python steps co-run."
    );
  }

  // ── Harness ────────────────────────────────────────────────────────────

  private static async Task<int> RunTwoIndependentPythonStepsAsync(int maxConcurrency)
  {
    var executor = new RecordingExecutor(maxConcurrency);
    var root = ItemFactory.Singleton.Memory<int>($"py-cg-root-{maxConcurrency}");
    var outA = ItemFactory.Singleton.Memory<int>($"py-cg-a-{maxConcurrency}");
    var outB = ItemFactory.Singleton.Memory<int>($"py-cg-b-{maxConcurrency}");
    await root.Save(0).Run();

    var flow = FlowBuilder.CreateFlow($"py-cg-{maxConcurrency}", b =>
    {
      b.Add(MakeStep("py-a", executor, root, outA));
      b.Add(MakeStep("py-b", executor, root, outB));
    });

    var profiles = new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new PythonExecutorProfileContributor(executor),
    });
    var result = await new ParallelFlowScheduler(profiles: profiles)
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "both Python steps should succeed");
    return executor.MaxObserved;
  }

  private static PythonStep<int, int> MakeStep(
    string label,
    IPythonExecutor executor,
    IItem<int> input,
    IItem<int> output,
    IReadOnlyList<ServiceDependency>? services = null
  ) =>
    new PythonStep<int, int>(
      label: label,
      moduleName: "module",
      functionName: "fn",
      transform: x => executor.Invoke<int, int>("module", "fn", x),
      inputs: new IItem[] { input },
      outputs: new IItem[] { output },
      loadInputs: () => input.Load(),
      saveOutputs: v => output.Save(v),
      serviceDependencies: services
    );

  /// <summary>
  /// Test executor whose <see cref="Invoke{TInput, TOutput}"/> records the
  /// peak number of concurrent invocations, sleeping briefly so overlap is
  /// observable. Declares a configurable <see cref="MaxConcurrency"/> so
  /// the gating capacity can be exercised both at the serial floor and above.
  /// </summary>
  private sealed class RecordingExecutor : IPythonExecutor
  {
    private readonly int _maxConcurrency;
    private int _running;
    private int _max;
    private readonly object _gate = new();

    public RecordingExecutor(int maxConcurrency = 1) => _maxConcurrency = maxConcurrency;

    public int MaxConcurrency => _maxConcurrency;
    public int MaxObserved { get { lock (_gate) return _max; } }

    public FlowIO<TOutput> Invoke<TInput, TOutput>(
      string moduleName,
      string functionName,
      TInput input
    ) => FlowIO.LiftAsync(
      async ct =>
      {
        var now = Interlocked.Increment(ref _running);
        lock (_gate) _max = Math.Max(_max, now);
        await Task.Delay(60, ct).ConfigureAwait(false); // window for overlap to surface
        Interlocked.Decrement(ref _running);
        return (TOutput)(object)input!; // identity passthrough — TInput == TOutput in these tests
      },
      source: "py:rec"
    );

    public FlowIO<PythonStepMetadata> ValidateStep(string moduleName, string functionName) =>
      FlowIO.Fail<PythonStepMetadata>(
        new RuntimeError.InvariantViolated("RecordingExecutor", "ValidateStep not used"));

    public FlowIO<Validated<PreFlightError, FlowUnit>> InvokeInspector(
      PythonServiceRegistration registration
    ) => FlowIO.Pure(Validated<PreFlightError, FlowUnit>.Pure(FlowUnit.Default));
  }
}
