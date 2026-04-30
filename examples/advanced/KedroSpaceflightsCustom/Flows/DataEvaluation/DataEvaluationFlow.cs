using Flowthru.Core.Flows;
using Flowthru.Core.Steps;
using KedroSpaceflightsCustom.Data;
using KedroSpaceflightsCustom.Data._03_Primary.Schemas;
using KedroSpaceflightsCustom.Data._04_Models.Schemas;
using KedroSpaceflightsCustom.Data._05_ModelOutput.Schemas;
using KedroSpaceflightsCustom.Flows.DataEvaluation.Steps;

namespace KedroSpaceflightsCustom.Flows.DataEvaluation;

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
/// <item>ValidateAgainstKedroStep - Compares Flowthru vs Kedro model input table (demonstrates 2-input, 0-output side-effect nodes)</item>
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
  public static Flow Create(Catalog catalog, FlowConfig config)
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
        transform: CrossValidateModelStep.Create,
        input: (catalog.ModelInputTable, config.CrossValidationParams),
        output: catalog.CrossValidationResults
      );
    });
  }
}
