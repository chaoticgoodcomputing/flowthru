using Flowthru.Step;
using WideTransformBenchmark.Data._01_Raw.Schemas;
using WideTransformBenchmark.Data._02_Intermediate.Schemas;
#if FUNIT_ENABLED
using Flowthru.Step.Testing;
#endif

namespace WideTransformBenchmark.Flows.EagerOptimize.Steps;

/// <summary>
/// The optimize pass as an ordinary C# Step — the rows-in-CLR baseline the
/// engine path is measured against. Sort by the composite key, keep the
/// first-ingested row per key, project down to the kept columns. Every input
/// row is materialised as a CLR object, so wall-clock and managed allocations
/// both grow with the input.
/// </summary>
/// <remarks>
/// "First-ingested per key" falls out of LINQ semantics: the Parquet item
/// yields rows in file order (ascending <see cref="RawReadingRow.RowId"/>),
/// <c>OrderBy</c>/<c>ThenBy</c> are stable, and <c>DistinctBy</c> keeps the
/// first occurrence — so the survivor per key is the lowest-<c>RowId</c> row,
/// exactly what the engine path's <c>ORDER BY RowId</c> window picks. Ordinal
/// string comparison matches DuckDB's default binary collation.
/// </remarks>
[FlowthruStep]
public static class OptimizeReadingsEagerStep
{
  public static Func<IEnumerable<RawReadingRow>, IEnumerable<OptimizedReadingRow>> Create()
  {
    return rows => rows
      .OrderBy(r => r.DeviceId, StringComparer.Ordinal)
      .ThenBy(r => r.Channel, StringComparer.Ordinal)
      .ThenBy(r => r.ObservedAt)
      .DistinctBy(r => (r.DeviceId, r.Channel, r.ObservedAt))
      .Select(r => new OptimizedReadingRow
      {
        DeviceId = r.DeviceId,
        Channel = r.Channel,
        ObservedAt = r.ObservedAt,
        Reading = r.Reading,
        Unit = r.Unit,
      })
      .ToList();
  }

#if FUNIT_ENABLED
  /// <summary>FUnit tests for <see cref="OptimizeReadingsEagerStep"/>.</summary>
  public class Tests : FUnitContext
  {
    private static RawReadingRow Row(
      long rowId,
      string deviceId = "dev-0001",
      string channel = "temp",
      int second = 0,
      double reading = 1.0
    ) =>
      new()
      {
        RowId = rowId,
        DeviceId = deviceId,
        Channel = channel,
        ObservedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(second),
        Reading = reading,
        Unit = "C",
        SourceFile = "ingest/batch_00000.jsonl",
        IngestedBy = "collector-00",
        RawPayload = "{}",
        BatchId = 0,
        Checksum = "00000000",
      };

    [FUnitStepTest(typeof(OptimizeReadingsEagerStep))]
    public void EmptyInput_YieldsEmptyOutput()
    {
      var result = Invoke(OptimizeReadingsEagerStep.Create(), Enumerable.Empty<RawReadingRow>());

      Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// The dedup contract both paths must share: among duplicate composite
    /// keys, the survivor is the first-ingested row (lowest RowId) — the
    /// same row the engine path's <c>ORDER BY RowId</c> window picks.
    /// </summary>
    [FUnitStepTest(typeof(OptimizeReadingsEagerStep))]
    public void DuplicateKeys_KeepFirstIngestedRow()
    {
      var rows = new[]
      {
        Row(rowId: 0, reading: 10.0),
        Row(rowId: 1, reading: 20.0), // duplicate key, later ingest — dropped
        Row(rowId: 2, second: 1, reading: 30.0),
      };

      var result = Invoke(OptimizeReadingsEagerStep.Create(), rows).ToList();

      Assert.That(result, Has.Count.EqualTo(2));
      Assert.That(result[0].Reading, Is.EqualTo(10.0));
      Assert.That(result[1].Reading, Is.EqualTo(30.0));
    }

    [FUnitStepTest(typeof(OptimizeReadingsEagerStep))]
    public void Output_IsSortedByCompositeKey_Ordinal()
    {
      var rows = new[]
      {
        Row(rowId: 0, deviceId: "dev-0002", channel: "temp"),
        Row(rowId: 1, deviceId: "dev-0001", channel: "vibration"),
        Row(rowId: 2, deviceId: "dev-0001", channel: "humidity", second: 5),
        Row(rowId: 3, deviceId: "dev-0001", channel: "humidity", second: 2),
      };

      var keys = Invoke(OptimizeReadingsEagerStep.Create(), rows)
        .Select(r => (r.DeviceId, r.Channel, r.ObservedAt.Second))
        .ToList();

      Assert.That(keys, Is.EqualTo(new[]
      {
        ("dev-0001", "humidity", 2),
        ("dev-0001", "humidity", 5),
        ("dev-0001", "vibration", 0),
        ("dev-0002", "temp", 0),
      }));
    }

    [FUnitStepTest(typeof(OptimizeReadingsEagerStep))]
    public void Projection_PrunesLineageColumns_AndKeepsValues()
    {
      var result = Invoke(OptimizeReadingsEagerStep.Create(), new[] { Row(rowId: 7, reading: 42.5) })
        .Single();

      Assert.That(result, Is.EqualTo(new OptimizedReadingRow
      {
        DeviceId = "dev-0001",
        Channel = "temp",
        ObservedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Reading = 42.5,
        Unit = "C",
      }));
    }
  }
#endif
}
