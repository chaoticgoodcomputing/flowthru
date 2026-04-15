using Flowthru.Core.Flows;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Flows.DataProcessing.Steps;

namespace SpaceflightsDistributed.DataProcessing.Flows.DataProcessing;

/// <summary>
/// Preprocesses raw shuttle and company data and joins it with reviews to
/// produce a unified model input table.
/// </summary>
public static class DataProcessingFlow
{
    public static Flow Create(DataProcessingCatalog catalog)
    {
        return FlowBuilder.CreateFlow(pipeline =>
        {
            pipeline.AddStep(
          label: "PreprocessCompanies",
          description: "Cleans and preprocesses raw company data.",
          transform: PreprocessCompaniesStep.Create(),
          input: catalog.Companies,
          output: catalog.PreprocessedCompanies
        );

            pipeline.AddStep(
          label: "PreprocessShuttles",
          description: "Cleans and preprocesses raw shuttle data.",
          transform: PreprocessShuttlesStep.Create(),
          input: catalog.Shuttles,
          output: catalog.PreprocessedShuttles
        );

            pipeline.AddStep(
          label: "CreateModelInputTable",
          description: "Joins preprocessed shuttle and company data with review scores.",
          transform: CreateModelInputTableStep.Create(),
          input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
          output: catalog.ModelInputTable
        );
        });
    }
}
