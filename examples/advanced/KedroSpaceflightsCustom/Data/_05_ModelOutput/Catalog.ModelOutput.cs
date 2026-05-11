using Flowthru.Data.Catalog;
using KedroSpaceflightsCustom.Data._05_ModelOutput.Schemas;

namespace KedroSpaceflightsCustom.Data;

public partial class Catalog
{
  /// <summary>Model evaluation metrics.</summary>
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .Json()
      .AtPath($"{_basePath}/_05_ModelOutput/Datasets/model_metrics.json")
      .Build());

  /// <summary>Model predictions with actual and predicted values.</summary>
  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .Csv()
      .AtPath($"{_basePath}/_05_ModelOutput/Datasets/model_predictions.csv")
      .Build());
}
