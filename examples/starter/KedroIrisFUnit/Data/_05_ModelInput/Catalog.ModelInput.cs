using Flowthru.Data.Catalog;
using KedroIrisFUnit.Data._05_ModelInput.Schemas;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>Training feature vectors (X).</summary>
  public IItem<IEnumerable<FeatureVectorSchema>> TrainX =>
    CreateItem(() =>
      ItemFactory.Enumerable.Json<FeatureVectorSchema>(
        label: "TrainX",
        filePath: $"{_basePath}/_05_ModelInput/Datasets/train_x.json"
      )
    );

  /// <summary>Training target labels (Y).</summary>
  public IItem<IEnumerable<TargetLabelSchema>> TrainY =>
    CreateItem(() =>
      ItemFactory.Enumerable.Json<TargetLabelSchema>(
        label: "TrainY",
        filePath: $"{_basePath}/_05_ModelInput/Datasets/train_y.json"
      )
    );

  /// <summary>Test feature vectors (X).</summary>
  public IItem<IEnumerable<FeatureVectorSchema>> TestX =>
    CreateItem(() =>
      ItemFactory.Enumerable.Json<FeatureVectorSchema>(
        label: "TestX",
        filePath: $"{_basePath}/_05_ModelInput/Datasets/test_x.json"
      )
    );

  /// <summary>Test target labels (Y).</summary>
  public IItem<IEnumerable<TargetLabelSchema>> TestY =>
    CreateItem(() =>
      ItemFactory.Enumerable.Json<TargetLabelSchema>(
        label: "TestY",
        filePath: $"{_basePath}/_05_ModelInput/Datasets/test_y.json"
      )
    );
}
