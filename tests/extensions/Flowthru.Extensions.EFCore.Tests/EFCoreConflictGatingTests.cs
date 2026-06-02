using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Extensions.EFCore.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Hosting;
using Flowthru.Prelude;
using Flowthru.Step;
using Flowthru.Validation.Runtime;
using Flowthru.Validation.Runtime.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Flowthru.Extensions.EFCore.Tests;

/// <summary>
/// #102 acceptance: EF Core catalog items declare their database as a
/// conflict resource, and <see cref="EFCoreDatabaseProfileContributor"/>
/// resolves it to the provider's read/write capacity. Concurrent writes
/// to one SQLite database serialize (no "database is locked"); concurrent
/// reads parallelize; an unrecognised dependency is left unbounded.
/// (ADR-0019.)
/// </summary>
[TestFixture]
[Category("EFCore")]
public class EFCoreConflictGatingTests
{
  private static readonly IServiceProfileProvider Gated =
    new CompositeServiceProfileProvider(new IServiceProfileContributor[]
    {
      new EFCoreDatabaseProfileContributor(),
    });

  private static readonly IServiceProfileProvider Ungated =
    new CompositeServiceProfileProvider(Array.Empty<IServiceProfileContributor>());

  // ── Acceptance: write serialization ────────────────────────────────────

  [Test]
  public async Task ConcurrentWritesToOneSqliteDatabase_Serialize()
  {
    var maxConcurrent = await RunTwoWritesAsync(Gated);
    Assert.That(maxConcurrent, Is.EqualTo(1),
      "Two steps writing to one single-writer SQLite database must serialize at Parallelism=4 — "
      + "the database's write capacity is 1."
    );
  }

