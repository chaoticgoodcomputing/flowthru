using Flowthru.Flows;
using Flowthru.Steps;
using KedroSpaceflights.Custom.Data;
using KedroSpaceflights.Custom.Data._03_Primary.Schemas;
using KedroSpaceflights.Custom.Data._04_Models.Schemas;
using KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;
using KedroSpaceflights.Custom.Pipelines.DataEvaluation.Nodes;

namespace KedroSpaceflights.Custom.Pipelines.DataEvaluation;

/// <summary>
/// Data validation pipeline that performs diagnostic and validation operations on pipeline outputs.
///
/// <para>
/// This pipeline contains all diagnostic nodes that validate Flowthru's implementation against
/// the original Kedro spaceflights example, as well as nodes that export data to CSV for
/// manual inspection.
/// </para>
///
/// <para><strong>Diagnostic Nodes:</strong></para>
/// <list type="bullet">
/// <item>GenerateSyntheticDataNode - Generates test data with NoData input (demonstrates no-input nodes)</item>
/// <item>ValidateAgainstKedroNode - Compares Flowthru vs Kedro model input table (demonstrates no-output nodes)</item>
/// <item>ExportToCsvNode - Exports intermediate datasets to CSV for debugging</item>
/// <item>CrossValidateModelNode - Performs k-fold cross-validation and comparison to Kedro</item>
/// </list>
///
/// <para>
/// Most nodes in this pipeline are pass-through nodes that output their inputs unchanged,
/// making this pipeline safe to run alongside production pipelines without affecting results.
/// </para>
/// </summary>
public static class DataEvaluationPipeline
{
  /// <summary>
  /// Parameters for the data evaluation pipeline.
  /// </summary>
  public class Params
  {
    /// <summary>
    /// Options for cross-validation.
    /// </summary>
    public CrossValidateModelNode.Params CrossValidationParams { get; init; } = new();
  }

  public static Flow Create(Catalog catalog, Params parameters)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Node 1: Evaluate OLS model (multi-input → multi-output)
      pipeline.AddStep(
        label: "EvaluateOLSModel",
        transform: EvaluateModelNode.Create(),
        input: (catalog.Regressor, catalog.XTest, catalog.YTest),
        output: (catalog.ModelMetrics, catalog.ModelPredictions)
      );

      // Node 2: Cross-validation for R² distribution analysis and comparison to Kedro
      pipeline.AddStep(
        label: "PerformCrossValidatedOLSRegressionTest",
        transform: CrossValidateModelNode.Create(parameters.CrossValidationParams),
        input: catalog.ModelInputTable,
        output: catalog.CrossValidationResults
      );
    });
  }
}
