using Flowthru.Data.Catalog;
using Spaceflights.Data._07_ModelOutput.Schemas;

namespace Spaceflights.Data;

/// <summary>
/// Model output data layer: Model predictions and scores.
/// </summary>
public partial class Catalog
{
  /// <summary>Evaluation metrics for the trained regression model.</summary>
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(() => Item.Of<ModelMetrics>("ModelMetrics")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_metrics.json")
      .Build());

  /// <summary>
  /// Model predictions containing actual and predicted values from the test set.
  /// Used for generating confusion matrices and prediction accuracy visualizations.
  /// </summary>
  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(() => Item.Of<IEnumerable<ModelPredictions>>("ModelPredictions")
      .Json()
      .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model_predictions.json")
      .Build());
}
