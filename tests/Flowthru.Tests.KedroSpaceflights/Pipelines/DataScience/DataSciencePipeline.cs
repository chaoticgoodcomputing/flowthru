using Flowthru.Pipelines;
using Flowthru.Tests.KedroSpaceflights.Data;
using Flowthru.Tests.KedroSpaceflights.Data.Schemas.Models;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataEvaluation.Nodes;
using Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience.Nodes;

namespace Flowthru.Tests.KedroSpaceflights.Pipelines.DataScience;

/// <summary>
/// Data science pipeline that splits data, trains model, and evaluates performance.
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
public static class DataSciencePipeline
{
  /// <summary>
  /// Parameters for the data science pipeline nodes.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Options for model training.
    /// </summary>
    public CreateTestTrainSplitNode.TestTrainSplitParams ModelParams { get; init; } = new();
  }

  public static Pipeline Create(SpaceflightsCatalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Split data into train/test sets (single input → multi-output)
      pipeline.AddNode(
        label: "CreateTestTrainSplitDatasets",
        transform: CreateTestTrainSplitNode.Create(parameters: parameters.ModelParams),
        input: catalog.ModelInputTable,
        output: (catalog.XTrain, catalog.XTest, catalog.YTrain, catalog.YTest)
      );

      // Node 2: Train OLS regression model (multi-input → single output)
      pipeline.AddNode(
        label: "TrainOLSModel",
        transform: TrainModelNode.Create(),
        input: (catalog.XTrain, catalog.YTrain),
        output: catalog.Regressor
      );
    });
  }
}
