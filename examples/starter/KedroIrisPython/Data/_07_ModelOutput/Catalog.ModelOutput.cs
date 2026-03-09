using Flowthru.Data;
using KedroIrisPython.Data._07_ModelOutput.Schemas;

namespace KedroIrisPython.Data;

/// <summary>
/// Model output layer: Predictions, scores, and inference results.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Predicted class indices for test data.
  /// </summary>
  public ICatalogEntry<IEnumerable<PredictionSchema>> Predictions =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<PredictionSchema>(
          label: "Predictions",
          filePath: $"{_basePath}/_07_ModelOutput/Datasets/predictions.parquet"
        )
    );
}
