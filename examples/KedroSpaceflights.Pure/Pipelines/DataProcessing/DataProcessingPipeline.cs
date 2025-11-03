using Flowthru.Pipelines;
using KedroSpaceflights.Pure.Data;
using KedroSpaceflights.Pure.Pipelines.DataProcessing.Nodes;

namespace KedroSpaceflights.Pure.Pipelines.DataProcessing;

/// <summary>
/// Creates the data processing pipeline that preprocesses raw company and shuttle data
/// and joins it with reviews to create a model input table.
/// </summary>
public static class DataProcessingPipeline
{
  /// <summary>
  /// Creates the data processing pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <returns>A configured pipeline that produces a model input table from raw data sources.</returns>
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
