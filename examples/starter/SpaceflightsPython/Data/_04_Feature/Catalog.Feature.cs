using Flowthru.Data.Catalog;

namespace SpaceflightsPython.Data;

/// <summary>
/// Feature data layer: Engineered features for ML.
/// This layer contains datasets derived from the primary layer, transformed into features suitable for model training.
/// </summary>
public partial class Catalog
{
  // TODO: Add feature datasets here as they are created
  // Example:
  // public IItem<IEnumerable<FeatureSchema>> Features =>
  //   CreateEntry(
  //     () =>
  //       ItemFactory.Enumerable.Csv<FeatureSchema>(
  //         label: "Features",
  //         filePath: $"{_basePath}/_04_Feature/Datasets/features.csv"
  //       )
  //   );
}
