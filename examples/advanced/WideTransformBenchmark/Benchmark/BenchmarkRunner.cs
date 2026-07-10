using System.Diagnostics;
using System.Globalization;
using Flowthru.Data.Catalog;
using Flowthru.Flow;
using Flowthru.Prelude;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using WideTransformBenchmark.Data;
using WideTransformBenchmark.Data._01_Raw.Schemas;
using WideTransformBenchmark.Data._02_Intermediate.Schemas;
using WideTransformBenchmark.Flows.EagerOptimize;
using WideTransformBenchmark.Flows.EngineOptimize;

namespace WideTransformBenchmark.Benchmark;

/// <summary>
/// The measurement harness — the pre-pipeline "staging step" of this example,
/// in the FlowthruCoverage sense, except instead of shell scripts the staging
/// work is <em>running the benchmark Flows</em>: fabricate the datasets, run
/// the optimize pass through both transform paths per size with a stopwatch
/// and an allocation meter around each flow execution, verify the two paths
/// agree, and write the measurement rows to the Raw CSV the Analyze Flow
/// ingests.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache correctness — why every measured run genuinely executes.</b>
/// Flowthru's step caching short-circuits steps whose input fingerprints are
/// unchanged, which would turn a second "measurement" into a file-existence
/// check. Three deliberate choices keep the measurements honest:
/// </para>
/// <list type="number">
///   <item>Nothing in this project registers <c>UseCacheStorage</c>, and the
///         flows here are built fresh and run via <c>BuiltFlow.RunAsync()</c>
///         directly — with no cache manifest there is no cache plan, and the
///         scheduler runs every step, every time.</item>
///   <item>Each (size × path) run owns its output file, and
///         <see cref="MeasureAsync"/> deletes that file before starting the
///         stopwatch — even a hypothetical output-existence shortcut would
///         find nothing to serve.</item>
///   <item>The run-twice check: re-running the example re-measures from
///         scratch and produces fresh (never identical-by-construction)
///         timings; see the README's cache-correctness note.</item>
/// </list>
/// </remarks>
public static class BenchmarkRunner
{
  /// <summary>Env knob for the fabricated dataset sizes, e.g. <c>FLOWTHRU_WTB_SIZES=100000,1000000,5000000</c>.</summary>
  public const string SizesEnvVar = "FLOWTHRU_WTB_SIZES";

  /// <summary>
  /// Default sizes — deliberately small enough that fabricate + measure +
  /// analyze completes in seconds, so the example is runnable on every clone.
  /// The env knob scales it up for the real reproduction.
  /// </summary>
  public static readonly int[] DefaultSizes = [10_000, 40_000, 160_000];

  /// <summary>Sizes from <see cref="SizesEnvVar"/> (comma-separated), or the defaults.</summary>
  public static int[] ResolveSizes()
  {
    var raw = Environment.GetEnvironmentVariable(SizesEnvVar);
    if (string.IsNullOrWhiteSpace(raw))
    {
      return DefaultSizes;
    }

    var parsed = raw
      .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
      .Select(token =>
        int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size)
        && size > 0
          ? size
          : throw new InvalidOperationException(
              $"{SizesEnvVar} must be a comma-separated list of positive integers; got '{raw}'."))
      .Distinct()
      .OrderBy(size => size)
      .ToArray();

