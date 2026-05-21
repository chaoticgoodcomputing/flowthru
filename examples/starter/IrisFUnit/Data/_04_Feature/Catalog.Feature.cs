using Flowthru.Data.Catalog;
using IrisFUnit.Data._04_Feature.Schemas;

namespace IrisFUnit.Data;

/// <summary>
/// Feature data layer: Engineered features for ML.
/// </summary>
public partial class Catalog
{
  public IItem<IEnumerable<IrisFeatureSchema>> IrisFeatures =>
    CreateItem(() => Item.Of<IEnumerable<IrisFeatureSchema>>("IrisFeatures")
      .Json()
      .AtPath($"{_basePath}/_04_Feature/Datasets/iris_features.json")
      .Build());
}
