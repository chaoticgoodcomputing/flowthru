using Flowthru.Data;
using KedroSpaceflights.Pure.Data._04_Models.Schemas;

namespace KedroSpaceflights.Pure.Data;

public partial class Catalog
{
  // Model entry
  public ICatalogEntry<LinearRegressionModel> Regressor =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<LinearRegressionModel>(
          label: "Regressor",
          filePath: $"{_basePath}/_04_Models/Datasets/regressor.json"
        )
    );

  public ICatalogEntry<ModelMetrics> ModelMetrics =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<ModelMetrics>(
          label: "ModelMetrics",
          filePath: $"{_basePath}/_04_Models/Datasets/model_metrics.json"
        )
    );
}
