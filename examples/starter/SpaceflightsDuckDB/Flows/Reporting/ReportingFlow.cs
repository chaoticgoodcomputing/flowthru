using Flowthru.Flow;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Step.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceflightsDuckDB.Data;
using SpaceflightsDuckDB.Data._08_Reporting.Schemas;
using SpaceflightsDuckDB.Flows.Reporting.Steps;

namespace SpaceflightsDuckDB.Flows.Reporting;

/// <summary>
/// Reporting pipeline that aggregates the model input table into per-company
/// summaries inside the embedded DuckDB engine, then formats a small
/// top-rated-companies report in C#.
/// </summary>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog, IDuckDbEngine engine, ILogger logger)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      // Aggregation is wide work — every output row depends on many input
      // rows — so it also runs engine-side. With a single input and no
      // explicit binding, the relation is named after the Item's label.
      // DuckDB widens aggregates (SUM over an integer column comes back as a
      // 128-bit integer), so the SQL CASTs each aggregate onto the type the
      // output Schema declares; COUNT(*) is already a 64-bit integer.
      #region docs:transform-duckdb
      pipeline.AddDuckDbTransform(
        label: "SummarizeCompanies",
        input: catalog.ModelInputTable,
        output: catalog.CompanySummaries,
        sql: """
          SELECT
            company_id,
            COUNT(*)                                   AS shuttle_count,
            CAST(AVG(price) AS DOUBLE)                 AS avg_price,
            CAST(AVG(review_scores_rating) AS DOUBLE)  AS avg_review_score,
            CAST(SUM(passenger_capacity) AS BIGINT)    AS total_passenger_capacity
          FROM ModelInputTable
          GROUP BY company_id
          ORDER BY avg_review_score DESC, company_id
          """,
        engine: engine
      );
      #endregion

      // The summaries are small (one row per company), so they cross back into
      // C# here for the narrow per-row work: rank, round, and cut the report.
      pipeline.AddStep<IEnumerable<CompanySummarySchema>, IEnumerable<CompanyRatingReport>>(
        label: "CreateCompanyRatingReport",
        transform: CreateCompanyRatingReportStep.Create(logger),
        inputs: catalog.CompanySummaries,
        outputs: catalog.CompanyRatingReport
      );
    });
  }

#if FUNIT_ENABLED
  /// <summary>Design-time checks for this Flow's engine-side SQL.</summary>
  public class Tests : FUnitContext
  {
    /// <summary>
    /// Binds every DuckDB transform's SQL against the declared input Schemas and
    /// verifies the result against the declared output Schema — no data is read,
    /// so a schema-breaking SQL edit fails this test before anything runs.
    /// </summary>
    [Test]
    public async Task TransformSqlAgreesWithDeclaredSchemas()
    {
      var flow = Create(new Catalog("Data"), new InProcessDuckDbEngine(), NullLogger.Instance);

      var result = await flow.ValidateDuckDbTransforms();

      Assert.That(result.IsValid, Is.True,
        string.Join("\n", result.Errors.Select(e => e.Message)));
    }
  }
#endif
}
