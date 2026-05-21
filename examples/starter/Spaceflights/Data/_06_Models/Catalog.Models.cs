using Flowthru.Data.Catalog;
using Spaceflights.Data._06_Models.Schemas;

namespace Spaceflights.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// Contains persisted model artifacts for reproducibility and deployment.
/// </summary>
public partial class Catalog
{
  /// <summary>Trained linear regression model with coefficients and feature names.</summary>
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .Json()
      .AtPath($"{_basePath}/_06_Models/Datasets/regressor.json")
      .Build());
}
