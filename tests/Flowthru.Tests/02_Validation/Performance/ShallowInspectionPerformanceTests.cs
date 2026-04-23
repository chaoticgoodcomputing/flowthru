using System.Diagnostics;
using Flowthru.Core.Abstractions;
using Flowthru.Core.Data;
using Flowthru.Core.Data.Storage;
using Flowthru.Core.Data.Storage.Container;
using Flowthru.Core.Data.Storage.Format;
using Flowthru.Core.Data.Storage.Medium;

namespace Flowthru.Tests.Validation.Performance;

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

        TestContext.Out.WriteLine($"CSV shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms");
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

        TestContext.Out.WriteLine($"JSON shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms");
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

        TestContext.Out.WriteLine($"Parquet shallow inspection (50k rows, sample={SampleSize}): {sw.ElapsedMilliseconds}ms");
    }

    // ── Data fabrication helpers ─────────────────────────────────────────────

    private static IEnumerable<PerfRow> GenerateRows(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new PerfRow { Id = i, Name = $"Row-{i}", Score = i * 0.001 };
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
