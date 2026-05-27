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

  /// <summary>
  /// Verification output from a second distributed step — a pickled
  /// dict of (rank, forward-pass-checksum) pairs. Exists primarily
  /// to exercise multi-invoke through one TorchrunLauncher-backed
  /// executor; the trained model from step 1 is the input here, so
  /// the DAG runs step 2 after step 1 via the same executor's worker
  /// pool.
  /// </summary>
  public IItem<byte[]> VerificationOutput =>
    CreateItem(() => Item.Of<byte[]>("VerificationOutput")
      .Binary()
      .AtPath($"{_basePath}/_06_Models/Datasets/verification.pkl")
      .Build());
}
