using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Nodes;
using Flowthru.Pipelines;
using KedroIrisPython.Data;
using KedroIrisPython.Data._01_Raw.Schemas;
using KedroIrisPython.Data._05_ModelInput.Schemas;

namespace KedroIrisPython.Pipelines.DataEngineering;

/// <summary>
/// Data engineering pipeline using Python node for train/test splitting.
/// </summary>
public static class DataEngineeringPipeline
{
  /// <summary>
  /// Creates the data engineering pipeline.
  /// </summary>
  public static Pipeline Create(Catalog catalog, IPythonExecutor executor)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddPythonNode<
        IEnumerable<IrisRawSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>,
        IEnumerable<FeatureVectorSchema>,
        IEnumerable<TargetLabelSchema>
      >(
        label: "SplitData",
        description: "Split iris data into train/test sets (Python 1×4 node)",
        module: "Pipelines.DataEngineering.Nodes.split_data",
        function: "split_data",
        input: catalog.IrisRaw,
        output: (catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY),
        executor: executor
      );
    });
  }
}
