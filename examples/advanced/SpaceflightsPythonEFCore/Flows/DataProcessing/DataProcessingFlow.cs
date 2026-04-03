using Flowthru.Flows;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Flows.DataProcessing.Steps;

namespace SpaceflightsPythonEFCore.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline implemented entirely in C# with EFCore-backed catalog entries.
/// Produces the ModelInputTable in SQLite, which is the EFCore → Python handoff point.
/// </summary>
public static class DataProcessingFlow
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PreprocessCompanies",
        description: "Parse and validate raw company data (C#). Stores result in EFCore.",
        transform: PreprocessCompaniesStep.Create(),
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies
      );

      pipeline.AddStep(
        label: "PreprocessShuttles",
        description: "Parse and validate raw shuttle data (C#). Stores result in EFCore.",
        transform: PreprocessShuttlesStep.Create(),
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles
      );

      pipeline.AddStep(
        label: "CreateModelInputTable",
        description: "Join preprocessed shuttles, companies, and reviews into a model input table (C#). Stores result in EFCore for Python consumption.",
        transform: CreateModelInputTableStep.Create(),
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
