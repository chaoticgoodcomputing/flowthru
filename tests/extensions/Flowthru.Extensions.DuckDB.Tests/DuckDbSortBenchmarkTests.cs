using System.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Extensions.DuckDB.Tests.Fixtures;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb.Internal;
using SysIO = System.IO;

namespace Flowthru.Extensions.DuckDB.Tests;

/// <summary>
/// The demo the transform exists for: a composite-key sort of a
/// multi-million-row Parquet file, engine-delegated versus the
/// equivalent LINQ <c>OrderBy</c> step. Generates its own data, runs
/// both flows over the same input, verifies they agree, and reports
/// wall-clock plus CLR-allocation numbers for each path.
/// </summary>
/// <remarks>
/// <para>
/// Excluded from default runs (<c>[Explicit]</c>); run on demand with
/// <c>dotnet test --filter "TestCategory=Benchmark"</c>. Row count
/// defaults to 5,000,000 and is overridable via
/// <c>FLOWTHRU_DUCKDB_BENCH_ROWS</c>.
/// </para>
/// <para>
/// The bounded-memory claim is asserted on <em>managed allocations</em>:
/// the DuckDB path must allocate a row-count-independent number of CLR
/// bytes during the flow run (the engine's native memory is bounded
/// separately by its own <c>MemoryLimit</c>). The LINQ path's
/// allocations — one object per row plus sort machinery — are recorded
/// for contrast, not asserted.
/// </para>
/// </remarks>
[TestFixture]
[Explicit("Benchmark — multi-million-row data generation; run via --filter TestCategory=Benchmark")]
[Category("Benchmark")]
[Category("DuckDB")]
public class DuckDbSortBenchmarkTests
{
  /// <summary>CLR-allocation ceiling for the engine-delegated path — row-count independent.</summary>
  private const long DuckDbManagedAllocationCeilingBytes = 64L * 1024 * 1024;

  private static readonly string[] Countries =
    ["AU", "NZ", "US", "GB", "DE", "FR", "JP", "BR", "IN", "ZA", "CA", "MX", "SE", "PL", "KR"];

  private string _root = null!;

