using Flowthru.Flow;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Step.Testing;
using WideTransformBenchmark.Data;

namespace WideTransformBenchmark.Flows.EngineOptimize;

/// <summary>
/// The engine path: the same optimize pass as
/// <c>OptimizeReadingsEagerStep</c>, expressed as one SQL statement and run
/// entirely inside the embedded DuckDB engine. The rows never enter the CLR —
/// the engine reads the input Parquet, transforms, and writes the output
/// Parquet itself, which is why this path's managed allocations stay flat as
/// the dataset grows.
/// </summary>
public static class EngineOptimizeFlow
{
  /// <summary>
  /// The optimize pass, engine-side. Semantically identical to the eager
  /// Step: keep the first-ingested row per composite key (lowest RowId, via
  /// the QUALIFY window), prune the lineage columns (the SELECT list), and
  /// sort by the composite key. DuckDB's default binary collation matches the
  /// eager Step's ordinal string comparison.
  /// </summary>
  public const string OptimizePassSql = """
    SELECT DeviceId, Channel, ObservedAt, Reading, Unit
    FROM readings
    QUALIFY row_number() OVER (
      PARTITION BY DeviceId, Channel, ObservedAt
      ORDER BY RowId
    ) = 1
    ORDER BY DeviceId, Channel, ObservedAt
    """;

  public static BuiltFlow Create(SizedBenchmarkCatalog catalog, IDuckDbEngine engine) =>
    FlowBuilder.CreateFlow($"EngineOptimize_{catalog.RowCount}", flow =>
      flow.AddDuckDbTransform(
        label: $"OptimizeReadingsEngine_{catalog.RowCount}",
        // Bind the per-size item to a fixed relation name so one SQL constant
        // serves every dataset size.
        inputs: [DuckDbInputRelation.From(catalog.RawReadings, "readings")],
        output: catalog.EngineOptimized,
        sql: OptimizePassSql,
        engine: engine));

#if FUNIT_ENABLED
  /// <summary>Design-time checks for this Flow's engine-side SQL.</summary>
  public class Tests : FUnitContext
  {
    /// <summary>
    /// Binds the optimize pass's SQL against the declared input Schema and
    /// verifies the result against the declared output Schema — no data is
    /// read, so a schema-breaking SQL edit fails this test before any
    /// benchmark runs.
    /// </summary>
    [Test]
    public async Task TransformSqlAgreesWithDeclaredSchemas()
    {
      var flow = Create(
        new SizedBenchmarkCatalog("Data", 10_000), new InProcessDuckDbEngine());

      var result = await flow.ValidateDuckDbTransforms();

      Assert.That(result.IsValid, Is.True,
        string.Join("\n", result.Errors.Select(e => e.Message)));
    }
  }
#endif
}
