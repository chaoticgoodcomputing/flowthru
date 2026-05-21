using Flowthru.Data.Catalog;
using SpaceflightsPython.Data._07_ModelOutput.Schemas;

namespace SpaceflightsPython.Data;

/// <summary>
/// Model output data layer: Model predictions and scores.
/// </summary>
public partial class Catalog
{
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json")
      .Build());

  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json")
      .Build());
}
