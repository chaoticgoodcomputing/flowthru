using Flowthru.Flows;
using SpaceflightsPythonEFCore.Data;
using SpaceflightsPythonEFCore.Pipelines.DataProcessing.Nodes;

namespace SpaceflightsPythonEFCore.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline implemented entirely in C# with EFCore-backed catalog entries.
/// Produces the ModelInputTable in SQLite, which is the EFCore → Python handoff point.
/// </summary>
public static class DataProcessingPipeline
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "PreprocessCompanies",
        description: "Parse and validate raw company data (C#). Stores result in EFCore.",
        transform: PreprocessCompaniesNode.Create(),
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies
      );

      pipeline.AddStep(
        label: "PreprocessShuttles",
        description: "Parse and validate raw shuttle data (C#). Stores result in EFCore.",
        transform: PreprocessShuttlesNode.Create(),
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles
      );

      pipeline.AddStep(
        label: "CreateModelInputTable",
        description: "Join preprocessed shuttles, companies, and reviews into a model input table (C#). Stores result in EFCore for Python consumption.",
        transform: CreateModelInputTableNode.Create(),
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable
      );
    });
  }
}
