using Flowthru.Data.Catalog;
using SpaceflightsNewTypes.Data._06_Models.Schemas;

namespace SpaceflightsNewTypes.Data;

/// <summary>
/// Models data layer: Serialized trained models.
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
