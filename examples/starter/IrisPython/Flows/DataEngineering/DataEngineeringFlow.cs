using Flowthru.Flow;
using Flowthru.Step.Python;
using IrisPython.Data;

namespace IrisPython.Flows.DataEngineering;

/// <summary>
/// Data engineering pipeline using Python node for train/test splitting.
/// </summary>
public static class DataEngineeringFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("DataEngineering", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "SplitData",
        module: "Flows.DataEngineering.Steps.split_data",
        function: "split_data",
        input: (catalog.IrisRaw, catalog.SplitDataOptions),
        output: (catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY),
        executor: executor
      );
    });
  }
}
