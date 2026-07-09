using Flowthru.Caching;
using Flowthru.Data.Catalog;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.DuckDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// Query-aware cache identity for the DuckDB transform (#138): the step
/// is first-class cacheable, with the wire-up data that decides its
/// output — the exact SQL text, the engine version, the relation
/// bindings, and the output-affecting write options — declared into its
/// cache key via <c>IStepNode.DeclaredCacheIdentity</c>. Covers the
/// identity's invalidation axes at the unit level, and cache hit /
/// invalidation / downstream cascade end-to-end through a real
/// <c>IFlowthruService</c> with a file-backed manifest.
/// </summary>
[TestFixture]
[Category("DuckDB")]
public class DuckDbCacheIdentityTests
{
  private string _root = null!;
  private string _cachePath = null!;
  private bool _downstreamInvoked;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-cache-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_root);
    _cachePath = SysIO.Path.Combine(_root, "cache.json");
    _downstreamInvoked = false;
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

  // ── Eligibility: the #135 stopgap is gone ───────────────────────────────

  [Test]
  public void TransformStep_IsCacheEligible_AndNoLongerDeclaresUncacheable()
  {
    var step = MakeStep("eligible");

    Assert.Multiple(() =>
    {
      Assert.That(((IStepNode)step).DeclaredUncacheableReason, Is.Null,
        "The #135 uncacheable stopgap must be gone — the SQL is in the key now.");
      Assert.That(((IStepNode)step).CodeVersion, Is.Not.Null,
        "The transform machinery's identity must be declared so the step passes "
        + "cache-plan eligibility.");
      Assert.That(((IStepNode)step).DeclaredCacheIdentity, Is.Not.Null.And.Not.Empty,
        "The wire-up data must be declared into the cache identity.");
    });
  }

  [Test]
  public async Task CachePlan_DoesNotReportTheTransformUncacheable()
  {
    await SeedEventsAsync();
    var engine = new InProcessDuckDbEngine();
    var catalog = new CacheCatalog(_root);
    var flow = FlowBuilder.CreateFlow("duckdb-cacheable", f => f.AddDuckDbTransform(
      "sort_events", catalog.Events, catalog.Sorted,
      "SELECT * FROM events ORDER BY Id", engine));

    var profiles = new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new DuckDbEngineProfileContributor(engine),
    });
    var plan = await CachePlanBuilder.BuildAsync(flow, CacheManifest.Empty, profiles);

    Assert.Multiple(() =>
    {
      Assert.That(plan.UncacheableStepLabels, Is.Empty,
        "Engine transforms must no longer appear in uncacheable reporting.");
      Assert.That(plan.StaleStepLabels, Does.Contain("sort_events"),
        "First sight of the step (empty manifest) is an ordinary stale miss.");
    });
  }

  [Test]
  public void InProcessEngine_ReportsTheLibraryVersion_OnceProbedStaysStable()
  {
    var first = new InProcessDuckDbEngine();
    var second = new InProcessDuckDbEngine(new DuckDbEngineOptions { Threads = 2 });

    Assert.Multiple(() =>
    {
      Assert.That(first.EngineVersion, Does.Match(@"^v?\d+\.\d+"),
        "The version must come from the embedded library (e.g. \"v1.5.3\") — it is "
        + "cache-identity data, not a placeholder.");
      Assert.That(second.EngineVersion, Is.EqualTo(first.EngineVersion),
        "Probed once per process: every in-process engine shares one native library.");
    });
  }

  // ── The identity's contents: each axis moves it, tuning doesn't ────────

  [Test]
  public void Identity_IsStable_ForIdenticalWireUp()
  {
    Assert.That(IdentityOf(MakeStep("a")), Is.EqualTo(IdentityOf(MakeStep("b"))),
      "Same SQL, same bindings, same engine, same options → same identity, "
      + "regardless of step label or instance.");
  }

  [Test]
  public void Identity_ChangesWithTheSqlText_ExactBytes()
  {
    var baseline = IdentityOf(MakeStep("s", sql: "SELECT * FROM events"));

    Assert.Multiple(() =>
    {
      Assert.That(IdentityOf(MakeStep("s", sql: "SELECT * FROM events ORDER BY Id")),
        Is.Not.EqualTo(baseline), "A semantic edit must invalidate.");
      Assert.That(IdentityOf(MakeStep("s", sql: "SELECT  *  FROM events")),
        Is.Not.EqualTo(baseline),
        "The SQL is hashed over its exact bytes — no normalization, so even a "
        + "whitespace-only edit invalidates rather than risking a stale hit on a "
        + "change a normalizer misjudged.");
    });
  }

  [Test]
  public void Identity_ChangesWithTheEngineVersion()
  {
    var v1 = IdentityOf(MakeStep("s", engine: new FixedVersionEngine("v1.5.3")));
    var v2 = IdentityOf(MakeStep("s", engine: new FixedVersionEngine("v1.6.0")));

    Assert.That(v1, Is.Not.EqualTo(v2),
      "An engine version bump must invalidate — query semantics and the engine's "
      + "Parquet writer can change between releases.");
  }

  [Test]
  public void Identity_ChangesWithOutputAffectingOptions()
  {
    var defaults = IdentityOf(MakeStep("s"));
    var zstd = IdentityOf(MakeStep(
      "s", options: new DuckDbTransformOptions { Compression = DuckDbParquetCompression.Zstd }));
    var rowGroups = IdentityOf(MakeStep(
      "s", options: new DuckDbTransformOptions { RowGroupSize = 10_000 }));

    Assert.Multiple(() =>
    {
      Assert.That(zstd, Is.Not.EqualTo(defaults),
        "The compression codec changes the output file's bytes — it belongs in the key.");
      Assert.That(rowGroups, Is.Not.EqualTo(defaults),
        "The row-group size changes the output file's layout (and downstream "
        + "streaming behaviour) — it belongs in the key.");
    });
  }

  [Test]
  public void Identity_ChangesWhenTheSameItemsRebindToDifferentRelationNames()
  {
    // Same SQL text, same input items — but which item answers to which
    // relation name is swapped, so the same query reads different data.
    // Input fingerprints can't see this; the identity must.
    var events = ParquetItem("events", "bind-events.parquet");
    var regions = ItemFactory.Enumerable.Parquet<CountryRegionRow>(
      "regions", SysIO.Path.Combine(_root, "bind-regions.parquet"));
    var output = ParquetItem("bound_out", "bind-out.parquet");
    const string sql = "SELECT * FROM a JOIN b USING (Country)";

    string? IdentityWith(string eventsRelation, string regionsRelation) =>
      IdentityOf(new DuckDbTransformStep<EventRow>(
        "bind", sql,
        new[]
        {
          DuckDbInputRelation.From(events, eventsRelation),
          DuckDbInputRelation.From(regions, regionsRelation),
        },
        output, new FixedVersionEngine("v1")));

    Assert.That(IdentityWith("a", "b"), Is.Not.EqualTo(IdentityWith("b", "a")),
      "Rebinding the same items to different relation names changes what the same "
      + "SQL reads — the binding map is output-affecting wire-up data.");
  }

  [Test]
  public void Identity_IgnoresEngineTuning()
  {
    var frugal = new InProcessDuckDbEngine(new DuckDbEngineOptions
    {
      MemoryLimit = "512MB",
      Threads = 1,
    });
    var roomy = new InProcessDuckDbEngine(new DuckDbEngineOptions
    {
      MemoryLimit = "8GB",
      Threads = 8,
      MaxConcurrentTransforms = 4,
    });

    Assert.That(IdentityOf(MakeStep("s", engine: frugal)),
      Is.EqualTo(IdentityOf(MakeStep("s", engine: roomy))),
      "Memory limits, thread counts, and concurrency are operational tuning — they "
      + "can't change output values, so re-tuning a host must never bust caches.");
  }

  // ── End-to-end: hit, each invalidation axis, downstream cascade ────────

  [Test]
  public async Task SecondRun_UnchangedSqlAndInputs_IsCacheHit()
  {
    await SeedEventsAsync();
    const string sql = "SELECT * FROM events ORDER BY Country, Id";

    var first = await RunFlowAsync(sql);
    Assert.That(first.IsSuccess, Is.True, Describe(first));
    Assert.That(StepOf(first, "sort_events").Reason, Is.Null,
      "First run is a cold miss — the transform really executes.");

    var second = await RunFlowAsync(sql);
    Assert.That(second.IsSuccess, Is.True, Describe(second));
    Assert.That(StepOf(second, "sort_events").Reason, Is.EqualTo("cached"),
      "Unchanged SQL + unchanged inputs + existing output → the step skips.");
  }

  [Test]
  public async Task EditedSqlText_InvalidatesTheCachedResult()
  {
    await SeedEventsAsync();

    await RunFlowAsync("SELECT * FROM events ORDER BY Country, Id");
    var rerun = await RunFlowAsync("SELECT * FROM events ORDER BY Id");

    Assert.That(rerun.IsSuccess, Is.True, Describe(rerun));
    Assert.That(StepOf(rerun, "sort_events").Reason, Is.Null,
      "An edited query must re-execute — serving the previous output would be "
      + "exactly the silent-stale failure the identity exists to kill.");
  }

  [Test]
  public async Task ChangedTransformOptions_InvalidateTheCachedResult()
  {
    await SeedEventsAsync();
    const string sql = "SELECT * FROM events ORDER BY Country, Id";

    await RunFlowAsync(sql);
    var rerun = await RunFlowAsync(sql, new DuckDbTransformOptions
    {
      Compression = DuckDbParquetCompression.Zstd,
    });

    Assert.That(rerun.IsSuccess, Is.True, Describe(rerun));
    Assert.That(StepOf(rerun, "sort_events").Reason, Is.Null,
      "Changing an output-affecting option must rewrite the output file.");
  }

  [Test]
  public async Task EngineVersionBump_InvalidatesTheCachedResult()
  {
    await SeedEventsAsync();
    const string sql = "SELECT * FROM events ORDER BY Country, Id";
    var realEngine = new InProcessDuckDbEngine();

    await RunFlowAsync(sql, engine: new VersionSpoofingEngine(realEngine, "v1.5.3"));
    var sameVersion = await RunFlowAsync(
      sql, engine: new VersionSpoofingEngine(realEngine, "v1.5.3"));
    Assert.That(StepOf(sameVersion, "sort_events").Reason, Is.EqualTo("cached"),
      "Control: an unchanged engine version stays a hit.");

    var bumped = await RunFlowAsync(
      sql, engine: new VersionSpoofingEngine(realEngine, "v1.6.0"));
    Assert.That(bumped.IsSuccess, Is.True, Describe(bumped));
    Assert.That(StepOf(bumped, "sort_events").Reason, Is.Null,
      "A different engine version must re-execute — cached output from another "
      + "engine version is not the output this engine would produce.");
  }

  [Test]
  public async Task DownstreamOfEngineStep_FollowsExistingCascadeRules()
  {
    await SeedEventsAsync();
    const string sql = "SELECT * FROM events ORDER BY Country, Id";

    var first = await RunFlowAsync(sql, withDownstream: true);
    Assert.That(first.IsSuccess, Is.True, Describe(first));
    Assert.That(_downstreamInvoked, Is.True, "Cold run executes the whole chain.");

    _downstreamInvoked = false;
    var second = await RunFlowAsync(sql, withDownstream: true);
    Assert.Multiple(() =>
    {
      Assert.That(StepOf(second, "sort_events").Reason, Is.EqualTo("cached"));
      Assert.That(StepOf(second, "downstream").Reason, Is.EqualTo("cached"),
        "A fresh engine parent leaves its CLR child cacheable — the engine step "
        + "participates in the cascade like any other step.");
      Assert.That(_downstreamInvoked, Is.False);
    });

    _downstreamInvoked = false;
    var edited = await RunFlowAsync(
      "SELECT * FROM events ORDER BY Id", withDownstream: true);
    Assert.Multiple(() =>
    {
      Assert.That(StepOf(edited, "sort_events").Reason, Is.Null,
        "The SQL edit invalidates the engine step…");
      Assert.That(StepOf(edited, "downstream").Reason, Is.Null,
        "…and the existing cascade rule carries the invalidation to its consumer.");
      Assert.That(_downstreamInvoked, Is.True);
    });
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private static string? IdentityOf(IStepNode step) => step.DeclaredCacheIdentity;

  private IItem<IEnumerable<EventRow>> ParquetItem(string label, string fileName) =>
    ItemFactory.Enumerable.Parquet<EventRow>(label, SysIO.Path.Combine(_root, fileName));

  private DuckDbTransformStep<EventRow> MakeStep(
    string label,
    string sql = "SELECT * FROM events",
    IDuckDbEngine? engine = null,
    DuckDbTransformOptions? options = null
  ) => new(
    label: label,
    sql: sql,
    inputs: new[] { DuckDbInputRelation.From(ParquetItem("events", "events.parquet")) },
    output: ParquetItem("sorted_events", "sorted.parquet"),
    engine: engine ?? new FixedVersionEngine("v-fixed"),
    options: options
  );

  private async Task SeedEventsAsync()
  {
    var events = ParquetItem("events", "events.parquet");
    var rows = new[]
    {
      new EventRow { Id = 2, Country = "NZ", OccurredAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), Value = 20.0 },
      new EventRow { Id = 1, Country = "AU", OccurredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Value = 10.0 },
    };
    var outcome = await events.Save(rows).Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      $"Seeding events failed: {outcome}");
  }

  private async Task<FlowResult> RunFlowAsync(
    string sql,
    DuckDbTransformOptions? options = null,
    IDuckDbEngine? engine = null,
    bool withDownstream = false
  )
  {
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    if (engine is not null)
    {
      // Registered ahead of UseDuckDb so its TryAddSingleton defers.
      services.AddSingleton(engine);
    }
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new CacheCatalog(_root));
      b.UseDuckDb();
      b.RegisterFlow<CacheCatalog, IDuckDbEngine>("duckdb-cache-e2e", (catalog, eng) =>
        FlowBuilder.CreateFlow("duckdb-cache-e2e", fb =>
        {
          fb.AddDuckDbTransform(
            "sort_events", catalog.Events, catalog.Sorted, sql, eng, options);
          if (withDownstream) fb.Add(BuildDownstreamStep(catalog));
        }));
      b.UseCacheStorage(_ =>
        Item.Of<CacheManifest>("flowthru.cache")
          .Json()
          .AtPath(_cachePath)
          .Build());
    });

    using var sp = services.BuildServiceProvider();
    return await sp.GetRequiredService<IFlowthruService>().RunAsync();
  }

  private Step<IEnumerable<EventRow>, IEnumerable<EventRow>> BuildDownstreamStep(
    CacheCatalog catalog
  ) => new(
    label: "downstream",
    transform: rows =>
    {
      _downstreamInvoked = true;
      return FlowIO.Pure(rows);
    },
    inputs: new IItem[] { catalog.Sorted },
    outputs: new IItem[] { catalog.Final },
    loadInputs: () => catalog.Sorted.Load(),
    saveOutputs: rows => catalog.Final.Save(rows),
    codeVersion: "downstream-v1"
  );

  private static StepResult.Succeeded StepOf(FlowResult result, string label) =>
    result.StepResults.OfType<StepResult.Succeeded>().Single(s => s.StepLabel == label);

  private static string Describe(FlowResult result) =>
    string.Join("; ", result.StepResults.Select(r => r switch
    {
      StepResult.Failed f => $"{f.StepLabel}: FAILED {f.Error.Message}",
      StepResult.Skipped s => $"{s.StepLabel}: skipped ({s.Reason})",
      StepResult.Succeeded ok => $"{ok.StepLabel}: ok ({ok.Reason ?? "ran"})",
      _ => $"{r.StepLabel}: {r.GetType().Name}",
    }));

  /// <summary>
  /// The parquet endpoints the e2e flows are wired between. Fresh
  /// instance per service provider; the files live under the per-test
  /// root so fingerprints persist across runs within a test.
  /// </summary>
  public sealed class CacheCatalog : CatalogAbstract
  {
    private readonly string _root;

    public CacheCatalog(string root) => _root = root;

    public IItem<IEnumerable<EventRow>> Events => CreateItem(() =>
      ItemFactory.Enumerable.Parquet<EventRow>(
        "events", SysIO.Path.Combine(_root, "events.parquet")));

    public IItem<IEnumerable<EventRow>> Sorted => CreateItem(() =>
      ItemFactory.Enumerable.Parquet<EventRow>(
        "sorted_events", SysIO.Path.Combine(_root, "sorted.parquet")));

    public IItem<IEnumerable<EventRow>> Final => CreateItem(() =>
      ItemFactory.Enumerable.Parquet<EventRow>(
        "final_events", SysIO.Path.Combine(_root, "final.parquet")));
  }

  /// <summary>
  /// Version-only fake: identity computation needs
  /// <see cref="IDuckDbEngine.EngineVersion"/> and nothing else, so the
  /// invalidation axes can be pinned without swapping DuckDB builds.
  /// </summary>
  private sealed class FixedVersionEngine : IDuckDbEngine
  {
    public FixedVersionEngine(string version) => EngineVersion = version;

    public int MaxConcurrency => 1;
    public string EngineVersion { get; }

    public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request) =>
      FlowIO.Pure(new DuckDbTransformResult(0, Array.Empty<(string, string)>()));
  }

  /// <summary>
  /// Delegates every transform to a real engine but reports an
  /// injectable version — the e2e engine-bump test runs real SQL under
  /// a spoofed version without depending on two DuckDB installs.
  /// </summary>
  private sealed class VersionSpoofingEngine : IDuckDbEngine
  {
    private readonly IDuckDbEngine _inner;

    public VersionSpoofingEngine(IDuckDbEngine inner, string version)
    {
      _inner = inner;
      EngineVersion = version;
    }

    public int MaxConcurrency => _inner.MaxConcurrency;
    public string EngineVersion { get; }

    public FlowIO<DuckDbTransformResult> ExecuteTransform(DuckDbTransformRequest request) =>
      _inner.ExecuteTransform(request);
  }
}
