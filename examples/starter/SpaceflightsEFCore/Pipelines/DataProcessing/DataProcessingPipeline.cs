using Flowthru.Flows;
using SpaceflightsEFCore.Data;
using SpaceflightsEFCore.Pipelines.DataProcessing.Nodes;

namespace SpaceflightsEFCore.Pipelines.DataProcessing;

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
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PreprocessCompanies",
        description: "Cleans and preprocesses raw company data.",
        transform: PreprocessCompaniesNode.Create(),
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies
      );

      pipeline.AddStep(
        label: "PreprocessShuttles",
        description: "Cleans and preprocesses raw shuttle data.",
        transform: PreprocessShuttlesNode.Create(),
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles
      );

      pipeline.AddNode(
        label: "CreateModelInputTable",
        description: """
          Joins preprocessed shuttle and company data with review scores to create a
          unified model input table.
        """,
        transform: CreateModelInputTableNode.Create(),
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
