using Flowthru.Data.Catalog;
using KedroIris.Data._06_Models.Schemas;

namespace KedroIris.Data;

/// <summary>
/// Models data layer: Serialized trained models.
/// </summary>
public partial class Catalog
{
  public IItem<ModelWeightsSchema> IrisModel =>
    CreateItem(() => Item.Of<ModelWeightsSchema>("IrisModel")
      .Json()
      .AtPath($"{_basePath}/_06_Models/Datasets/iris_model.json")
      .Build());
}
