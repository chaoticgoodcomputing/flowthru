using Flowthru.Data.Catalog;
using WideTransformBenchmark.Data._01_Raw.Schemas;
using WideTransformBenchmark.Data._02_Intermediate.Schemas;

namespace WideTransformBenchmark.Data;

/// <summary>
/// The benchmark endpoints for one fabricated dataset size: the Raw readings
/// Parquet both paths consume, and one Intermediate Parquet output per
/// transform path. One instance exists per size in the run's size list,
/// closure-captured by the per-size flow factories — the shard-catalog
/// pattern from RetailDataSplitFlow, with dataset size as the shard key.
/// </summary>
/// <remarks>
/// Each (size × path) run writes to its own output file. Combined with the
/// harness deleting outputs before every measured run and no
/// <c>UseCacheStorage</c> registration anywhere in this project, this is what
/// guarantees a "measured run" can never be a cache hit serving a previous
/// run's output — see <c>Benchmark/BenchmarkRunner.cs</c>.
/// </remarks>
public class SizedBenchmarkCatalog : CatalogAbstract
{
  private readonly string _basePath;

  /// <summary>Fabricated rows in this shard's dataset (pre-dedup).</summary>
  public int RowCount { get; }

  public SizedBenchmarkCatalog(string basePath, int rowCount)
    : base($"BenchmarkCatalog_{rowCount}")
  {
    _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    RowCount = rowCount;
  }

  /// <summary>
  /// The fabricated multi-column readings dataset, written once per size by
  /// the generator (seeded, deterministic — same size, same file). Both
  /// transform paths read exactly this file.
  /// </summary>
  public IItem<IEnumerable<RawReadingRow>> RawReadings =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<RawReadingRow>(
        $"raw_readings_{RowCount}", RawReadingsPath(_basePath, RowCount)));

  /// <summary>The eager LINQ path's output for this size.</summary>
  public IItem<IEnumerable<OptimizedReadingRow>> EagerOptimized =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<OptimizedReadingRow>(
        $"optimized_eager_{RowCount}", EagerOptimizedPath(_basePath, RowCount)));

  /// <summary>The DuckDB engine path's output for this size.</summary>
  public IItem<IEnumerable<OptimizedReadingRow>> EngineOptimized =>
    CreateItem(() =>
      ItemFactory.Enumerable.Parquet<OptimizedReadingRow>(
        $"optimized_engine_{RowCount}", EngineOptimizedPath(_basePath, RowCount)));

  // Path builders are shared with the harness so "the file the item writes"
  // and "the file the harness deletes before a measured run" can never drift.

  public static string RawReadingsPath(string basePath, int rowCount) =>
    $"{basePath}/_01_Raw/Datasets/readings_{rowCount}.parquet";

  public static string EagerOptimizedPath(string basePath, int rowCount) =>
    $"{basePath}/_02_Intermediate/Datasets/optimized_eager_{rowCount}.parquet";

  public static string EngineOptimizedPath(string basePath, int rowCount) =>
    $"{basePath}/_02_Intermediate/Datasets/optimized_engine_{rowCount}.parquet";
}
