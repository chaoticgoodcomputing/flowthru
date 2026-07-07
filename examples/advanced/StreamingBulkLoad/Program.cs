using System.Diagnostics;
using System.Globalization;
using Flowthru.Flow;
using Flowthru.Prelude;
using Microsoft.EntityFrameworkCore;
using StreamingBulkLoad.Data;
using StreamingBulkLoad.Data._01_Raw.Schemas;
using StreamingBulkLoad.Flows.EagerIngest;
using StreamingBulkLoad.Flows.Reporting;
using StreamingBulkLoad.Flows.StreamingIngest;

namespace StreamingBulkLoad;

/// <summary>
/// Self-instrumenting entry point. Unlike the CLI-driven examples, this harness
/// wraps each ingest variant in a background RSS sampler so it can measure — in
/// this very process — the peak memory eager and streaming ingest each hold
/// while loading the same Parquet dataset into the same SQLite schema. It then
/// runs a pure-Flowthru Reporting Flow over the samples to emit the verdict.
/// </summary>
/// <remarks>
/// Sequence: generate (once) → warm up → measure Streaming → measure Eager →
/// write <c>memory_samples.csv</c> → run the Reporting Flow → print the numbers.
/// Streaming is measured first, from a clean low baseline, because the OS does
/// not hand working-set back after the eager path balloons it.
/// </remarks>
public static class Program
{
  private const int DefaultRows = 200_000;

  // One in this many generated rows is invalid (zero amount) so the filter
  // demonstrably drops the same count on both paths.
  private const int InvalidEveryNth = 50;

  public static async Task<int> Main(string[] args)
  {
    var rows = ResolveRowCount(args);
    var generateOnly = args.Contains("--generate");
    var dryRun = args.Contains("--dry-run");

    var cwd = Directory.GetCurrentDirectory();
    var dataDir = Path.Combine(cwd, "Data");
    var dbPath = Path.Combine(dataDir, "_02_Intermediate", "transactions.db");
    var parquetPath = Path.Combine(dataDir, "_01_Raw", "Datasets", "transactions.parquet");

    EnsureDirectories(dataDir);

    if (dryRun)
    {
      Console.WriteLine("StreamingBulkLoad --dry-run: EagerIngest, StreamingIngest, Reporting flows registered. No compute.");
      return 0;
    }

    var catalog = new Catalog(dataDir, dbPath);

    if (generateOnly || !File.Exists(parquetPath))
    {
      await GenerateDatasetAsync(catalog, rows);
      if (generateOnly)
      {
        return 0;
      }
    }

    EnsureDatabase(catalog);

    // Warm up the ingest code paths (JIT, Parquet reader, EF bulk) so the
    // first measured run isn't charged for one-time startup cost.
    Console.WriteLine("Warming up ingest paths...");
    ClearTable(catalog);
    await RunFlowOrThrow(StreamingIngestFlow.Create(catalog), "warmup streaming ingest");

    Console.WriteLine($"\nMeasuring ingest of {rows:N0} rows ({Catalog.WriteRowGroupSize:N0}-row Parquet groups, {Catalog.BulkBatchSize:N0}-row bulk batches)...\n");

    var streamingSample = await MeasureVariantAsync(
      "Streaming", catalog, () => StreamingIngestFlow.Create(catalog));
    PrintSample(streamingSample);

    var eagerSample = await MeasureVariantAsync(
      "Eager", catalog, () => EagerIngestFlow.Create(catalog));
    PrintSample(eagerSample);

    // Persist the measured facts as a Raw CSV, then let a pure Flowthru Flow
    // read them back and render the report — the example proving its own thesis.
    await RunIoOrThrow(
      catalog.MemorySamples.Save(new[] { eagerSample, streamingSample }),
      "write memory_samples.csv");

    await RunFlowOrThrow(ReportingFlow.Create(catalog), "reporting");

    PrintFinalSummary(eagerSample, streamingSample, dataDir);
    return 0;
  }

