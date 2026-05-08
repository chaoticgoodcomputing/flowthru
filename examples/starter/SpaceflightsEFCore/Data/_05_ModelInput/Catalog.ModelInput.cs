using Flowthru.Data.Catalog;
using Flowthru.Data.Storage.EFCore;
using SpaceflightsEFCore.Data._05_ModelInput.Schemas;

namespace SpaceflightsEFCore.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Training dataset with features and labels.
  /// </summary>
  public IItem<IEnumerable<TrainingData>> TrainSplit =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.EFCore<TrainingData, SpaceflightsDbContext>(
          label: "XTrain",
          contextFactory: _contextFactory
        )
    );

  /// <summary>
  /// Test dataset with features and labels for model evaluation.
  /// </summary>
  public IItem<IEnumerable<TestData>> TestSplit =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.EFCore<TestData, SpaceflightsDbContext>(
          label: "XTest",
          contextFactory: _contextFactory
        )
    );
}
