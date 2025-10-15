using Flowthru.Pipelines;

namespace Flowthru.Spaceflights.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline that preprocesses raw data and creates model input table.
/// </summary>
public static class DataProcessingPipeline
{
  public static Pipeline Create(DataCatalog catalog)
  {
    return PipelineBuilder.CreatePipeline(catalog, pipeline =>
    {
      pipeline
        .AddNode<Nodes.PreprocessCompaniesNode>()
        .AddNode<Nodes.PreprocessShuttlesNode>()
        .AddNode<Nodes.CreateModelInputTableNode>();
    });
  }
}
