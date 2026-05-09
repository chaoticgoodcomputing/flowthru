using Flowthru.Data.Catalog;

namespace KedroIrisPython.Data;

/// <summary>
/// Models layer: Trained model artifacts (weights, parameters, metadata).
/// </summary>
public partial class Catalog
{
  public IItem<byte[]> ModelWeights =>
    CreateItem(() => Item.Of<byte[]>("ModelWeights")
      .Binary()
      .AtPath($"{_basePath}/_06_Models/Datasets/model.pkl")
      .Build());
}
