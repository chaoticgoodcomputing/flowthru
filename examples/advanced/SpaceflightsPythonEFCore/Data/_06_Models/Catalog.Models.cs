using Flowthru.Data;
using SpaceflightsPythonEFCore.Data._06_Models.Schemas;

namespace SpaceflightsPythonEFCore.Data;

/// <summary>
/// Models data layer: Serialized trained model artifact.
/// Written by the Python train_model node, stored as a JSON file.
/// </summary>
public partial class Catalog
{
  public IItem<LinearRegressionModel> Regressor =>
    CreateItem(
      () =>
        Items.Single.Json<LinearRegressionModel>(
          label: "Regressor",
          filePath: $"{_basePath}/_06_Models/Datasets/regressor.json"
        )
    );
}
