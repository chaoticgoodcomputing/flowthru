using Flowthru.Data.Catalog;
using IrisFUnit.Data._06_Models.Schemas;

namespace IrisFUnit.Data;

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
