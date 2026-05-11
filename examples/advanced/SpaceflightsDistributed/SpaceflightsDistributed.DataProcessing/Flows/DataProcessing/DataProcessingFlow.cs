using Flowthru.Flow;
using SpaceflightsDistributed.DataProcessing.Data;
using SpaceflightsDistributed.DataProcessing.Data._01_Raw.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._02_Intermediate.Schemas;
using SpaceflightsDistributed.DataProcessing.Data._03_Primary.Schemas;
using SpaceflightsDistributed.DataProcessing.Flows.DataProcessing.Steps;

namespace SpaceflightsDistributed.DataProcessing.Flows.DataProcessing;

/// <summary>
/// Preprocesses raw shuttle and company data and joins it with reviews to
/// produce a unified model input table.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(DataProcessingCatalog catalog)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>>(
        label: "PreprocessCompanies",
        transform: PreprocessCompaniesStep.Create(),
        inputs: catalog.Companies,
        outputs: catalog.PreprocessedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        inputs: catalog.Shuttles,
        outputs: catalog.PreprocessedShuttles
      );

      pipeline.AddStep<
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<ReviewSchema>,
        IEnumerable<ModelInputTableSchema>
      >(
        label: "CreateModelInputTable",
        transform: CreateModelInputTableStep.Create(),
        inputs: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        outputs: catalog.ModelInputTable
      );
    });
  }
}
