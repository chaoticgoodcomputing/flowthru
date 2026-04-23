using System.Diagnostics;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;
using Parquet;

namespace Flowthru.Extensions.Parquet.Tests.Validation.Performance;

// A minimal schema that satisfies all three serializer marker interfaces,
// enabling use with CSV (ITextSerializable), JSON (IStructuredSerializable),
// and Parquet (IBinarySerializable) format serializers in a single test file.
// Uses required members (not a positional record) so CsvHelper can deserialize it.
public record PerfRow : IFlatSchema, ITextSerializable, IStructuredSerializable, IBinarySerializable
{
  [SerializedLabel("id")]
  public required int Id { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("score")]
  public required double Score { get; init; }
}

/// <summary>
/// Guardrail tests asserting that shallow inspection does NOT materialize the full dataset.
/// </summary>
/// <remarks>
/// <para>
/// These tests fabricate medium-large files (~50 000 rows) and assert that
/// <see cref="Flowthru.Core.Data.IItem.InspectShallow"/> completes within a wall-clock
/// budget that would be impossible to meet if the full dataset were deserialized.
/// </para>
/// <para>
/// Wall-clock budgets are intentionally generous (5 000 ms) to avoid flakiness on loaded
/// CI machines — the intent is to catch "accidentally reads the whole file" regressions,
/// not to measure absolute throughput.  A correct shallow read of 100 rows from a
/// 50 000-row file should complete in tens of milliseconds even on slow hardware.
/// </para>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("Performance")]
public class ShallowInspectionPerformanceTests
{
  private const int RowCount = 50_000;
  private const int SampleSize = 100;
  private const int BudgetMs = 5_000;

  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-perf-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
      Directory.Delete(_tempDir, recursive: true);
  }

  // ── CSV ──────────────────────────────────────────────────────────────────

  [Test]
  public async Task Csv_ShallowInspect_CompletesWithinBudget_On50kRows()
  {
    var filePath = Path.Combine(_tempDir, "data.csv");
    await WriteCsvFile(filePath, RowCount);

    var adapter = new ComposedStorageAdapter<IEnumerable<PerfRow>, PerfRow>(
      new FileStorageMedium(filePath),
      new CsvFormatSerializer<PerfRow>(),
      new EnumerableContainerAdapter<PerfRow>()
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"CSV shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"CSV shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── JSON ─────────────────────────────────────────────────────────────────

  [Test]
  public async Task Json_ShallowInspect_CompletesWithinBudget_On50kRows()
  {
    var filePath = Path.Combine(_tempDir, "data.json");
    await WriteJsonFile(filePath, RowCount);

    var adapter = new ComposedStorageAdapter<IEnumerable<PerfRow>, PerfRow>(
      new FileStorageMedium(filePath),
      new JsonFormatSerializer<PerfRow>(),
      new EnumerableContainerAdapter<PerfRow>()
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"JSON shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"JSON shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── Parquet ───────────────────────────────────────────────────────────────

  [Test]
  public async Task Parquet_ShallowInspect_CompletesWithinBudget_On50kRows()
  {
    var filePath = Path.Combine(_tempDir, "data.parquet");
    await WriteParquetFile(filePath, RowCount);

    var adapter = new ComposedStorageAdapter<IEnumerable<PerfRow>, PerfRow>(
      new FileStorageMedium(filePath),
      new ParquetFormatSerializer<PerfRow>(),
      new EnumerableContainerAdapter<PerfRow>()
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(BudgetMs),
      $"Parquet shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {BudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"Parquet shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── Data fabrication helpers ─────────────────────────────────────────────

  private static IEnumerable<PerfRow> GenerateRows(int count)
  {
    for (int i = 0; i < count; i++)
    {
      yield return new PerfRow
      {
        Id = i,
        Name = $"Row-{i}",
        Score = i * 0.001,
      };
    }
  }

  private static async Task WriteCsvFile(string path, int rowCount)
  {
    var format = new CsvFormatSerializer<PerfRow>();
    await using var stream = File.Create(path);
    await format.SerializeRows(stream, GenerateRows(rowCount).ToAsyncEnumerable());
  }

  private static async Task WriteJsonFile(string path, int rowCount)
  {
    var format = new JsonFormatSerializer<PerfRow>();
    await using var stream = File.Create(path);
    await format.SerializeRows(stream, GenerateRows(rowCount).ToAsyncEnumerable());
  }

  private static async Task WriteParquetFile(string path, int rowCount)
  {
    var format = new ParquetFormatSerializer<PerfRow>();
    await using var stream = File.Create(path);
    await format.SerializeRows(stream, GenerateRows(rowCount).ToAsyncEnumerable());
  }
}

/// <summary>
/// Guardrail tests for large Parquet datasets in the 1M–5M row range.
/// </summary>
/// <remarks>
/// <para>
/// These tests verify three properties under load:
/// </para>
/// <list type="number">
/// <item><b>Write produces multiple row groups.</b> A 3M-row file written with a 1M-row group
/// size must produce ≥ 2 row groups. Failure means the old "buffer everything" path is back.</item>
/// <item><b>Read is streaming / bounded.</b> Full read of a 3M-row file must complete within
/// a generous wall-clock budget that would be impossible if rows were fully materialized twice.</item>
/// <item><b>Shallow inspection is fast.</b> Sampling 100 rows from a 3M-row file must complete
/// within a tight budget — verifying that the per-row-group early-exit path is used.</item>
/// </list>
/// <para>
/// Row counts are deliberately lower than a real 1–10 GB dataset to keep CI runtimes
/// reasonable. The structural guarantees (multi-row-group, streaming read, early-exit) are
/// format-level properties that hold regardless of total row count.
/// </para>
/// </remarks>
[TestFixture]
[Category("Validation")]
[Category("Performance")]
public class ParquetLargeDatasetPerformanceTests
{
  private const int LargeRowCount = 3_000_000;
  private const int RowGroupSize = 1_000_000; // Expect 3 row groups (3 × 1M)
  private const int SampleSize = 100;
  private const int ShallowBudgetMs = 10_000; // Generous: any correct impl is <1s
  private const int FullReadBudgetMs = 60_000; // 60s — full 3M row read is allowed
  private const int WriteBudgetMs = 120_000; // 120s — write is the expensive path

  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), $"flowthru-parquet-large-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_tempDir))
      Directory.Delete(_tempDir, recursive: true);
  }

  // ── Write ─────────────────────────────────────────────────────────────────

  [Test]
  public async Task Parquet_Write_ProducesMultipleRowGroups_WhenDataExceedsRowGroupSize()
  {
    var filePath = Path.Combine(_tempDir, "large.parquet");

    var sw = Stopwatch.StartNew();
    await WriteLargeParquetFile(filePath, LargeRowCount, RowGroupSize);
    sw.Stop();

    // Open the file directly with Parquet.NET to inspect row group count.
    // (We don't expose reader internals through Flowthru's public API.)
    await using var stream = File.OpenRead(filePath);
    using var reader = await ParquetReader.CreateAsync(stream);
    int rowGroupCount = reader.RowGroupCount;

    Assert.That(
      rowGroupCount,
      Is.GreaterThanOrEqualTo(2),
      $"Expected ≥ 2 row groups for {LargeRowCount:N0} rows at size {RowGroupSize:N0}/group, got {rowGroupCount}."
    );

    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(WriteBudgetMs),
      $"Write took {sw.ElapsedMilliseconds}ms — expected < {WriteBudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"Write {LargeRowCount:N0} rows -> {rowGroupCount} row groups "
        + $"({new FileInfo(filePath).Length / 1024 / 1024} MB file) in {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── Shallow inspection ────────────────────────────────────────────────────

  [Test]
  public async Task Parquet_ShallowInspect_CompletesWithinBudget_On3MRows()
  {
    var filePath = Path.Combine(_tempDir, "large.parquet");
    await WriteLargeParquetFile(filePath, LargeRowCount, RowGroupSize);

    var adapter = new ComposedStorageAdapter<IEnumerable<PerfRow>, PerfRow>(
      new FileStorageMedium(filePath),
      new ParquetFormatSerializer<PerfRow>(),
      new EnumerableContainerAdapter<PerfRow>()
    );

    var sw = Stopwatch.StartNew();
    var result = await adapter.InspectShallow(SampleSize).Run(CancellationToken.None);
    sw.Stop();

    Assert.That(result.IsValid, Is.True, string.Join(", ", result.Errors.Select(e => e.Message)));
    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(ShallowBudgetMs),
      $"Parquet shallow inspection took {sw.ElapsedMilliseconds}ms — expected < {ShallowBudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"Parquet shallow inspection (3M rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── Full read ─────────────────────────────────────────────────────────────

  [Test]
  public async Task Parquet_FullRead_MaterializesAllRows_On3MRows()
  {
    var filePath = Path.Combine(_tempDir, "large.parquet");
    await WriteLargeParquetFile(filePath, LargeRowCount, RowGroupSize);

    var adapter = new ComposedStorageAdapter<IEnumerable<PerfRow>, PerfRow>(
      new FileStorageMedium(filePath),
      new ParquetFormatSerializer<PerfRow>(),
      new EnumerableContainerAdapter<PerfRow>()
    );

    var sw = Stopwatch.StartNew();
    var loaded = await adapter.Load().Run(CancellationToken.None);
    int totalRows = loaded.Count();
    sw.Stop();

    Assert.That(
      totalRows,
      Is.EqualTo(LargeRowCount),
      $"Expected {LargeRowCount:N0} rows, got {totalRows:N0}"
    );
    Assert.That(
      sw.ElapsedMilliseconds,
      Is.LessThan(FullReadBudgetMs),
      $"Full read took {sw.ElapsedMilliseconds}ms — expected < {FullReadBudgetMs}ms"
    );

    TestContext.Out.WriteLine(
      $"Parquet full read (3M rows): {totalRows:N0} rows in {sw.ElapsedMilliseconds}ms"
    );
  }

  // ── Data fabrication helper ───────────────────────────────────────────────

  private static IAsyncEnumerable<PerfRow> GenerateLargeRows(int count) =>
    Enumerable
      .Range(0, count)
      .Select(i => new PerfRow
      {
        Id = i,
        Name = $"Row-{i}",
        Score = i * 0.001,
      })
      .ToAsyncEnumerable();

  private static async Task WriteLargeParquetFile(string path, int rowCount, int rowGroupSize)
  {
    var options = new ParquetItemOptions<PerfRow> { RowGroupSize = rowGroupSize };
    var format = new ParquetFormatSerializer<PerfRow>(options);
    await using var stream = File.Create(path);
    await format.SerializeRows(stream, GenerateLargeRows(rowCount));
  }
}
