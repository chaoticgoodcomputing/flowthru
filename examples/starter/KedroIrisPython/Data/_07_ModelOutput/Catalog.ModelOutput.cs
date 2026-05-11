using Flowthru.Data.Catalog;
using KedroIrisPython.Data._07_ModelOutput.Schemas;

namespace KedroIrisPython.Data;

/// <summary>
/// Model output layer: Predictions, scores, and inference results.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<PredictionSchema>> Predictions =>
    CreateItem(() => Item.Of<IEnumerable<PredictionSchema>>("Predictions")
      .Parquet()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/predictions.parquet")
      .Build());
}
