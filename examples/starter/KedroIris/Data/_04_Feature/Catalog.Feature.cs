using Flowthru.Data;
using KedroIris.Data._04_Feature.Schemas;

namespace KedroIris.Data;

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
    CreateItem(
      () =>
        Items.Enumerable.Csv<IrisFeatureSchema>(
          label: "IrisFeatures",
          filePath: $"{_basePath}/_04_Feature/Datasets/iris_features.csv"
        )
    );
}
