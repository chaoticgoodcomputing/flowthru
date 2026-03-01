using Flowthru.Data;
using KedroSpaceflights.Data._05_ModelInput.Schemas;

namespace KedroSpaceflights.Data;

/// <summary>
/// Model input data layer: Joined feature tables ("master tables").
/// Contains training and test splits ready for model consumption.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Training dataset split from the model input table. Transient (memory only).
  /// </summary>
  public ICatalogEntry<IEnumerable<TrainingData>> TrainSplit =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TrainingData>(label: "XTrain"));

  /// <summary>
  /// Test dataset split from the model input table. Transient (memory only).
  /// </summary>
  public ICatalogEntry<IEnumerable<TestData>> TestSplit =>
    GetOrCreateEntry(() => CatalogEntries.Enumerable.Memory<TestData>(label: "XTest"));
}
