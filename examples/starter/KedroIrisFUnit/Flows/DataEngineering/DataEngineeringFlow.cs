using Flowthru.Core.Flows;
using KedroIrisFUnit.Data;
using KedroIrisFUnit.Flows.DataEngineering.Steps;

namespace KedroIrisFUnit.Flows.DataEngineering;

/// <summary>
/// Creates the data engineering pipeline that splits iris data and encodes species labels.
/// </summary>
public static class DataEngineeringFlow
{
  public static Flow Create(Catalog catalog, FlowConfig config)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "SplitAndEncode",
        description: """
          Splits the Iris dataset into training and test sets.
          Applies one-hot encoding to species labels and separates features from targets.
        """,
        transform: SplitAndEncodeStep.Create,
        input: (catalog.IrisRaw, config.SplitOptions),
        output: (catalog.IrisFeatures, catalog.TrainX, catalog.TrainY, catalog.TestX, catalog.TestY)
      );
    });
  }
}
