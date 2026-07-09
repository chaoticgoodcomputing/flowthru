using Flowthru.Flow;
using Flowthru.Step.DuckDb;
using Flowthru.Step.DuckDb.Internal;
using Flowthru.Step.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceflightsDuckDB.Data;
using SpaceflightsDuckDB.Data._01_Raw.Schemas;
using SpaceflightsDuckDB.Data._02_Intermediate.Schemas;
using SpaceflightsDuckDB.Flows.DataProcessing.Steps;

namespace SpaceflightsDuckDB.Flows.DataProcessing;

/// <summary>
/// Creates the data processing pipeline that preprocesses raw company, shuttle, and
/// review data in C#, then joins the typed Parquet outputs into a model input table
/// with SQL executed inside the embedded DuckDB engine.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog, IDuckDbEngine engine, ILogger logger)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>>(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(logger),
        inputs: catalog.Companies,
        outputs: catalog.PreprocessedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(logger),
        inputs: catalog.Shuttles,
        outputs: catalog.PreprocessedShuttles
      );

      pipeline.AddStep<IEnumerable<ReviewSchema>, IEnumerable<PreprocessedReviewSchema>>(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(logger),
        inputs: catalog.Reviews,
        outputs: catalog.PreprocessedReviews
      );

      // The three-way join runs as SQL inside the embedded DuckDB engine: each
      // input Item binds to a relation name, the query is the step body, and
      // the result is written straight to the output Item's Parquet file. The
      // joined rows never enter this process. Column names in the SQL are the
      // Schemas' serialized labels (the names in the Parquet files).
      pipeline.AddDuckDbTransform(
        label: "CreateModelInputTable",
        inputs:
        [
          DuckDbInputRelation.From(catalog.PreprocessedShuttles, "shuttles"),
          DuckDbInputRelation.From(catalog.PreprocessedCompanies, "companies"),
          DuckDbInputRelation.From(catalog.PreprocessedReviews, "reviews"),
        ],
        output: catalog.ModelInputTable,
        sql: """
          SELECT
            shuttles.id AS shuttle_id,
            shuttles.shuttle_type,
            shuttles.company_id,
            shuttles.engines,
            shuttles.passenger_capacity,
            shuttles.crew,
            shuttles.d_check_complete,
            shuttles.moon_clearance_complete,
            shuttles.price,
            companies.iata_approved,
            companies.company_rating,
            -- Try it: replace the line above with the misspelled one below and
            -- run `dotnet run` (or `dotnet test`). The Flow fails pre-flight
            -- before any step runs — see the README for the exact diagnostic.
            -- companies.company_ratings,
            reviews.review_scores_rating
          FROM shuttles
          JOIN reviews   ON reviews.shuttle_id = shuttles.id
          JOIN companies ON companies.id = shuttles.company_id
          """,
        engine: engine
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
