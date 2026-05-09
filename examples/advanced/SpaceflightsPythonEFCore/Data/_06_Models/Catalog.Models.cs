using Flowthru.Data.Catalog;
using SpaceflightsPythonEFCore.Data._06_Models.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Models data layer: Serialized trained model artifact.
/// Written by the Python train_model node, stored as a JSON file.
/// </summary>
public partial class Catalog
{
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(() => Item.Of<LinearRegressionModel>("Regressor")
      .Json()
      .AtPath($"{_basePath}/_06_Models/Datasets/regressor.json")
      .Build());
}
