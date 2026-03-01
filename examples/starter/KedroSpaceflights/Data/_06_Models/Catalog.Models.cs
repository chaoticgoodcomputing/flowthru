using Flowthru.Data;
using KedroSpaceflights.Data._06_Models.Schemas;

namespace KedroSpaceflights.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// Contains persisted model artifacts for reproducibility and deployment.
/// </summary>
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
          filePath: $"{_basePath}/_06_Models/Datasets/regressor.json"
        )
    );
}
