using Flowthru.Core.Data;

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
    public IItem<byte[]> ModelWeights =>
      CreateItem(
        () =>
          ItemFactory.Single.Binary(
            label: "ModelWeights",
            filePath: $"{_basePath}/_06_Models/Datasets/model.pkl"
          )
      );
}
