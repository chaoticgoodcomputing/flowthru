using Flowthru.Flow;
using Flowthru.Step.Python;
using KedroSpaceflightsPython.Data;

namespace KedroSpaceflightsPython.Flows.DataProcessing;

/// <summary>
/// Data processing pipeline using Python nodes for preprocessing and joining.
/// </summary>
public static class DataProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("DataProcessing", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "PreprocessCompanies",
        module: "Flows.DataProcessing.Steps.preprocess_companies",
        function: "preprocess_companies",
        input: catalog.Companies,
        output: catalog.PreprocessedCompanies,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "PreprocessShuttles",
        module: "Flows.DataProcessing.Steps.preprocess_shuttles",
        function: "preprocess_shuttles",
        input: catalog.Shuttles,
        output: catalog.PreprocessedShuttles,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "CreateModelInputTable",
        module: "Flows.DataProcessing.Steps.create_model_input_table",
        function: "create_model_input_table",
        input: (catalog.PreprocessedShuttles, catalog.PreprocessedCompanies, catalog.Reviews),
        output: catalog.ModelInputTable,
        executor: executor
      );
    });
  }
}
