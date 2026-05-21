using Flowthru.Data.Catalog;
using Iris.Data._07_ModelOutput.Schemas;

namespace Iris.Data;

/// <summary>
/// Model output data layer: Model predictions and scores.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<PredictionSchema>> Predictions =>
    CreateItem(() => Item.Of<IEnumerable<PredictionSchema>>("Predictions")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/predictions.json")
      .Build());
}
