using Flowthru.Flows;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Pipelines.DataProcessing.Nodes;

namespace KedroSpaceflights.Custom.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
///
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// This pipeline uses a strongly-typed catalog (Catalog) to ensure:
/// - All catalog references are validated at compile-time
/// - Node input/output types must match catalog entry types
/// - CatalogMap enforces property-to-catalog type consistency
/// - Refactoring tools work seamlessly (rename, find references)
/// - IntelliSense shows available catalog entries with their types
/// </para>
///
/// <para><strong>Matches Kedro 1:1:</strong></para>
/// <para>
/// This pipeline now matches the original Kedro spaceflights data_processing pipeline,
/// with the addition of PreprocessReviewsNode (a minor refactor for better data handling).
/// All diagnostic nodes have been moved to the DataDiagnostics pipeline.
/// </para>
/// </summary>
public static class DataProcessingPipeline
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Node 1: Preprocess companies (simple: single input → single output)
      pipeline.AddStep(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesNode.Create(),
        input: catalog.Companies,
        output: catalog.CleanedCompanies
      );

      // Node 2: Preprocess shuttles (simple: single input → single output)
      pipeline.AddStep(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesNode.Create(),
        input: catalog.Shuttles,
        output: catalog.CleanedShuttles
      );

      // Node 3: Preprocess reviews (simple: single input → single output)
      // Note: Minor refactor compared to Kedro - we preprocess reviews separately
      // rather than handling raw reviews in create_model_input_table
      pipeline.AddStep(
        label: "PreprocessReviews",
        transform: PreprocessReviewsNode.Create(),
        input: catalog.Reviews,
        output: catalog.CleanedReviews
      );

      // Node 4: Create model input table (multi-input: 3 inputs → single output)
      pipeline.AddStep(
        label: "CreateModelInputTable",
        transform: CreateModelInputTableNode.Create(),
        input: (catalog.CleanedShuttles, catalog.CleanedCompanies, catalog.CleanedReviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
