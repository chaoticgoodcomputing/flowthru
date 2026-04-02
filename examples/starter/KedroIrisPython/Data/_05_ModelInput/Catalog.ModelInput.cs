using Flowthru.Data;
using KedroIrisPython.Data._05_ModelInput.Schemas;

namespace KedroIrisPython.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Training feature vectors (X).
  /// </summary>
  public IItem<IEnumerable<FeatureVectorSchema>> TrainX =>
    CreateItem(
      () =>
        Items.Enumerable.Parquet<FeatureVectorSchema>(
          label: "TrainX",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/train_x.parquet"
        )
    );

  /// <summary>
  /// Training target labels (Y) with one-hot encoding.
  /// </summary>
  public IItem<IEnumerable<TargetLabelSchema>> TrainY =>
    CreateItem(
      () =>
        Items.Enumerable.Parquet<TargetLabelSchema>(
          label: "TrainY",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/train_y.parquet"
        )
    );

  /// <summary>
  /// Test feature vectors (X).
  /// </summary>
  public IItem<IEnumerable<FeatureVectorSchema>> TestX =>
    CreateItem(
      () =>
        Items.Enumerable.Parquet<FeatureVectorSchema>(
          label: "TestX",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/test_x.parquet"
        )
    );

  /// <summary>
  /// Test target labels (Y) with one-hot encoding.
  /// </summary>
  public IItem<IEnumerable<TargetLabelSchema>> TestY =>
    CreateItem(
      () =>
        Items.Enumerable.Parquet<TargetLabelSchema>(
          label: "TestY",
          filePath: $"{_basePath}/_05_ModelInput/Datasets/test_y.parquet"
        )
    );
}
