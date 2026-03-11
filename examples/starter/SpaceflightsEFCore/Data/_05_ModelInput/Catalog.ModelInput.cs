using Flowthru.Data;
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
  public ICatalogEntry<IEnumerable<TrainingData>> TrainSplit =>
    GetOrCreateEntry(
      () =>
        EFCoreCatalogEntries.Enumerable.EFCore<TrainingData>(label: "XTrain", context: _dbContext)
    );

  /// <summary>
  /// Test dataset with features and labels for model evaluation.
  /// </summary>
  public ICatalogEntry<IEnumerable<TestData>> TestSplit =>
    GetOrCreateEntry(
      () => EFCoreCatalogEntries.Enumerable.EFCore<TestData>(label: "XTest", context: _dbContext)
    );
}
