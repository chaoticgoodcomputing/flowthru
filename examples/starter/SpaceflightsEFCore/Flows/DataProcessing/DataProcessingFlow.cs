using Flowthru.Flow;
using SpaceflightsEFCore.Data;
using SpaceflightsEFCore.Data._01_Raw.Schemas;
using SpaceflightsEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsEFCore.Data._03_Primary.Schemas;
using SpaceflightsEFCore.Flows.DataProcessing.Steps;

namespace SpaceflightsEFCore.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline: cleans raw company + shuttle data and joins
/// them with reviews to produce a unified model input table.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog, FlowConfig config)
  {
    var preprocessCompanies = PreprocessCompaniesStep.Create();
    var preprocessShuttles = PreprocessShuttlesStep.Create();
    var createModelInputTable = CreateModelInputTableStep.Create();

    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddStep<IEnumerable<CompanySchema>, IEnumerable<PreprocessedCompanySchema>>(
        label: "PreprocessCompanies",
        transform: preprocessCompanies,
        inputs: catalog.Companies,
        outputs: catalog.PreprocessedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: preprocessShuttles,
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
        transform: createModelInputTable,
        inputs: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        outputs: catalog.ModelInputTable
      );
    });
  }
}
