using Flowthru.Pipelines;
using KedroSpaceflights.Pure.Data;

namespace KedroSpaceflights.Pure.Pipelines.DataScience;

public static class DataSciencePipeline
{
  public record Params
  {
    public Nodes.SplitDataNode.ModelOptions ModelOptions { get; init; } = new();
  }

  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        name: "SplitData",
        transform: Nodes.SplitDataNode.Create(parameters.ModelOptions),
        input: catalog.ModelInputTable,
        output: (catalog.XTrain, catalog.XTest)
      );

      pipeline.AddNode(
        name: "TrainModel",
        transform: Nodes.TrainModelNode.Create(),
        input: catalog.XTrain,
        output: catalog.Regressor
      );

      pipeline.AddNode(
        name: "EvaluateModel",
        transform: Nodes.EvaluateModelNode.Create(),
        input: (catalog.Regressor, catalog.XTest),
        output: catalog.ModelMetrics
      );
    });
  }
}
