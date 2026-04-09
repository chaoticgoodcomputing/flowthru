using Flowthru.Core.Data;
using Flowthru.Extensions.EFCore.Data;
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
        EFCoreItemFactory.Enumerable.EFCore<TrainingData, SpaceflightsDbContext>(
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
        EFCoreItemFactory.Enumerable.EFCore<TestData, SpaceflightsDbContext>(
          label: "XTest",
          contextFactory: _contextFactory
        )
    );
}
