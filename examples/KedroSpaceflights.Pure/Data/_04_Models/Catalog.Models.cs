using Flowthru.Data;
using KedroSpaceflights.Pure.Data._04_Models.Schemas;

namespace KedroSpaceflights.Pure.Data;

public partial class Catalog
{
  /// <summary>
  /// Trained linear regression model with coefficients and feature names.
  /// </summary>
  public ICatalogEntry<LinearRegressionModel> Regressor =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<LinearRegressionModel>(
          label: "Regressor",
          filePath: $"{_basePath}/_04_Models/Datasets/regressor.json"
        )
    );

  /// <summary>
  /// Evaluation metrics for the trained regression model.
  /// </summary>
  public ICatalogEntry<ModelMetrics> ModelMetrics =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_04_Models/Datasets/model_metrics.json"
        )
    );
}