  [SetUp]
  public void SetUp()
  {
    _root = SysIO.Path.Combine(
      SysIO.Path.GetTempPath(), $"flowthru-duckdb-bench-{Guid.NewGuid():N}");
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
  public async Task CompositeKeySort_EngineDelegated_VersusLinqOrderBy()
  {
    var rowCount = int.TryParse(
      Environment.GetEnvironmentVariable("FLOWTHRU_DUCKDB_BENCH_ROWS"), out var parsed)
      ? parsed
      : 5_000_000;

    // ── Seed ──────────────────────────────────────────────────────────────
    var inputPath = SysIO.Path.Combine(_root, "events.parquet");
    var input = ItemFactory.Enumerable.Parquet<EventRow>("events", inputPath);
    var seedWatch = Stopwatch.StartNew();
    await SaveOrFail(input, GenerateRows(rowCount));
    seedWatch.Stop();

    var linqOutput = ItemFactory.Enumerable.Parquet<EventRow>(
      "linq_sorted", SysIO.Path.Combine(_root, "linq_sorted.parquet"));
    var duckOutput = ItemFactory.Enumerable.Parquet<EventRow>(
      "duckdb_sorted", SysIO.Path.Combine(_root, "duckdb_sorted.parquet"));

    // ── LINQ OrderBy step (the row path) ──────────────────────────────────
    var linqFlow = FlowBuilder.CreateFlow("bench-linq", f =>
      f.AddStep<IEnumerable<EventRow>, IEnumerable<EventRow>>(
        label: "linq_sort",
        transform: rows => rows
          .OrderBy(r => r.Country, StringComparer.Ordinal)
          .ThenBy(r => r.Id),
        inputs: input,
        outputs: linqOutput
      ));
    var (linqElapsed, linqAllocated) = await MeasureAsync(linqFlow);

    // ── DuckDB transform (the engine path) ────────────────────────────────
    var duckFlow = FlowBuilder.CreateFlow("bench-duckdb", f => f.AddDuckDbTransform(
      label: "duckdb_sort",
      input: input,
      output: duckOutput,
      sql: "SELECT * FROM events ORDER BY Country, Id",
      engine: new InProcessDuckDbEngine()
    ));
    var (duckElapsed, duckAllocated) = await MeasureAsync(duckFlow);

    // ── Report ────────────────────────────────────────────────────────────
    var report = $"""

      DuckDB composite-key sort benchmark ({rowCount:N0} rows, {Countries.Length} countries)
        seed (generate + Parquet write) : {seedWatch.Elapsed.TotalSeconds,8:F2} s
        LINQ OrderBy step               : {linqElapsed.TotalSeconds,8:F2} s   {FormatBytes(linqAllocated)} managed allocations
        DuckDB engine transform         : {duckElapsed.TotalSeconds,8:F2} s   {FormatBytes(duckAllocated)} managed allocations
        speedup (LINQ / DuckDB)         : {linqElapsed.TotalSeconds / duckElapsed.TotalSeconds,8:F1} x
        process peak working set        : {FormatBytes(Process.GetCurrentProcess().PeakWorkingSet64)}
      """;
    TestContext.Out.WriteLine(report);

    // ── Assertions ────────────────────────────────────────────────────────
    Assert.That(duckAllocated, Is.LessThan(DuckDbManagedAllocationCeilingBytes),
      "The engine-delegated sort must allocate a row-count-independent number of CLR "
      + $"bytes; {FormatBytes(duckAllocated)} exceeds the {FormatBytes(DuckDbManagedAllocationCeilingBytes)} "
      + "ceiling, which means rows (or something proportional to them) entered the CLR.");

    // Both paths must agree: same count, same composite-key order.
    var linqKeys = await LoadKeys(linqOutput);
    var duckKeys = await LoadKeys(duckOutput);
    Assert.That(duckKeys, Has.Count.EqualTo(rowCount));
    Assert.That(duckKeys, Is.EqualTo(linqKeys),
      "The engine sort and the LINQ sort must produce the same composite-key sequence.");
  }

  // ── Harness ─────────────────────────────────────────────────────────────

  private static IReadOnlyList<EventRow> GenerateRows(int count)
  {
    var random = new Random(20260708);
    var baseline = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var rows = new List<EventRow>(count);
    for (var i = 0; i < count; i++)
    {
      rows.Add(new EventRow
      {
        Id = random.NextInt64(0, long.MaxValue),
        Country = Countries[random.Next(Countries.Length)],
        OccurredAt = baseline.AddSeconds(random.Next(0, 365 * 24 * 3600)),
        Value = random.NextDouble() * 1000,
      });
    }
    return rows;
  }

  private static async Task<(TimeSpan Elapsed, long AllocatedBytes)> MeasureAsync(BuiltFlow flow)
  {
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
    var watch = Stopwatch.StartNew();
    var result = await flow.RunAsync();
    watch.Stop();
    var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

    Assert.That(result.IsSuccess, Is.True,
      string.Join("; ", result.StepResults.Select(r => r.ToString())));
    return (watch.Elapsed, allocatedAfter - allocatedBefore);
  }

  private static async Task SaveOrFail(
    IItem<IEnumerable<EventRow>> item, IReadOnlyList<EventRow> rows
  )
  {
    var outcome = await item.Save(rows).Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      $"Seeding failed: {outcome}");
  }

  private static async Task<List<(string Country, long Id)>> LoadKeys(
    IItem<IEnumerable<EventRow>> item
  )
  {
    var outcome = await item.Load().Run();
    Assert.That(outcome, Is.InstanceOf<EffResult<IEnumerable<EventRow>>.Success>(),
      $"Loading '{item.Label}' failed: {outcome}");
    return ((EffResult<IEnumerable<EventRow>>.Success)outcome).Value
      .Select(r => (r.Country, r.Id))
      .ToList();
  }

  private static string FormatBytes(long bytes) =>
    bytes switch
    {
      >= 1L << 30 => $"{bytes / (double)(1L << 30):F2} GiB",
      >= 1L << 20 => $"{bytes / (double)(1L << 20):F2} MiB",
      >= 1L << 10 => $"{bytes / (double)(1L << 10):F2} KiB",
      _ => $"{bytes} B",
    };
}
