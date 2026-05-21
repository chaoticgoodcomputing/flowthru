using Flowthru.Data.Catalog;
using SpaceflightsEnhanced.Data._04_Models.Schemas;

namespace SpaceflightsEnhanced.Data;

public partial class Catalog
{
  /// <summary>Trained ordinary least squares linear regression model.</summary>
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .Json()
      .AtPath($"{_basePath}/_04_Models/Datasets/regressor.json")
      .Build());
}
