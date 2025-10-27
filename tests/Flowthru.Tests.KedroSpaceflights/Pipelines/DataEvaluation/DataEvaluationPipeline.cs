using Flowthru.Data.Implementations;
using Flowthru.Nodes;
using Flowthru.Pipelines;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Processed;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataEvaluation.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataEvaluation;

/// <summary>
/// Parameters for the data evaluation pipeline.
/// </summary>
public class DataEvaluationPipelineParams {
  /// <summary>
  /// Options for cross-validation.
  /// </summary>
  public CrossValidationParams CrossValidationParams { get; init; } = new();
}

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
public static class DataEvaluationPipeline {
  public static Pipeline Create(SpaceflightsCatalog catalog, DataEvaluationPipelineParams parameters) {
    return PipelineBuilder.CreatePipeline(pipeline => {

      // Node 1: Evaluate OLS model (multi-input → multi-output)
      pipeline.AddNode<EvaluateModelNode>(
        label: "EvaluateOLSModel",
        input: (catalog.Regressor, catalog.XTest, catalog.YTest),
        output: (catalog.ModelMetrics, catalog.ModelPredictions)
      );

      // Node 2: Cross-validation for R² distribution analysis and comparison to Kedro
      pipeline.AddNode<CrossValidateModelNode>(
        label: "PerformCrossValidatedOLSRegressionTest",
        input: catalog.ModelInputTable,
        output: catalog.CrossValidationResults,
        configure: node => node.Parameters = parameters.CrossValidationParams
      );

    });
  }
}
