using Flowthru.Data.Catalog;
using KedroSpaceflightsFUnit.Data._06_Models.Schemas;

namespace KedroSpaceflightsFUnit.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// </summary>
public partial class Catalog
{
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .Json()
      .AtPath($"{_basePath}/_06_Models/Datasets/regressor.json")
      .Build());
}
