using Flowthru.Data;
using KedroIris.Data._05_ModelInput.Schemas;

namespace KedroIris.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Training feature vectors (X).
  /// </summary>
  public ICatalogEntry<IEnumerable<FeatureVectorSchema>> TrainX =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<FeatureVectorSchema>(
          label: "TrainX",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/train_x.csv"
        )
    );

  /// <summary>
  /// Training target labels (Y).
  /// </summary>
  public ICatalogEntry<IEnumerable<TargetLabelSchema>> TrainY =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<TargetLabelSchema>(
          label: "TrainY",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/train_y.csv"
        )
    );

  /// <summary>
  /// Test feature vectors (X).
  /// </summary>
  public ICatalogEntry<IEnumerable<FeatureVectorSchema>> TestX =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<FeatureVectorSchema>(
          label: "TestX",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/test_x.csv"
        )
    );

  /// <summary>
  /// Test target labels (Y).
  /// </summary>
  public ICatalogEntry<IEnumerable<TargetLabelSchema>> TestY =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<TargetLabelSchema>(
          label: "TestY",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/test_y.csv"
        )
    );
}
