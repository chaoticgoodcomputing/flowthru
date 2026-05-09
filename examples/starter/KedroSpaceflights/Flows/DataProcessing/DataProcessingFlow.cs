using Flowthru.Flow;
using KedroSpaceflights.Data;
using KedroSpaceflights.Data._01_Raw.Schemas;
using KedroSpaceflights.Data._02_Intermediate.Schemas;
using KedroSpaceflights.Data._03_Primary.Schemas;
using KedroSpaceflights.Flows.DataProcessing.Steps;

namespace KedroSpaceflights.Flows.DataProcessing;

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
        input1: catalog.Companies,
        output1: catalog.PreprocessedCompanies
      );

      pipeline.AddStep<IEnumerable<ShuttleSchema>, IEnumerable<PreprocessedShuttleSchema>>(
        label: "PreprocessShuttles",
        transform: PreprocessShuttlesStep.Create(),
        input1: catalog.Shuttles,
        output1: catalog.PreprocessedShuttles
      );

      pipeline.AddStep<
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<ReviewSchema>,
        IEnumerable<ModelInputTableSchema>
      >(
        label: "CreateModelInputTable",
        transform: CreateModelInputTableStep.Create(),
        input1: catalog.PreprocessedShuttles,
        input2: catalog.PreprocessedCompanies,
        input3: catalog.Reviews,
        output1: catalog.ModelInputTable
      );
    });
  }
}