  // ── Measurement ────────────────────────────────────────────────────────

  private static async Task<MemorySample> MeasureVariantAsync(
    string variant,
    Catalog catalog,
    Func<BuiltFlow> buildFlow)
  {
    ClearTable(catalog);

    // Establish a clean managed baseline so the in-window peak reflects this
    // variant's own allocations.
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);
    GC.WaitForPendingFinalizers();
    GC.Collect(2, GCCollectionMode.Forced, blocking: true);

    using var sampler = new RssSampler();
    sampler.Start();
    var stopwatch = Stopwatch.StartNew();

    var result = await buildFlow().RunAsync();

    stopwatch.Stop();
    sampler.Stop();

    if (!result.IsSuccess)
    {
      var failure = result.FirstFailure;
      throw new InvalidOperationException(
        $"{variant} ingest failed: {failure?.Error?.ToString() ?? "unknown error"}");
    }

    var rowCount = CountRows(catalog);

    return new MemorySample
    {
      Variant = variant,
      RowCount = rowCount,
      PeakWorkingSetBytes = sampler.PeakWorkingSetBytes,
      PeakManagedBytes = sampler.PeakManagedBytes,
      DurationMs = stopwatch.ElapsedMilliseconds,
    };
  }

  /// <summary>
  /// Background sampler tracking the peak OS working set and managed heap over a
  /// measurement window. Polls every 25 ms — fast enough to catch a transient
  /// O(file) spike, cheap enough not to perturb the run.
  /// </summary>
  private sealed class RssSampler : IDisposable
  {
    private readonly Process _process = Process.GetCurrentProcess();
    private volatile bool _running;
    private Thread? _thread;

    public long PeakWorkingSetBytes { get; private set; }
    public long PeakManagedBytes { get; private set; }

    public void Start()
    {
      _process.Refresh();
      PeakWorkingSetBytes = _process.WorkingSet64;
      PeakManagedBytes = GC.GetTotalMemory(forceFullCollection: false);
      _running = true;
      _thread = new Thread(Loop) { IsBackground = true, Name = "rss-sampler" };
      _thread.Start();
    }

    private void Loop()
    {
      while (_running)
      {
        Observe();
        Thread.Sleep(25);
      }
    }

    private void Observe()
    {
      _process.Refresh();
      var workingSet = _process.WorkingSet64;
      if (workingSet > PeakWorkingSetBytes) PeakWorkingSetBytes = workingSet;
      var managed = GC.GetTotalMemory(forceFullCollection: false);
      if (managed > PeakManagedBytes) PeakManagedBytes = managed;
    }

    public void Stop()
    {
      _running = false;
      _thread?.Join();
      Observe();
    }

    public void Dispose()
    {
      _running = false;
      _process.Dispose();
    }
  }

  // ── Dataset generation ───────────────────────────────────────────────────

  private static async Task GenerateDatasetAsync(Catalog catalog, int rows)
  {
    Console.WriteLine($"Generating {rows:N0}-row multi-row-group Parquet dataset...");
    await RunIoOrThrow(catalog.RawTransactions.Save(GenerateRows(rows)), "generate transactions.parquet");
    Console.WriteLine("Dataset written to Data/_01_Raw/Datasets/transactions.parquet");
  }

  private static IEnumerable<TransactionRecord> GenerateRows(int count)
  {
    // Deterministic seed so re-runs produce the same dataset (stable report).
    var random = new Random(12345);
    var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Noisy source categories — mixed case and stray whitespace — so the
    // streaming .Map normalisation has real work to do.
    string[] categories =
    {
      " groceries ", "RENT", "utilities", " Travel", "dining ", "Salary", "  Fees",
    };

    for (var i = 0; i < count; i++)
    {
      var invalid = i % InvalidEveryNth == 0;
      yield return new TransactionRecord
      {
        Id = i,
        AccountId = 1_000 + (i % 5_000),
        AmountCents = invalid ? 0 : random.Next(-50_000, 500_000),
        Category = categories[i % categories.Length],
        TimestampUtc = baseTime.AddSeconds(i),
      };
    }
  }

  // ── SQLite helpers ───────────────────────────────────────────────────────

  private static void EnsureDatabase(Catalog catalog)
  {
    using var db = catalog.NewContext();
    db.Database.EnsureCreated();
  }

  private static void ClearTable(Catalog catalog)
  {
    using var db = catalog.NewContext();
    db.Transactions.ExecuteDelete();
  }

  private static int CountRows(Catalog catalog)
  {
    using var db = catalog.NewContext();
    return db.Transactions.Count();
  }

  // ── Plumbing ─────────────────────────────────────────────────────────────

  private static int ResolveRowCount(string[] args)
  {
    for (var i = 0; i < args.Length - 1; i++)
    {
      if (args[i] == "--rows" && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
      {
        return parsed;
      }
    }

    var env = Environment.GetEnvironmentVariable("STREAMINGBULKLOAD_ROWS");
    if (int.TryParse(env, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromEnv) && fromEnv > 0)
    {
      return fromEnv;
    }

    return DefaultRows;
  }

  private static void EnsureDirectories(string dataDir)
  {
    Directory.CreateDirectory(Path.Combine(dataDir, "_01_Raw", "Datasets"));
    Directory.CreateDirectory(Path.Combine(dataDir, "_01_Raw", "Templates"));
    Directory.CreateDirectory(Path.Combine(dataDir, "_02_Intermediate"));
    Directory.CreateDirectory(Path.Combine(dataDir, "_04_Reporting", "Datasets"));
  }

  private static async Task RunFlowOrThrow(BuiltFlow flow, string what)
  {
    var result = await flow.RunAsync();
    if (!result.IsSuccess)
    {
      var failure = result.FirstFailure;
      throw new InvalidOperationException($"{what} failed: {failure?.Error?.ToString() ?? "unknown error"}");
    }
  }

  private static async Task RunIoOrThrow(FlowIO<FlowUnit> io, string what)
  {
    var result = await io.Run();
    if (result is EffResult<FlowUnit>.Failure failure)
    {
      throw new InvalidOperationException($"{what} failed: {failure.Error}");
    }
  }

  // ── Console output ───────────────────────────────────────────────────────

  private static void PrintSample(MemorySample sample)
  {
    Console.WriteLine(
      $"  {sample.Variant,-9} | rows {sample.RowCount,10:N0} | peak managed {Mb(sample.PeakManagedBytes),8:N1} MB "
      + $"| peak working set {Mb(sample.PeakWorkingSetBytes),8:N1} MB | {sample.DurationMs,6:N0} ms");
  }

  private static void PrintFinalSummary(MemorySample eager, MemorySample streaming, string dataDir)
  {
    var ratio = eager.PeakManagedBytes > 0
      ? 100.0 * streaming.PeakManagedBytes / eager.PeakManagedBytes
      : 0.0;

    Console.WriteLine();
    Console.WriteLine("──────────────────────────────────────────────────────────────");
    Console.WriteLine($"  Eager peak managed:     {Mb(eager.PeakManagedBytes),8:N1} MB");
    Console.WriteLine($"  Streaming peak managed: {Mb(streaming.PeakManagedBytes),8:N1} MB");
    Console.WriteLine($"  Streaming held peak to {ratio:N1}% of eager (both loaded {eager.RowCount:N0} rows).");
    Console.WriteLine("──────────────────────────────────────────────────────────────");
    Console.WriteLine($"  Report: {Path.Combine(dataDir, "_04_Reporting", "Datasets", "memory_report.md")}");
    Console.WriteLine();
  }

  private static double Mb(long bytes) => bytes / (1024.0 * 1024.0);
}
