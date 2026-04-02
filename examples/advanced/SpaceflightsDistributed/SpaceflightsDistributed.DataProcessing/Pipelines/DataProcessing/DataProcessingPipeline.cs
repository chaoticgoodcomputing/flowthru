using Flowthru.Flows;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Pipelines.DataProcessing.Nodes;

namespace SpaceflightsDistributed.DataProcessing.Pipelines.DataProcessing;

/// <summary>
/// Preprocesses raw shuttle and company data and joins it with reviews to
/// produce a unified model input table.
/// </summary>
public static class DataProcessingPipeline
{
  public static Flow Create(DataProcessingCatalog catalog)
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
        description: "Joins preprocessed shuttle and company data with review scores.",
        transform: CreateModelInputTableNode.Create(),
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
