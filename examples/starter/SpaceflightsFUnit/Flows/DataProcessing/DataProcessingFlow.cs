using Flowthru.Flow;
using SpaceflightsFUnit.Data;
using SpaceflightsFUnit.Data._01_Raw.Schemas;
using SpaceflightsFUnit.Data._02_Intermediate.Schemas;
using SpaceflightsFUnit.Data._03_Primary.Schemas;
using SpaceflightsFUnit.Flows.DataProcessing.Steps;

namespace SpaceflightsFUnit.Flows.DataProcessing;

/// <summary>
/// Creates the data processing pipeline that preprocesses raw company and shuttle data
/// and joins it with reviews to create a model input table.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog)
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
