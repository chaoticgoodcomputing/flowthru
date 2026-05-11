using System.Diagnostics;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;
using Flowthru.Data.Storage.Parquet;
using Flowthru.Extensions.Parquet.Tests.Fixtures;
using Flowthru.Prelude;
using SysIO = System.IO;

namespace Flowthru.Extensions.Parquet.Tests.Performance;

/// <summary>
/// Guardrail asserting that a Parquet shallow inspection does NOT
/// materialise the full dataset. The wall-clock budget is intentionally
/// generous — the goal is to catch "accidentally reads the whole file"
/// regressions, not to measure absolute throughput.
/// </summary>
/// <remarks>
/// <para>
/// A 50 000-row Parquet file is generated with a small row-group size
/// so the file contains many groups. <see cref="IItem{T}.InspectShallow"/>
/// reads only enough rows to satisfy the sample size; correct streaming
/// behavior reads at most one row group, completing in tens of
/// milliseconds even on slow hardware.
/// </para>
/// <para>
/// If <see cref="ParquetFormatSerializer{TRow}.DeserializeRows"/> stops
/// streaming row-group-at-a-time (e.g. someone accidentally collapses
/// it back to a full <c>DeserializeAsync</c>), this test would blow
/// past the budget rather than silently regress on production loads.
/// </para>
/// </remarks>
[TestFixture]
[Category("Parquet")]
[Category("Performance")]
public class RowGroupStreamingGuardrailTests
{
  private const int RowCount = 50_000;
  private const int SampleSize = 100;
  private const int RowGroupSize = 1_000; // 50 row groups in the test file
  private const int BudgetMs = 5_000;

  private string _tempDir = null!;

  [SetUp]
  public void SetUp()
  {
    _tempDir = SysIO.Path.Combine(SysIO.Path.GetTempPath(), $"flowthru-parquet-perf-{Guid.NewGuid():N}");
    SysIO.Directory.CreateDirectory(_tempDir);
  }

  [TearDown]
  public void TearDown()
  {
    if (SysIO.Directory.Exists(_tempDir))
    {
      try { SysIO.Directory.Delete(_tempDir, recursive: true); }
      catch { /* best effort */ }
    }
  }

  [Test]
  public async Task ShallowInspect_CompletesWithinBudget_On50kRows()
  {
    var path = SysIO.Path.Combine(_tempDir, "data.parquet");
    var options = new ParquetItemOptions<PerfRow> { RowGroupSize = RowGroupSize };

    // Seed a multi-row-group file so the streaming claim has something
    // to short-circuit.
    var seedItem = ItemFactory.Enumerable.Parquet<PerfRow>("seed", path, options: options);
    var rows = Enumerable.Range(0, RowCount).Select(i => new PerfRow
    {
      Id = i,
      Name = $"row-{i}",
      Score = i * 0.1,
    }).ToArray();
    var saveResult = await seedItem.Save(rows).Run();
    Assert.That(saveResult, Is.InstanceOf<EffResult<FlowUnit>.Success>(),
      "Precondition: the seed file must save successfully.");

    // The actual guardrail: shallow inspect must complete fast enough
    // that a full-file read could not have happened. A correct
    // implementation reads one row group's worth of rows then breaks.
    var probeItem = ItemFactory.Enumerable.Parquet<PerfRow>("probe", path);
    var sw = Stopwatch.StartNew();
    var inspectResult = await probeItem.InspectShallow(SampleSize).Run();
    sw.Stop();

    Assert.That(inspectResult, Is.InstanceOf<EffResult<ValidationResult>.Success>());
    var validation = ((EffResult<ValidationResult>.Success)inspectResult).Value;
    Assert.That(validation.IsValid, Is.True,
      $"Shallow inspect must succeed on a well-formed file. "
      + $"Errors: {string.Join("; ", validation.Errors.Select(e => e.Message))}");
    Assert.That(sw.ElapsedMilliseconds, Is.LessThan(BudgetMs),
      $"Shallow inspect on a {RowCount}-row Parquet file should not "
      + $"materialise the whole dataset. Budget: {BudgetMs}ms; actual: {sw.ElapsedMilliseconds}ms. "
      + "If this fails, ParquetFormatSerializer.DeserializeRows likely lost its "
      + "row-group-at-a-time streaming behavior.");
  }
}
