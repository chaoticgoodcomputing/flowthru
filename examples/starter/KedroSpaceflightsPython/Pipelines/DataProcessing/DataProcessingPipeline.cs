using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using Flowthru.Flows;
using KedroSpaceflightsPython.Data;
using KedroSpaceflightsPython.Data._01_Raw.Schemas;
using KedroSpaceflightsPython.Data._02_Intermediate.Schemas;
using KedroSpaceflightsPython.Data._03_Primary.Schemas;

namespace KedroSpaceflightsPython.Pipelines.DataProcessing;

/// <summary>
/// Data processing pipeline using Python nodes for preprocessing and joining.
/// </summary>
public static class DataProcessingPipeline
{
  /// <summary>
  /// Creates the data processing pipeline.
  /// </summary>
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddPythonStep(
        label: "PreprocessCompanies",
        description: "Clean and parse company data (Python)",
        module: "Pipelines.DataProcessing.Nodes.preprocess_companies",
        function: "preprocess_companies",
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "PreprocessShuttles",
        description: "Clean and parse shuttle data (Python)",
        module: "Pipelines.DataProcessing.Nodes.preprocess_shuttles",
        function: "preprocess_shuttles",
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles,
        executor: executor
      );

      pipeline.AddPythonStep<
        IEnumerable<PreprocessedShuttleSchema>,
        IEnumerable<PreprocessedCompanySchema>,
        IEnumerable<ReviewSchema>,
        IEnumerable<ModelInputTableSchema>
      >(
        label: "CreateModelInputTable",
        description: "Join shuttles, companies, and reviews (Python 3×1 node)",
        module: "Pipelines.DataProcessing.Nodes.create_model_input_table",
        function: "create_model_input_table",
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable,
        executor: executor
      );
    });
  }
}
