using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Pins the caching stopgap: a DuckDB transform step is uncacheable —
/// its SQL isn't part of the cache identity, so caching would risk
/// stale hits after a query edit — and the opt-out is <em>loud</em>:
/// the step declares a reason that the cache plan records verbatim and
/// every reason consumer (pre-flight logging, the JSON projection)
/// renders through <see cref="StepUncacheableReason.Describe"/>.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbUncacheableReasonTests
{
  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-cache-{Guid.NewGuid():N}");
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

  [Test]
  public void TransformStep_DeclaresALoudUncacheableReason()
  {
    var step = MakeStep("declared");

    Assert.That(step.DeclaredUncacheableReason, Is.Not.Null,
      "The step must opt out of caching explicitly — never silently.");
    Assert.That(step.DeclaredUncacheableReason,
      Is.InstanceOf<StepUncacheableReason.DeclaredByStep>());
    Assert.That(step.DeclaredUncacheableReason!.Describe(), Does.Contain("SQL"),
      "The rendered reason must say WHY: the SQL text isn't in the cache identity.");
  }

  [Test]
  public async Task CachePlan_MarksTransformUncacheable_WithTheDeclaredReason()
  {
    var step = MakeStep("plan");
    var flow = FlowBuilder.CreateFlow("duckdb-cache-plan", b => b.Add(step));

    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty);

    Assert.That(plan.UncacheableStepLabels, Does.Contain("plan"));
    Assert.That(plan.UncacheableReasons["plan"],
      Is.InstanceOf<StepUncacheableReason.DeclaredByStep>(),
      "The plan must carry the step's own declared reason — not NoCodeVersion or a "
      + "service-dependency verdict — so the surfaced explanation is the true one.");
    Assert.That(plan.UncacheableReasons["plan"].Describe(), Does.Contain("SQL"));
  }

  [Test]
  public async Task EngineDependency_IsNotWhatMakesTheStepUncacheable()
  {
    // The engine dependency resolves cache-neutral (AffectsOutputs =
    // false), so when the declared opt-out is later lifted by a
    // query-aware cache identity, no profile change is needed. Pin that
    // by resolving the plan WITH the contributor: the reason must be
    // the declaration, not HasServiceDependencies.
    var engine = new InProcessDuckDbEngine();
    var step = MakeStep("neutral", engine);
    var flow = FlowBuilder.CreateFlow("duckdb-cache-neutral", b => b.Add(step));

    var profiles = new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new DuckDbEngineProfileContributor(engine),
    });
    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty, profiles);

    Assert.That(plan.UncacheableReasons["neutral"],
      Is.InstanceOf<StepUncacheableReason.DeclaredByStep>(),
      "With the engine profiled cache-neutral, the ONLY thing holding the step out of "
      + "the cache is its own declaration.");
  }

  private DuckDbTransformStep<EventRow> MakeStep(
    string label, IDuckDbEngine? engine = null
  )
  {
    var input = ItemFactory.Enumerable.Parquet<EventRow>(
      $"{label}-in", SysIO.Path.Combine(_root, $"{label}-in.parquet"));
    var output = ItemFactory.Enumerable.Parquet<EventRow>(
      $"{label}-out", SysIO.Path.Combine(_root, $"{label}-out.parquet"));

    return new DuckDbTransformStep<EventRow>(
      label: label,
      sql: $"SELECT * FROM \"{label}-in\"",
      inputs: new[] { DuckDbInputRelation.From(input) },
      output: output,
      engine: engine ?? new InProcessDuckDbEngine()
    );
  }
}
