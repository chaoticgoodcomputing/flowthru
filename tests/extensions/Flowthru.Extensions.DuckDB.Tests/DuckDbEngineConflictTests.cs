using Flowthru.Data.Catalog;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the engine's scheduler profile: every DuckDB transform step
/// declares the shared <see cref="IDuckDbEngine"/> as a service
/// dependency, and <see cref="DuckDbEngineProfileContributor"/> resolves
/// that dependency to the engine's
/// <see cref="IDuckDbEngine.MaxConcurrency"/> capacity — cache-neutral,
/// concurrency-constrained. End to end, two independent transforms
/// backed by a serial engine must not co-run under the
/// <see cref="ParallelFlowScheduler"/>.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbEngineConflictTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-conflict-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_root))
    {
      try { SysIO.Directory.Delete(_root, recursive: true); }
      catch { /* best effort */ }
    }
  }

  // ── The step carries the engine dependency ──────────────────────────────

  [Test]
  public void TransformStep_DeclaresEngine_AsServiceDependency()
  {
    var step = MakeStep("dep", new RecordingEngine(), "dep");

    Assert.That(
      step.ServiceDependencies,
      Has.Some.Matches<ServiceDependency>(
        d => d is ServiceDependency.CSharp cs && cs.ServiceType == typeof(IDuckDbEngine)),
      "Every DuckDB transform must declare IDuckDbEngine so the scheduler can gate it."
    );
  }

  // ── Contributor maps the engine dep to its capacity ─────────────────────

  [Test]
  public void Contributor_ResolvesEngineDep_ToMaxConcurrency_CacheNeutral()
  {
    var contributor = new DuckDbEngineProfileContributor(new RecordingEngine(maxConcurrency: 3));

    var profile = contributor.Contribute(ServiceDependency.Of<IDuckDbEngine>());

    Assert.That(profile, Is.Not.Null, "The contributor must recognise the engine dependency.");
    Assert.That(profile!.Capacity, Is.EqualTo(3),
      "Capacity must reflect the resolved engine's MaxConcurrency, not a hardcoded value.");
    Assert.That(profile.AffectsOutputs, Is.False,
      "The engine is cache-neutral — a transform's determinism lives in its SQL and "
      + "inputs, not the engine instance's identity.");
  }

  [Test]
  public void Contributor_StaysSilent_OnUnrelatedDependency()
  {
    var contributor = new DuckDbEngineProfileContributor(new RecordingEngine());

    Assert.That(
      contributor.Contribute(ServiceDependency.Of<IDisposable>()),
      Is.Null,
      "A contributor speaks only for its own resource — null lets the composite provider "
      + "fall through."
    );
  }

  // ── End-to-end gating under the scheduler ───────────────────────────────

  [Test]
  public async Task Transforms_SharingSerialEngine_SerializeUnderParallelism()
  {
    var maxConcurrent = await RunTwoIndependentTransformsAsync(maxConcurrency: 1);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "A capacity-1 engine must hold two independent transforms apart even at Parallelism=4."
    );
  }

  [Test]
  public async Task Transforms_ConcurrentCapableEngine_RunConcurrently()
  {
    // Control: raise the engine's MaxConcurrency to 2 and the same two
    // transforms overlap — proving the gate reads MaxConcurrency
    // dynamically, and that the harness genuinely observes overlap (so
    // the serial assertion above is real).
    var maxConcurrent = await RunTwoIndependentTransformsAsync(maxConcurrency: 2);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "An engine declaring MaxConcurrency=2 must let two transforms co-run."
    );
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private async Task<int> RunTwoIndependentTransformsAsync(int maxConcurrency)
  {
    var engine = new RecordingEngine(maxConcurrency);

    var flow = FlowBuilder.CreateFlow($"duckdb-cg-{maxConcurrency}", b =>
    {
      b.Add(MakeStep("transform-a", engine, $"cg-a-{maxConcurrency}"));
      b.Add(MakeStep("transform-b", engine, $"cg-b-{maxConcurrency}"));
    });

    var profiles = new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new DuckDbEngineProfileContributor(engine),
    });
    var result = await new ParallelFlowScheduler(profiles: profiles)
      .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

    Assert.That(result.IsSuccess, Is.True, "both transforms should succeed");
    return engine.MaxObserved;
  }

  /// <summary>
  /// Builds a transform step over real (byte-addressable) Parquet items;
  /// the recording engine never touches the files, so nothing is seeded.
  /// </summary>
  private DuckDbTransformStep<EventRow> MakeStep(
    string label, IDuckDbEngine engine, string filePrefix
  )
  {
    var input = ItemFactory.Enumerable.Parquet<EventRow>(
      $"{filePrefix}-in", SysIO.Path.Combine(_root, $"{filePrefix}-in.parquet"));
    var output = ItemFactory.Enumerable.Parquet<EventRow>(
      $"{filePrefix}-out", SysIO.Path.Combine(_root, $"{filePrefix}-out.parquet"));

    return new DuckDbTransformStep<EventRow>(
      label: label,
      sql: $"SELECT * FROM \"{filePrefix}-in\"",
      inputs: new[] { DuckDbInputRelation.From(input) },
      output: output,
      engine: engine
    );
  }

  /// <summary>
  /// Test engine whose <see cref="ExecuteTransform"/> records the peak
  /// number of concurrent invocations, sleeping briefly so overlap is
  /// observable. Declares a configurable <see cref="MaxConcurrency"/> so
  /// the gating capacity can be exercised both at the serial floor and
  /// above it.
  /// </summary>
  private sealed class RecordingEngine : IDuckDbEngine
  {
    private readonly int _maxConcurrency;
    private int _running;
    private int _max;
    private readonly object _gate = new();

    public RecordingEngine(int maxConcurrency = 1) => _maxConcurrency = maxConcurrency;

    public int MaxConcurrency => _maxConcurrency;
    public string EngineVersion => "recording-engine-v1";
    public int MaxObserved { get { lock (_gate) return _max; } }

    public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request) =>
      FlowIO.LiftAsync(
        async ct =>
        {
          var now = Interlocked.Increment(ref _running);
          lock (_gate) _max = Math.Max(_max, now);
          await Task.Delay(60, ct).ConfigureAwait(false); // window for overlap to surface
          Interlocked.Decrement(ref _running);
          return new DuckDbTransformResult(0, Array.Empty<(string, string)>());
        },
        source: "duckdb:rec"
      );
  }
}
