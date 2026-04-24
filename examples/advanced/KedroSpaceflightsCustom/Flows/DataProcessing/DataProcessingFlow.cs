using Flowthru.Core.Flows;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Flows.DataProcessing.Steps;

namespace KedroSpaceflightsCustom.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// Follows Kedro's pattern: nodes are pure functions, pipeline declares the data flow.
///
/// <para><strong>Compile-Time Type Safety:</strong></para>
/// <para>
/// This pipeline uses a strongly-typed catalog (Catalog) to ensure:
/// - All catalog references are validated at compile-time
/// - Step input/output types must match catalog entry types
/// - CatalogMap enforces property-to-catalog type consistency
/// - Refactoring tools work seamlessly (rename, find references)
/// - IntelliSense shows available catalog entries with their types
/// </para>
///
/// <para><strong>Matches Kedro 1:1:</strong></para>
/// <para>
/// This pipeline now matches the original Kedro spaceflights data_processing pipeline,
/// with the addition of PreprocessReviewsStep (a minor refactor for better data handling).
/// All diagnostic nodes have been moved to the DataDiagnostics pipeline.
/// </para>
/// </summary>
public static class DataProcessingFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Step 1: Preprocess companies (simple: single input → single output)
      pipeline.AddStep(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(),
        input: catalog.Companies,
        output: catalog.CleanedCompanies
      );

      // Step 2: Preprocess shuttles (simple: single input → single output)
      pipeline.AddStep(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        input: catalog.Shuttles,
        output: catalog.CleanedShuttles
      );

      // Step 3: Preprocess reviews (simple: single input → single output)
      // Note: Minor refactor compared to Kedro - we preprocess reviews separately
      // rather than handling raw reviews in create_model_input_table
      pipeline.AddStep(
        label: "PreprocessReviews",
        transform: PreprocessReviewsStep.Create(),
        input: catalog.Reviews,
        output: catalog.CleanedReviews
      );

      // Step 4: Create model input table (multi-input: 3 inputs → single output)
      pipeline.AddStep(
        label: "CreateModelInputTable",
        transform: CreateModelInputTableStep.Create(),
        input: (catalog.CleanedShuttles, catalog.CleanedCompanies, catalog.CleanedReviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
