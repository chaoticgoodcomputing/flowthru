using Flowthru.Data;
using KedroSpaceflights.Custom.Data._05_ModelOutput.Schemas;

namespace KedroSpaceflights.Custom.Data;

public partial class Catalog
{
  /// <summary>
  /// Model evaluation metrics.
  /// Contains R², MAE, RMSE, etc.
  /// Stored as a singleton object (pipeline produces single metrics object).
  /// </summary>
  public ICatalogEntry<ModelMetrics> ModelMetrics =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_05_ModelOutput/Datasets/model_metrics.json"
        )
    );

  /// <summary>
  /// Model predictions with actual and predicted values.
  /// Used for evaluation and visualization.
  /// </summary>
  public ICatalogEntry<IEnumerable<ModelPredictions>> ModelPredictions =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<ModelPredictions>(
          label: "ModelPredictions",
          filePath: $"{_basePath}/_05_ModelOutput/Datasets/model_predictions.csv"
        )
    );
}
