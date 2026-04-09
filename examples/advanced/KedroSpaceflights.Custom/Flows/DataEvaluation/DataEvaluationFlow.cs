using Flowthru.Core.Flows;
using Flowthru.Core.Steps;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Data._03_Primary.Schemas;
using KedroSpaceflights.Custom.Data._04_Models.Schemas;
using KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;
using KedroSpaceflights.Custom.Flows.DataEvaluation.Steps;

namespace KedroSpaceflights.Custom.Flows.DataEvaluation;

/// <summary>
/// Data validation pipeline that performs diagnostic and validation operations on pipeline outputs.
///
/// <para>
/// This pipeline contains all diagnostic nodes that validate Flowthru's implementation against
/// the original Kedro spaceflights example, as well as nodes that export data to CSV for
/// manual inspection.
/// </para>
///
/// <para><strong>Diagnostic Steps:</strong></para>
/// <list type="bullet">
/// <item>GenerateSyntheticDataStep - Generates test data with NoData input (demonstrates no-input nodes)</item>
/// <item>ValidateAgainstKedroStep - Compares Flowthru vs Kedro model input table (demonstrates no-output nodes)</item>
/// <item>ExportToCsvStep - Exports intermediate datasets to CSV for debugging</item>
/// <item>CrossValidateModelStep - Performs k-fold cross-validation and comparison to Kedro</item>
/// </list>
///
/// <para>
/// Most nodes in this pipeline are pass-through nodes that output their inputs unchanged,
/// making this pipeline safe to run alongside production pipelines without affecting results.
/// </para>
/// </summary>
public static class DataEvaluationFlow
{
  /// <summary>
  /// Parameters for the data evaluation pipeline.
  /// </summary>
  public class Params
  {
    /// <summary>
    /// Options for cross-validation.
    /// </summary>
    public CrossValidateModelStep.Params CrossValidationParams { get; init; } = new();
  }

  public static Flow Create(Catalog catalog, Params parameters)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Step 1: Evaluate OLS model (multi-input → multi-output)
      pipeline.AddStep(
        label: "EvaluateOLSModel",
        transform: EvaluateModelStep.Create(),
        input: (catalog.Regressor, catalog.XTest, catalog.YTest),
        output: (catalog.ModelMetrics, catalog.ModelPredictions)
      );

      // Step 2: Cross-validation for R² distribution analysis and comparison to Kedro
      pipeline.AddStep(
        label: "PerformCrossValidatedOLSRegressionTest",
        transform: CrossValidateModelStep.Create(parameters.CrossValidationParams),
        input: catalog.ModelInputTable,
        output: catalog.CrossValidationResults
      );
    });
  }
}
