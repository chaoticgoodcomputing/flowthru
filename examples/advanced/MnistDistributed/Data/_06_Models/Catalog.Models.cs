using Flowthru.Data.Catalog;

namespace MnistDistributed.Data;

public partial class Catalog
{
  /// <summary>
  /// Trained model weights as a pickled blob. Only rank 0 of the
  /// distributed training step writes here; ranks 1+ exit without
  /// returning anything to the Flowthru protocol.
  /// </summary>
  public IItem<byte[]> ModelWeights =>
    CreateItem(() => Item.Of<byte[]>("ModelWeights")
      .Binary()
      .AtPath($"{_basePath}/_06_Models/Datasets/model.pkl")
      .Build());
}
