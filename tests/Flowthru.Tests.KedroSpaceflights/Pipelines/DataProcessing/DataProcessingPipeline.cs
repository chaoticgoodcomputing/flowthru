using Flowthru.Data.Implementations;
using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Pipelines.Mapping;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Raw;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
/// 
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// This pipeline uses a strongly-typed catalog (SpaceflightsCatalog) to ensure:
/// - All catalog references are validated at compile-time
/// - Node input/output types must match catalog entry types
/// - CatalogMap enforces property-to-catalog type consistency
/// - Refactoring tools work seamlessly (rename, find references)
/// - IntelliSense shows available catalog entries with their types
/// </para>
/// </summary>
public static class DataProcessingPipeline
{
  public static Pipeline Create(SpaceflightsCatalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Preprocess companies (simple: single input → single output)
      pipeline.AddNode<PreprocessCompaniesNode, CompanyRawSchema, CompanySchema, NoParams>(
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies,
        name: "PreprocessCompanies"
      );

      // Node 2: Preprocess shuttles (simple: single input → single output)
      pipeline.AddNode<PreprocessShuttlesNode, ShuttleRawSchema, ShuttleSchema, NoParams>(
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles,
        name: "PreprocessShuttles"
      );

      // Node 3: Preprocess reviews (simple: single input → single output)
      pipeline.AddNode<PreprocessReviewsNode, ReviewRawSchema, ReviewSchema, NoParams>(
        input: catalog.Reviews,
        output: catalog.PreprocessedReviews,
        name: "PreprocessReviews"
      );

      // Node 4: Create model input table (multi-input: 3 inputs → single output)
      var createModelInputs = pipeline.AddNode<CreateModelInputTableNode, CreateModelInputTableInputs, ModelInputSchema, NoParams>(
        name: "CreateModelInputTable",
        input: new CatalogMap<CreateModelInputTableInputs>()
          .Map(x => x.Shuttles, catalog.PreprocessedShuttles)
          .Map(x => x.Companies, catalog.PreprocessedCompanies)
          .Map(x => x.Reviews, catalog.PreprocessedReviews),
        output: catalog.ModelInputTable
      );

      // DIAGNOSTIC NODES
      //
      // This differs from the original Kedro source — however, for manual inspection, these nodes
      // are useful for manual review. They also flex additional points of functionality, such as:
      // - Ability to perform pure, no-ouput diagnostics;
      // - Ability to re-use generic nodes like ExportToCsvNode with generic types; and
      // - Ability to re-use the same node for different inputs of the same schema.

      // Validation: validate quality of our generated model input table against the original Kedro
      // output.
      pipeline.AddNode<ValidateAgainstKedroNode, ValidateAgainstKedroInputs, ModelInputSchema, NoParams>(
        name: "ValidateModelInputTableAgainstKedroSource",
        input: new CatalogMap<ValidateAgainstKedroInputs>()
          .Map(x => x.FlowthruData, catalog.ModelInputTable)
          .Map(x => x.KedroData, catalog.KedroModelInputTable),
        output: new MemoryCatalogDataset<ModelInputSchema>("_validation_throwaway")
      );

      // Siphon off postprocessed companies for manual inspection
      pipeline.AddNode<ExportToCsvNode<CompanySchema>, CompanySchema, CompanySchema, NoParams>(
        name: "ExportCompaniesToDiagnosticCsv",
        input: catalog.PreprocessedCompanies,
        output: catalog.PreprocessedCompaniesCsv
      );

      // Siphon off postprocessed shuttles for manual inspection
      pipeline.AddNode<ExportToCsvNode<ShuttleSchema>, ShuttleSchema, ShuttleSchema, NoParams>(
        name: "ExportShuttlesToDiagnosticCsv",
        input: catalog.PreprocessedShuttles,
        output: catalog.PreprocessedShuttlesCsv
      );

      // Siphon off model output for manual inspection
      pipeline.AddNode<ExportToCsvNode<ModelInputSchema>, ModelInputSchema, ModelInputSchema, NoParams>(
        name: "ExportModelInputTableToDiagnosticCsv",
        input: catalog.ModelInputTable,
        output: catalog.ModelInputTableCsv
      );

    });
  }
}