    return parsed.Length > 0 ? parsed : DefaultSizes;
  }

  // ── Staging (fabricate-if-missing, measure-if-missing) ──────────────────

  /// <summary>
  /// Idempotent staging for hosted runs (the CLI verbs and the example
  /// integration harness): fabricate any missing dataset, and if the
  /// measurement CSV is absent — or was measured for a different size list —
  /// run the measured benchmark and write it. A bespoke <c>dotnet run</c>
  /// always re-measures instead (see <c>Program.cs</c>).
  /// </summary>
  public static async Task EnsureStagedAsync(string dataPath, int[] sizes)
  {
    EnsureDirectories(dataPath);
    await FabricateMissingAsync(dataPath, sizes);

    var catalog = new Catalog(dataPath);
    var staged = await TryLoadMeasurementsAsync(catalog);
    var stagedSizes = staged?.Select(m => m.InputRows).Distinct().OrderBy(s => s).ToArray();

    if (stagedSizes is not null && stagedSizes.SequenceEqual(sizes))
    {
      return;
    }

    var measurements = await MeasureAsync(dataPath, sizes);
    await SaveMeasurementsAsync(catalog, measurements);
  }

  /// <summary>Fabricate the seeded dataset for every size whose Parquet file is missing.</summary>
  public static async Task FabricateMissingAsync(string dataPath, int[] sizes)
  {
    EnsureDirectories(dataPath);
    foreach (var size in sizes)
    {
      if (File.Exists(SizedBenchmarkCatalog.RawReadingsPath(dataPath, size)))
      {
        continue;
      }

      Console.WriteLine($"Fabricating readings_{size}.parquet ({size:N0} rows)...");
      var catalog = new SizedBenchmarkCatalog(dataPath, size);
      await RunIoOrThrow(
        catalog.RawReadings.Save(ReadingsGenerator.Generate(size)),
        $"fabricate readings_{size}.parquet");
    }
  }

  // ── Measurement ──────────────────────────────────────────────────────────

  /// <summary>
  /// Run the optimize pass through both paths at every size, measured. Also
  /// warms both paths up front (JIT, Parquet reader, engine startup) at the
  /// smallest size so the first measured run isn't charged one-time cost, and
  /// verifies after each pair that both paths produced equivalent output.
  /// </summary>
  public static async Task<List<BenchmarkMeasurement>> MeasureAsync(string dataPath, int[] sizes)
  {
    EnsureDirectories(dataPath);
    await FabricateMissingAsync(dataPath, sizes);

    var engine = new InProcessDuckDbEngine();

    // Warm up both paths at the smallest size, then discard the outputs so
    // the measured runs start from a clean slate.
    var warmupCatalog = new SizedBenchmarkCatalog(dataPath, sizes.Min());
    Console.WriteLine($"Warming up both transform paths at {warmupCatalog.RowCount:N0} rows...");
    DeleteOutputs(dataPath, warmupCatalog.RowCount);
    await RunFlowOrThrow(EagerOptimizeFlow.Create(warmupCatalog), "warmup eager");
    await RunFlowOrThrow(EngineOptimizeFlow.Create(warmupCatalog, engine), "warmup engine");
    DeleteOutputs(dataPath, warmupCatalog.RowCount);

    var measurements = new List<BenchmarkMeasurement>();

    foreach (var size in sizes)
    {
      var catalog = new SizedBenchmarkCatalog(dataPath, size);

      // Delete both outputs before the pair of measured runs — a measured run
      // must produce its output, not find it (see the class remarks).
      DeleteOutputs(dataPath, size);

      var (eagerMs, eagerAllocated) = await MeasureFlowAsync(
        EagerOptimizeFlow.Create(catalog), $"eager {size:N0}");
      var (engineMs, engineAllocated) = await MeasureFlowAsync(
        EngineOptimizeFlow.Create(catalog, engine), $"engine {size:N0}");

      // Honesty check, outside the measurement windows: both paths must have
      // produced the same optimized dataset.
      var outputRows = await VerifyEquivalenceAsync(catalog);

      measurements.Add(new BenchmarkMeasurement
      {
        TransformPath = "Eager",
        InputRows = size,
        OutputRows = outputRows,
        ElapsedMs = eagerMs,
        AllocatedBytes = eagerAllocated,
      });
      measurements.Add(new BenchmarkMeasurement
      {
        TransformPath = "Engine",
        InputRows = size,
        OutputRows = outputRows,
        ElapsedMs = engineMs,
        AllocatedBytes = engineAllocated,
      });

      Console.WriteLine(
        $"  {size,10:N0} rows -> {outputRows,10:N0} | eager {eagerMs,6:N0} ms "
        + $"({FormatMb(eagerAllocated)} alloc) | engine {engineMs,6:N0} ms "
        + $"({FormatMb(engineAllocated)} alloc)");
    }

    return measurements;
  }

  /// <summary>
  /// One measured flow run: settle the GC, snapshot managed allocations, run
  /// the freshly built flow, snapshot again. The stopwatch wraps exactly the
  /// flow execution — fabrication, verification, and CSV writing all happen
  /// outside this window. (Same pattern as the extension's
  /// <c>DuckDbSortBenchmarkTests</c>.)
  /// </summary>
  private static async Task<(long ElapsedMs, long AllocatedBytes)> MeasureFlowAsync(
    BuiltFlow flow, string what)
  {
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
    var watch = Stopwatch.StartNew();
    var result = await flow.RunAsync();
    watch.Stop();
    var allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

    if (!result.IsSuccess)
    {
      var failure = result.FirstFailure;
      throw new InvalidOperationException(
        $"Measured run '{what}' failed: {failure?.Error?.ToString() ?? "unknown error"}");
    }

    return (watch.ElapsedMilliseconds, allocatedAfter - allocatedBefore);
  }

  /// <summary>
  /// Assert both paths produced equivalent output: same row count, and full
  /// row equality over the first 1,000 rows plus a stride of ~1,000 sample
  /// rows across the file — cheap even at env-knob sizes, and a mismatch
  /// anywhere in sort order shifts rows into the sample. Returns the count.
  /// </summary>
  private static async Task<int> VerifyEquivalenceAsync(SizedBenchmarkCatalog catalog)
  {
    var eager = (await LoadOrThrow(catalog.EagerOptimized, "eager output")).ToList();
    var engine = (await LoadOrThrow(catalog.EngineOptimized, "engine output")).ToList();

    if (eager.Count != engine.Count)
    {
      throw new InvalidOperationException(
        $"Path outputs disagree at {catalog.RowCount:N0} input rows: eager wrote "
        + $"{eager.Count:N0} rows, engine wrote {engine.Count:N0}.");
    }

    var stride = Math.Max(1, eager.Count / 1_000);
    for (var i = 0; i < eager.Count; i = i < 1_000 ? i + 1 : i + stride)
    {
      if (eager[i] != engine[i])
      {
        throw new InvalidOperationException(
          $"Path outputs disagree at {catalog.RowCount:N0} input rows, output row {i}: "
          + $"eager={eager[i]} engine={engine[i]}.");
      }
    }

    return eager.Count;
  }

  // ── Plumbing ─────────────────────────────────────────────────────────────

  private static void DeleteOutputs(string dataPath, int size)
  {
    File.Delete(SizedBenchmarkCatalog.EagerOptimizedPath(dataPath, size));
    File.Delete(SizedBenchmarkCatalog.EngineOptimizedPath(dataPath, size));
  }

  private static void EnsureDirectories(string dataPath)
  {
    Directory.CreateDirectory(Path.Combine(dataPath, "_01_Raw", "Datasets"));
    Directory.CreateDirectory(Path.Combine(dataPath, "_01_Raw", "Templates"));
    Directory.CreateDirectory(Path.Combine(dataPath, "_02_Intermediate", "Datasets"));
    Directory.CreateDirectory(Path.Combine(dataPath, "_04_Reporting", "Datasets"));
  }

  private static async Task<List<BenchmarkMeasurement>?> TryLoadMeasurementsAsync(Catalog catalog)
  {
    if (!File.Exists($"{catalog.DataPath}/_01_Raw/Datasets/benchmark_measurements.csv"))
    {
      return null;
    }

    var outcome = await catalog.Measurements.Load().Run();
    return outcome is EffResult<IEnumerable<BenchmarkMeasurement>>.Success success
      ? success.Value.ToList()
      : null;
  }

  public static async Task SaveMeasurementsAsync(
    Catalog catalog, IEnumerable<BenchmarkMeasurement> measurements)
  {
    await RunIoOrThrow(catalog.Measurements.Save(measurements), "write benchmark_measurements.csv");
  }

  public static async Task RunFlowOrThrow(BuiltFlow flow, string what)
  {
    var result = await flow.RunAsync();
    if (!result.IsSuccess)
    {
      var failure = result.FirstFailure;
      throw new InvalidOperationException(
        $"{what} failed: {failure?.Error?.ToString() ?? "unknown error"}");
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

  private static async Task<IEnumerable<OptimizedReadingRow>> LoadOrThrow(
    IItem<IEnumerable<OptimizedReadingRow>> item, string what)
  {
    var outcome = await item.Load().Run();
    if (outcome is EffResult<IEnumerable<OptimizedReadingRow>>.Success success)
    {
      return success.Value;
    }

    throw new InvalidOperationException($"Loading {what} ('{item.Label}') failed: {outcome}");
  }

  private static string FormatMb(long bytes) =>
    $"{bytes / (1024.0 * 1024.0):N1} MiB";
}