  [Test]
  public async Task ConcurrentWritesToOneSqliteDatabase_WithoutContributor_RunConcurrently()
  {
    // Control: drop the contributor and the same two writes overlap.
    // Proves the contributor is what enforces serialization (not the DAG
    // shape) and that the harness genuinely observes overlap, so the
    // serialized assertion above is meaningful.
    var maxConcurrent = await RunTwoWritesAsync(Ungated);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "Without the EFCore contributor the database resolves to unbounded capacity, so the two "
      + "writes co-run — confirming gating, not DAG precedence, is what serializes them."
    );
  }

  // ── Acceptance: read parallelism (read/write asymmetry) ─────────────────

  [Test]
  public async Task ConcurrentReadsFromOneSqliteDatabase_RunConcurrently()
  {
    // Same database, same contributor — but reads are unbounded, so two
    // steps reading it co-run even while writes to it would serialize.
    var maxConcurrent = await RunTwoReadsAsync(Gated);
    Assert.That(maxConcurrent, Is.EqualTo(2),
      "Concurrent reads of one SQLite database must parallelize — read capacity is unbounded, "
      + "distinct from the single-writer write key."
    );
  }

  // ── Contributor translation ─────────────────────────────────────────────

  [Test]
  public void Contributor_MapsEFCoreDependency_ToDeclaredCapacities()
  {
    var dep = new ServiceDependency.External(
      new EFCoreDatabaseDependency("sqlite|/tmp/x.db/main", "/tmp/x.db/main", WriteCapacity: 1, ReadCapacity: int.MaxValue));

    var profile = new EFCoreDatabaseProfileContributor().Contribute(dep);

    Assert.That(profile, Is.Not.Null);
    Assert.That(profile!.Capacity, Is.EqualTo(1), "Write capacity flows from the dependency.");
    Assert.That(profile.ReadCapacity, Is.EqualTo(int.MaxValue), "Read capacity flows from the dependency.");
  }

  [Test]
  public void Contributor_StaysSilent_OnUnrelatedDependency()
  {
    Assert.That(
      new EFCoreDatabaseProfileContributor().Contribute(ServiceDependency.Of<IDisposable>()),
      Is.Null,
      "The contributor speaks only for EF Core database dependencies; null lets the composite fall through."
    );
  }

  // ── Item declares the dependency ────────────────────────────────────────

  [Test]
  public void SqliteItem_DeclaresDatabaseDependency_WithSingleWriterCapacity()
  {
    var (factory, dbPath) = TestDbContextFactoryBuilder.Build();
    try
    {
      var item = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("items", factory);

      var efcoreDep = item.ServiceDependencies
        .OfType<ServiceDependency.External>()
        .Select(e => e.Cause)
        .OfType<EFCoreDatabaseDependency>()
        .SingleOrDefault();

      Assert.That(efcoreDep, Is.Not.Null,
        "A SQLite-backed EFCore item must declare its database as a conflict resource.");
      Assert.That(efcoreDep!.WriteCapacity, Is.EqualTo(1),
        "SQLite is single-writer — the declared write capacity is 1.");
      Assert.That(efcoreDep.ReadCapacity, Is.EqualTo(int.MaxValue),
        "SQLite allows many readers — read capacity is unbounded.");
      Assert.That(efcoreDep.Category, Is.EqualTo("efcore"));
    }
    finally
    {
      TryDelete(dbPath);
    }
  }

  // ── UseEFCore registers the contributor ─────────────────────────────────

  [Test]
  public void UseEFCore_RegistersProfileContributor()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.UseEFCore();
    });
    using var sp = services.BuildServiceProvider();

    var contributors = sp.GetServices<IServiceProfileContributor>().ToArray();
    Assert.That(
      contributors.Any(c => c is EFCoreDatabaseProfileContributor),
      Is.True,
      "UseEFCore() must register the EFCore profile contributor so the scheduler can gate database writes."
    );
  }

  [Test]
  public void UseEFCore_IsIdempotent()
  {
    var services = new ServiceCollection();
    services.AddFlowthru(b =>
    {
      b.RegisterCatalog(_ => new EmptyCatalog());
      b.RegisterFlow("noop", () => FlowBuilder.CreateFlow("noop", _ => { }));
      b.UseEFCore();
      b.UseEFCore();
    });
    using var sp = services.BuildServiceProvider();

    var count = sp.GetServices<IServiceProfileContributor>()
      .Count(c => c is EFCoreDatabaseProfileContributor);
    Assert.That(count, Is.EqualTo(1),
      "Repeated UseEFCore() calls must not stack duplicate contributors (TryAddEnumerable semantics).");
  }

  // ── Harness ──────────────────────────────────────────────────────────────

  private static async Task<int> RunTwoWritesAsync(IServiceProfileProvider provider)
  {
    var (factory, dbPath) = TestDbContextFactoryBuilder.Build();
    try
    {
      var root = ItemFactory.Singleton.Memory<int>($"efw-root-{Guid.NewGuid():N}");
      await root.Save(0).Run();

      var (recordEntry, recordExit, max) = MakeConcurrencyMeter();
      Func<TestDbContext, IEnumerable<TestEntity>, CancellationToken, Task> recordingSave =
        async (_, _, ct) =>
        {
          recordEntry();
          await Task.Delay(60, ct).ConfigureAwait(false); // window for overlap to surface
          recordExit();
        };

      // Two output items backed by the SAME SQLite file → same write
      // conflict key. The recording save measures whether their writes
      // overlapped; it intentionally performs no DB write so the test
      // isolates scheduler gating from SQLite's own locking.
      var outA = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("efw-a", factory, saveFunc: recordingSave);
      var outB = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("efw-b", factory, saveFunc: recordingSave);

      Func<int, FlowIO<IEnumerable<TestEntity>>> transform = x =>
        FlowIO.Pure<IEnumerable<TestEntity>>(new[] { new TestEntity { Id = x, Name = "x", Value = 0 } });

      IStepNode Step(string label, IItem<IEnumerable<TestEntity>> output) =>
        new Step<int, IEnumerable<TestEntity>>(
          label, transform, new IItem[] { root }, new IItem[] { output },
          loadInputs: () => root.Load(), saveOutputs: v => output.Save(v));

      var flow = FlowBuilder.CreateFlow("efcore-writes", b =>
      {
        b.Add(Step("efw-step-a", outA));
        b.Add(Step("efw-step-b", outB));
      });

      var result = await new ParallelFlowScheduler(profiles: provider)
        .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

      Assert.That(result.IsSuccess, Is.True, "both write steps should succeed");
      return max();
    }
    finally
    {
      TryDelete(dbPath);
    }
  }

  private static async Task<int> RunTwoReadsAsync(IServiceProfileProvider provider)
  {
    var (factory, dbPath) = TestDbContextFactoryBuilder.Build();
    try
    {
      var outA = ItemFactory.Singleton.Memory<int>($"efr-a-{Guid.NewGuid():N}");
      var outB = ItemFactory.Singleton.Memory<int>($"efr-b-{Guid.NewGuid():N}");

      // Two input items backed by the same SQLite file → same read key.
      var inA = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("efr-in-a", factory, allowEmptyData: true);
      var inB = ItemFactory.Enumerable.EFCore<TestEntity, TestDbContext>("efr-in-b", factory, allowEmptyData: true);

      var (recordEntry, recordExit, max) = MakeConcurrencyMeter();
      Func<IEnumerable<TestEntity>, FlowIO<int>> transform = _ => FlowIO.LiftAsync(
        async ct =>
        {
          recordEntry();
          await Task.Delay(60, ct).ConfigureAwait(false);
          recordExit();
          return 0;
        },
        source: "efr:track");

      IStepNode Step(string label, IItem<IEnumerable<TestEntity>> input, IItem<int> output) =>
        new Step<IEnumerable<TestEntity>, int>(
          label, transform, new IItem[] { input }, new IItem[] { output },
          loadInputs: () => input.Load(), saveOutputs: v => output.Save(v));

      var flow = FlowBuilder.CreateFlow("efcore-reads", b =>
      {
        b.Add(Step("efr-step-a", inA, outA));
        b.Add(Step("efr-step-b", inB, outB));
      });

      var result = await new ParallelFlowScheduler(profiles: provider)
        .ExecuteAsync(flow, new ExecutionOptions { Parallelism = 4 });

      Assert.That(result.IsSuccess, Is.True, "both read steps should succeed");
      return max();
    }
    finally
    {
      TryDelete(dbPath);
    }
  }

  /// <summary>A peak-concurrency meter: count entries, record the high-water mark, count exits.</summary>
  private static (Action Entry, Action Exit, Func<int> Max) MakeConcurrencyMeter()
  {
    var running = 0;
    var max = 0;
    var gate = new object();
    void Entry()
    {
      var now = Interlocked.Increment(ref running);
      lock (gate) max = Math.Max(max, now);
    }
    void Exit() => Interlocked.Decrement(ref running);
    int Max() { lock (gate) return max; }
    return (Entry, Exit, Max);
  }

  private static void TryDelete(string path)
  {
    if (File.Exists(path))
    {
      try { File.Delete(path); }
      catch { /* best effort */ }
    }
  }

  private sealed class EmptyCatalog : CatalogAbstract { }
}
