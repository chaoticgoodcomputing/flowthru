using Flowthru.Core.Data;
using KedroIris.Data._07_ModelOutput.Schemas;

namespace KedroIris.Data;

/// <summary>
/// Model output data layer: Model predictions and scores.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Predictions from the trained model on the test set.
  /// </summary>
  public IItem<IEnumerable<PredictionSchema>> Predictions =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<PredictionSchema>(
          label: "Predictions",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/predictions.json"
        )
    );
}
