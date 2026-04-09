using Flowthru.Core.Data;
using KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;

namespace KedroSpaceflights.Custom.Data;

public partial class Catalog
{
  /// <summary>
  /// Model evaluation metrics.
  /// Contains R², MAE, RMSE, etc.
  /// Stored as a singleton object (pipeline produces single metrics object).
  /// </summary>
  public IItem<ModelMetrics> ModelMetrics =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_05_ModelOutput/Datasets/model_metrics.json"
        )
    );

  /// <summary>
  /// Model predictions with actual and predicted values.
  /// Used for evaluation and visualization.
  /// </summary>
  public IItem<IEnumerable<ModelPredictions>> ModelPredictions =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Csv<ModelPredictions>(
          label: "ModelPredictions",
          filePath: $"{_basePath}/_05_ModelOutput/Datasets/model_predictions.csv"
        )
    );
}
