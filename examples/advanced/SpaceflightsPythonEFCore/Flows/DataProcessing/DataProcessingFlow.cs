using Flowthru.Flow;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Data._01_Raw.Schemas;
using SpaceflightsPythonEFCore.Data._02_Intermediate.Schemas;
using SpaceflightsPythonEFCore.Data._03_Primary.Schemas;
using SpaceflightsPythonEFCore.Flows.DataProcessing.Steps;

namespace SpaceflightsPythonEFCore.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline implemented entirely in C# with EFCore-backed catalog entries.
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
