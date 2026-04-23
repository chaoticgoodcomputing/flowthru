using Flowthru.Core.Flows;
using KedroIris.Data;
using KedroIris.Flows.DataEngineering.Steps;

namespace KedroIris.Flows.DataEngineering;

/// <summary>
/// Creates the data engineering pipeline that splits iris data and encodes species labels.
/// </summary>
public static class DataEngineeringFlow
{
  /// <summary>
  /// Creates the data engineering pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="config">Configuration catalog providing pipeline parameters.</param>
  /// <returns>A configured pipeline that produces training and test splits with one-hot encoding.</returns>
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
