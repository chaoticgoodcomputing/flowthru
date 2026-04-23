using Flowthru.Core.Flows;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Data._03_Primary.Schemas;
using KedroSpaceflights.Custom.Flows.DataEvaluation.Steps;
using KedroSpaceflights.Custom.Flows.DataScience.Steps;

namespace KedroSpaceflights.Custom.Flows.DataScience;

/// <summary>
/// Data science pipeline that splits data, trains model, and evaluates performance.
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
/// <para><strong>Zero Runtime Type Errors:</strong></para>
/// <para>
/// If this code compiles, the pipeline is correctly wired. Type mismatches
/// between nodes and catalog entries will cause compilation failures, not runtime errors.
/// </para>
///
/// <para><strong>Matches Kedro 1:1:</strong></para>
/// <para>
/// This pipeline now matches the original Kedro spaceflights data_science pipeline exactly.
/// Cross-validation has been moved to the DataDiagnostics pipeline.
/// </para>
/// </summary>
public static class DataScienceFlow
{
  public static Flow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Step 1: Split data into train/test sets (single input → multi-output)
      pipeline.AddStep(
        label: "CreateTestTrainSplitDatasets",
        transform: CreateTestTrainSplitStep.Create,
        input: (catalog.ModelInputTable, config.ModelParams),
        output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
      );

      // Step 2: Train OLS regression model (multi-input → single output)
      pipeline.AddStep(
        label: "TrainOLSModel",
        transform: TrainModelStep.Create(),
        input: (catalog.XTrain, catalog.YTrain),
        output: catalog.Regressor
      );
    });
  }
}
