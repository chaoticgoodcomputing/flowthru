using Flowthru.Data.Catalog;
using KedroIrisFUnit.Data._04_Feature.Schemas;

namespace KedroIrisFUnit.Data;

/// <summary>
/// Feature data layer: Engineered features for ML.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Iris data with one-hot encoded species classifications.
  /// Created by the DataEngineering pipeline after splitting and encoding.
  /// </summary>
  public IItem<IEnumerable<IrisFeatureSchema>> IrisFeatures =>
    CreateItem(() =>
      ItemFactory.Enumerable.Json<IrisFeatureSchema>(
        label: "IrisFeatures",
        filePath: $"{_basePath}/_04_Feature/Datasets/iris_features.json"
      )
    );
}
