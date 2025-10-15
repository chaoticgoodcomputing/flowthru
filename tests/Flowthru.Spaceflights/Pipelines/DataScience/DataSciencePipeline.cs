using Flowthru.Pipelines;
using Flowthru.Spaceflights.Pipelines.DataScience.Parameters;

namespace Flowthru.Spaceflights.Pipelines.DataScience;

/// <summary>
/// Data science pipeline that splits data, trains model, and evaluates performance.
/// </summary>
public static class DataSciencePipeline
{
  public static Pipeline Create(DataCatalog catalog, ModelOptions? options = null)
  {
    return PipelineBuilder.CreatePipeline(catalog, pipeline =>
    {
      var splitNode = new Nodes.SplitDataNode
      {
        Parameters = options ?? new ModelOptions()
      };

      pipeline
              .AddNode(splitNode)
              .AddNode<Nodes.TrainModelNode>()
              .AddNode<Nodes.EvaluateModelNode>();
    });
  }
}
