using Flowthru.Pipelines;
using KedroSpaceflights.Pure.Data;
using KedroSpaceflights.Pure.Pipelines.DataProcessing.Nodes;

namespace KedroSpaceflights.Pure.Pipelines.DataProcessing;

public static class DataProcessingPipeline
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        name: "PreprocessCompanies",
        transform: PreprocessCompaniesNode.Create(),
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies
      );

      pipeline.AddNode(
        name: "PreprocessShuttles",
        transform: PreprocessShuttlesNode.Create(),
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles
      );

      pipeline.AddNode(
        name: "CreateModelInputTable",
        transform: CreateModelInputTableNode.Create(),
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
