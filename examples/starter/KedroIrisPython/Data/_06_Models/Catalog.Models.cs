using Flowthru.Data;

namespace KedroIrisPython.Data;

/// <summary>
/// Models layer: Trained model artifacts (weights, parameters, metadata).
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Trained logistic regression model weights.
  /// Stored as a pickled binary file (Python pickle format).
  /// </summary>
  public ICatalogEntry<byte[]> ModelWeights =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "ModelWeights",
          filePath: $"{_basePath}/_06_Models/Datasets/model.pkl"
        )
    );
}
